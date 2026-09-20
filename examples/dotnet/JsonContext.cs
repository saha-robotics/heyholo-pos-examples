using System.Text.Json;
using System.Text.Json.Serialization;

namespace HeyHoloHpiExample;

// Main response/request types
[JsonSerializable(typeof(ProductsResponse))]
[JsonSerializable(typeof(CreateOrderRequest))]
[JsonSerializable(typeof(CreateOrderResponse))]
[JsonSerializable(typeof(OrderStatusResponse))]
[JsonSerializable(typeof(CancelOrderRequest))]
[JsonSerializable(typeof(List<Location>))]

// Nested types
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(Category))]
[JsonSerializable(typeof(ProductOption))]
[JsonSerializable(typeof(ProductOptionValue))]
[JsonSerializable(typeof(OrderItem))]
[JsonSerializable(typeof(OrderItemOption))]
[JsonSerializable(typeof(Location))]
[JsonSerializable(typeof(OrderStatus))]

// Collections
[JsonSerializable(typeof(List<Product>))]
[JsonSerializable(typeof(List<ProductOption>))]
[JsonSerializable(typeof(List<ProductOptionValue>))]
[JsonSerializable(typeof(List<OrderItem>))]
[JsonSerializable(typeof(List<OrderItemOption>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]

// Error responses
[JsonSerializable(typeof(ErrorResponse))]

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(SnakeCaseLowerEnumConverter<OrderStatus>)]
)]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}

// Custom enum converter for snake_case_lower
public class SnakeCaseLowerEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            throw new JsonException($"Cannot convert null or empty string to {typeof(T)}");

        // Convert snake_case to PascalCase for parsing
        var pascalCase = string.Concat(value.Split('_').Select(word =>
            char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));

        if (Enum.TryParse<T>(pascalCase, true, out var result))
            return result;

        throw new JsonException($"Unable to convert \"{value}\" to {typeof(T)}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var name = value.ToString();
        var snakeCase = string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
        writer.WriteStringValue(snakeCase);
    }
}

public record ErrorResponse
{
    public required string Error { get; init; }
}
