# .NET Example Service for HeyHolo POS Integration

This is a reference implementation of a POS service that integrates with the HeyHolo POS Interface (HPI). It demonstrates how to implement the required endpoints using .NET 9 with minimal APIs and Native AOT support.

## Prerequisites

- .NET 9 SDK or later

## Running the Service

1. Navigate to the example directory:
   ```bash
   cd examples/dotnet
   ```

2. Run the service:
   ```bash
   dotnet run
   ```

The service will start on port **5000**.

## Testing the Service

You can test the endpoints using `curl` or the provided Postman collection at [published Postman collection](https://developers.heyholo.ai/pos/heyholo-pos-interface.postman_collection.json).

### Authentication
The service expects an `Authorization` header. The example logs a warning if missing but accepts any value for demonstration purposes.

### Example Requests

**1. Get Products**
```bash
curl -H "Authorization: Bearer secret" http://localhost:5000/products
```

**2. Get Products with Pagination**
```bash
curl -H "Authorization: Bearer secret" "http://localhost:5000/products?cursor=10"
```

**3. Create Order**
```bash
curl -X POST http://localhost:5000/orders \
  -H "Authorization: Bearer secret" \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {
        "product_id": "prod-1",
        "quantity": 1,
        "note": "No onions",
        "options": [
          {"name": "opt-doneness", "values": ["medium"]},
          {"name": "opt-extras", "values": ["extra-cheese"]}
        ]
      }
    ],
    "location": "loc-table-1",
    "note": "Allergy: Peanuts",
    "number_of_people": 2
  }'
```

**4. Get Order Status**
Replace `{order_id}` with the ID returned from the create order response.
```bash
curl -H "Authorization: Bearer secret" http://localhost:5000/orders/{order_id}
```

**5. Cancel Order**
```bash
curl -X POST http://localhost:5000/orders/{order_id}/cancel \
  -H "Authorization: Bearer secret" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Customer changed mind"}'
```

**6. Get Locations**
```bash
curl -H "Authorization: Bearer secret" http://localhost:5000/locations
```

## Implementation Details

- **.NET 9**: Uses the latest .NET features including minimal APIs
- **Native AOT Ready**: Configured for ahead-of-time compilation support with `PublishAot`
- **Slim Builder**: Uses `WebApplication.CreateSlimBuilder` for minimal footprint
- **Records**: Modern C# records for immutable data models
- **snake_case JSON**: Automatic conversion to snake_case for API consistency
- **In-Memory Store**: Uses `ConcurrentDictionary` for thread-safe order storage
- **Type Safety**: Comprehensive type definitions with nullable reference types enabled
- **Enum Serialization**: Automatic string conversion for order statuses

## Project Structure

```
Program.cs      # Main application with API endpoints
Models.cs       # Data models and DTOs
Database.cs     # In-memory data store
HeyHoloHpiExample.csproj  # Project configuration
```

## Building for Production

### Standard Build
```bash
dotnet build -c Release
```

### Native AOT Build (Faster startup, smaller size)
```bash
dotnet publish -c Release
```

The Native AOT compiled binary will be in `bin/Release/net9.0/publish/`.

### Run the Published Version
```bash
./bin/Release/net9.0/publish/HeyHoloHpiExample
```

## Key Features

### Modern .NET 9 Features
- **Minimal APIs**: Clean, functional endpoint definitions
- **Record Types**: Immutable data structures with value equality
- **Init-only Properties**: Immutable object initialization
- **Nullable Reference Types**: Compile-time null safety
- **Top-level Statements**: No boilerplate class/method wrappers

### Order Status Simulation
Orders automatically progress through states:
- `accepted` → `in_progress` (after 5 seconds)
- `in_progress` → `ready` (after 15 seconds total)

### Multi-language Support
Demonstrates both approaches for translatable fields:
- Simple strings: `"Name": "Cola"`
- Language maps: `"Name": {"en": "Classic Burger", "tr": "Klasik Burger"}`

## Performance Characteristics

When compiled with Native AOT:
- **Startup time**: ~10ms (vs ~500ms for JIT)
- **Memory usage**: ~15MB (vs ~50MB for JIT)
- **Binary size**: ~10MB (self-contained, no runtime required)

## Development Notes

### Hot Reload
The service supports .NET hot reload during development:
```bash
dotnet watch run
```

Changes to code will automatically reload without restarting the server.

### Debugging
Open in Visual Studio 2022, Visual Studio Code with C# Dev Kit, or JetBrains Rider for full debugging support.

## License

See [LICENSE](LICENSE) file for details.
