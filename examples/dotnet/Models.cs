using System.Text.Json.Serialization;

namespace HeyHoloHpiExample;

// Product Models
public record Product
{
    public required string Id { get; init; }
    public required object Name { get; init; } // string or Dictionary<string, string>
    public object? Description { get; init; }
    public object? Ingredients { get; init; }
    public object? Unit { get; init; }
    public string? ImageUrl { get; init; }
    public required decimal Price { get; init; }
    public string? Currency { get; init; }
    public required Category Category { get; init; }
    public List<ProductOption>? Options { get; init; }
    public List<string>? CrossSellIds { get; init; }
}

public record Category
{
    public required string Id { get; init; }
    public required object Name { get; init; } // string or Dictionary<string, string>
    public string? ImageUrl { get; init; }
}

public record ProductOption
{
    public required string Name { get; init; }
    public required object Label { get; init; } // string or Dictionary<string, string>
    public object? Description { get; init; }
    public string? Default { get; init; }
    public required List<ProductOptionValue> Options { get; init; }
    public bool Multiple { get; init; }
    public bool Required { get; init; }
}

public record ProductOptionValue
{
    public required string Value { get; init; }
    public required object Label { get; init; } // string or Dictionary<string, string>
    public object? Ingredients { get; init; }
    public decimal AdditionalPrice { get; init; }
}

public record ProductsResponse
{
    public required List<Product> Products { get; init; }
    public string? NextCursor { get; init; }
}

// Order Models
public record CreateOrderRequest
{
    public required List<OrderItem> Items { get; init; }
    public string? Location { get; init; }
    public string? Note { get; init; }
    public int? NumberOfPeople { get; init; }
    public string? ExistingOrderId { get; init; }
}

public record OrderItem
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
    public string? Note { get; init; }
    public List<OrderItemOption>? Options { get; init; }
}

public record OrderItemOption
{
    public required string Name { get; init; }
    public required List<string> Values { get; init; }
}

public record CreateOrderResponse
{
    public required string Id { get; init; }
    public required OrderStatus Status { get; init; }
    public required bool IsAddition { get; init; }
}

public record OrderStatusResponse
{
    public required string Id { get; init; }
    public required OrderStatus Status { get; init; }
}

public record CancelOrderRequest
{
    public string? Reason { get; init; }
}

public enum OrderStatus
{
    Pending,
    Accepted,
    InProgress,
    Ready,
    Completed,
    Cancelled,
    Failed
}

public record Order
{
    public required string Id { get; init; }
    public required OrderStatus Status { get; set; }
    public required List<OrderItem> Items { get; init; }
    public string? Note { get; init; }
    public int? NumberOfPeople { get; init; }
    public required DateTime CreatedAt { get; init; }
}

// Location Models
public record Location
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}
