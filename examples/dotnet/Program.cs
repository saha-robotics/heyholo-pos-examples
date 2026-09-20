using System.Text.Json;
using System.Text.Json.Serialization;
using HeyHoloHpiExample;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure JSON serialization for Native AOT
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

// Middleware for logging and authentication
app.Use(async (context, next) =>
{
    var requestPath = context.Request.Path;
    var method = context.Request.Method;
    Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {method} {requestPath}");

    // Simple authentication check
    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrEmpty(authHeader))
    {
        Console.WriteLine("⚠️  Warning: No Authorization header provided");
    }

    await next();
});

// GET /products - Sync products
app.MapGet("/products", (string? cursor) =>
{
    var products = Database.GetProducts();

    // Simple cursor-based pagination (for demo purposes)
    var pageSize = 10;
    var startIndex = 0;

    if (!string.IsNullOrEmpty(cursor) && int.TryParse(cursor, out var cursorIndex))
    {
        startIndex = cursorIndex;
    }

    var pageProducts = products.Skip(startIndex).Take(pageSize).ToList();
    var nextCursor = startIndex + pageSize < products.Count ? (startIndex + pageSize).ToString() : null;

    return Results.Json(new ProductsResponse
    {
        Products = pageProducts,
        NextCursor = nextCursor
    }, AppJsonSerializerContext.Default.ProductsResponse);
});

// POST /orders - Create order
// Idempotency-Key -> order id, for HPI's create-order deduplication rule.
//
// In a real POS this MUST survive a restart: the window it closes is "we accepted the
// order, the response was lost to a timeout, HeyHolo retried" - and a process that
// restarts in between would take the retry as a new order. An in-memory dictionary is
// fine for an example and wrong for production.
var IdempotencyKeys = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

app.MapPost("/orders", async (HttpContext context) =>
{
    // HPI rule: every create carries an Idempotency-Key equal to HeyHolo's internal order
    // id. Deduplicating is OPTIONAL in the contract and strongly recommended in practice -
    // without it, a timed-out-but-successful create becomes a real double order on retry.
    // Checked before the body is read: a replay needs no deserialization.
    var idempotencyKey = context.Request.Headers["Idempotency-Key"].ToString();
    if (!string.IsNullOrEmpty(idempotencyKey) && IdempotencyKeys.TryGetValue(idempotencyKey, out var knownId))
    {
        Console.WriteLine($"Idempotent replay - returning the original order: {knownId}");
        return Results.Json(new CreateOrderResponse
        {
            Id = knownId,
            Status = OrderStatus.Accepted,
            IsAddition = false
        }, AppJsonSerializerContext.Default.CreateOrderResponse);
    }

    CreateOrderRequest? request;
    try
    {
        var options = new System.Text.Json.JsonSerializerOptions(AppJsonSerializerContext.Default.Options);
        request = await context.Request.ReadFromJsonAsync(AppJsonSerializerContext.Default.CreateOrderRequest);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed to deserialize request: {ex.Message}");
        return Results.BadRequest(new ErrorResponse { Error = "Invalid request format" });
    }

    if (request == null || request.Items == null || request.Items.Count == 0)
    {
        return Results.BadRequest(new ErrorResponse { Error = "Items are required" });
    }

    var orderId = $"order-{Guid.NewGuid().ToString()[..8]}";
    var isAddition = !string.IsNullOrEmpty(request.ExistingOrderId);

    var order = new Order
    {
        Id = orderId,
        Status = OrderStatus.Accepted,
        Items = request.Items,
        Note = request.Note,
        NumberOfPeople = request.NumberOfPeople,
        CreatedAt = DateTime.UtcNow
    };

    Database.SaveOrder(order);

    if (!string.IsNullOrEmpty(idempotencyKey))
    {
        IdempotencyKeys[idempotencyKey] = orderId;
    }

    Console.WriteLine($"✅ Order created: {orderId} with {request.Items.Count} items");
    if (isAddition)
    {
        Console.WriteLine($"   Added to existing order: {request.ExistingOrderId}");
    }

    return Results.Json(new CreateOrderResponse
    {
        Id = orderId,
        Status = OrderStatus.Accepted,
        IsAddition = isAddition
    }, AppJsonSerializerContext.Default.CreateOrderResponse);
});

// GET /orders/{id} - Get order status
app.MapGet("/orders/{id}", (string id) =>
{
    var order = Database.GetOrder(id);

    if (order == null)
    {
        return Results.NotFound(new ErrorResponse { Error = "Order not found" });
    }

    // Simulate order progression
    if (order.Status == OrderStatus.Accepted &&
        (DateTime.UtcNow - order.CreatedAt).TotalSeconds > 5)
    {
        order.Status = OrderStatus.InProgress;
    }

    if (order.Status == OrderStatus.InProgress &&
        (DateTime.UtcNow - order.CreatedAt).TotalSeconds > 15)
    {
        order.Status = OrderStatus.Ready;
    }

    return Results.Json(new OrderStatusResponse
    {
        Id = order.Id,
        Status = order.Status
    }, AppJsonSerializerContext.Default.OrderStatusResponse);
});

// POST /orders/{id}/cancel - Cancel order
app.MapPost("/orders/{id}/cancel", async (string id, HttpContext context) =>
{
    CancelOrderRequest? request;
    try
    {
        request = await context.Request.ReadFromJsonAsync(AppJsonSerializerContext.Default.CancelOrderRequest);
    }
    catch
    {
        request = null;
    }

    var order = Database.GetOrder(id);

    if (order == null)
    {
        return Results.NotFound(new ErrorResponse { Error = "Order not found" });
    }

    if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
    {
        return Results.BadRequest(new ErrorResponse { Error = $"Cannot cancel order with status: {order.Status}" });
    }

    order.Status = OrderStatus.Cancelled;
    Console.WriteLine($"❌ Order cancelled: {id} - Reason: {request?.Reason ?? "No reason provided"}");

    return Results.Json(new OrderStatusResponse
    {
        Id = order.Id,
        Status = order.Status
    }, AppJsonSerializerContext.Default.OrderStatusResponse);
});

// GET /locations - Get locations
app.MapGet("/locations", () =>
{
    var locations = Database.GetLocations();
    return Results.Json(locations, AppJsonSerializerContext.Default.ListLocation);
});

Console.WriteLine("🚀 HeyHolo POS Interface - .NET Example");
Console.WriteLine("📡 Server starting on http://localhost:5000");
Console.WriteLine("📝 Endpoints available:");
Console.WriteLine("   GET  /products");
Console.WriteLine("   POST /orders");
Console.WriteLine("   GET  /orders/{id}");
Console.WriteLine("   POST /orders/{id}/cancel");
Console.WriteLine("   GET  /locations");
Console.WriteLine();

app.Run("http://localhost:5000");
