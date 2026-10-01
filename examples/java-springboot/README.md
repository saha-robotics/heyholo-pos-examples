# Java Spring Boot Example Service for HeyHolo POS Integration

This is a reference implementation of a POS service that integrates with the HeyHolo POS Interface (HPI). It demonstrates how to implement the required endpoints using Java 17 with Spring Boot 3.4.

## Prerequisites

- Java 17 or later
- Maven 3.9+ (or use the included Maven wrapper)

## Running the Service

1. Navigate to the example directory:
   ```bash
   cd examples/java-springboot
   ```

2. Run the service using Maven:
   ```bash
   mvn spring-boot:run
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

- **Spring Boot 3.4**: Uses the latest Spring Boot with Java 17+
- **Java Records**: Modern immutable data classes for all DTOs
- **snake_case JSON**: Automatic conversion via Jackson configuration
- **In-Memory Store**: Uses `ConcurrentHashMap` for thread-safe order storage
- **Validation**: Request validation with clear error messages
- **Logging**: Structured logging with SLF4J

## Project Structure

```
src/main/java/ai/heyholo/hpi/
├── HpiExampleApplication.java     # Main Spring Boot application
├── config/
│   └── AppConfig.java             # Application configuration
├── controller/
│   └── HpiController.java         # REST API endpoints
├── model/
│   ├── Category.java              # Product category
│   ├── CancelOrderRequest.java    # Cancel order request DTO
│   ├── CreateOrderRequest.java    # Create order request DTO
│   ├── CreateOrderResponse.java   # Create order response DTO
│   ├── ErrorResponse.java         # Error response DTO
│   ├── Location.java              # Location/table
│   ├── OrderItem.java             # Order line item
│   ├── OrderItemOption.java       # Order item option selection
│   ├── OrderStatus.java           # Order status enum
│   ├── OrderStatusResponse.java   # Order status response DTO
│   ├── Product.java               # Product entity
│   ├── ProductOption.java         # Product option/modifier
│   ├── ProductOptionValue.java    # Option value with price
│   ├── ProductsResponse.java      # Products list response
│   └── StoredOrder.java           # Internal order storage
└── service/
    └── DataService.java           # Data access and storage
```

## Building for Production

### Create JAR
```bash
mvn clean package
```

### Run the JAR
```bash
java -jar target/hpi-example-1.0.0.jar
```

### Create Docker Image (optional)
```bash
mvn spring-boot:build-image
```

## Key Features

### Modern Java 17+ Features
- **Record Classes**: Immutable DTOs with automatic equals/hashCode/toString
- **Pattern Matching**: Enhanced switch expressions and instanceof

### Order Status Simulation
Orders automatically progress through states:
- `accepted` → `in_progress` (after 5 seconds)
- `in_progress` → `ready` (after 15 seconds total)

### Multi-language Support
Demonstrates both approaches for translatable fields:
- Simple strings: `"name": "Cola"`
- Language maps: `"name": {"en": "Classic Burger", "tr": "Klasik Burger"}`

## Configuration

The application can be configured via `application.properties` or environment variables:

| Property | Default | Description |
|----------|---------|-------------|
| `server.port` | 5000 | HTTP server port |
| `spring.jackson.property-naming-strategy` | SNAKE_CASE | JSON field naming |

## License

See [LICENSE](LICENSE) file for details.
