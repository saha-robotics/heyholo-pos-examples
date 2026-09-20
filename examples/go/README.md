# Go Example Service for HeyHolo POS Integration

This is a reference implementation of a POS service that integrates with the HeyHolo POS Interface (HPI). It demonstrates how to implement the required endpoints using standard Go libraries.

## Prerequisites

- Go 1.22 or higher

## Running the Service

1. Navigate to the example directory:
   ```bash
   cd examples/go
   ```

2. Run the service:
   ```bash
   go run .
   ```

The service will start on port **5000**.

## Testing the Service

You can test the endpoints using `curl` or the provided Postman collection at [published Postman collection](https://developers.heyholo.ai/pos/heyholo-pos-interface.postman_collection.json).

### Authentication
The service expects an `Authorization` header. The example uses a hardcoded token, but accepts any value for demonstration purposes (logs a warning if missing).

### Example Requests

**1. Get Products**
```bash
curl -H "Authorization: Bearer secret" http://localhost:5000/products
```

**2. Create Order**
```bash
curl -X POST http://localhost:5000/orders \
  -H "Authorization: Bearer secret" \
  -H "Content-Type: application/json" \
  -d '{
    "items": [{"product_id": "prod-1", "quantity": 1}],
    "location": "Table 1",
    "note": "No onions",
    "number_of_people": 2
  }'
```

**3. Get Order Status**
Replace `{order_id}` with the ID returned from the create order response.
```bash
curl -H "Authorization: Bearer secret" http://localhost:5000/orders/{order_id}
```

**4. Cancel Order**
```bash
curl -X POST http://localhost:5000/orders/{order_id}/cancel \
  -H "Authorization: Bearer secret" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Customer changed mind"}'
```

## Implementation Details

- **Framework-less**: Uses Go's standard `net/http` library with the Go 1.22 `ServeMux` for routing.
- **Middleware**: Demonstrates a simple middleware chain for logging and authentication.
- **In-Memory Store**: Uses a simple map to store order statuses (reset on restart).
- **Type Safety**: Uses enums for order statuses (`PENDING`, `CANCELLED`, `UNKNOWN`).
