# Python Example Service for HeyHolo POS Integration

This is a reference implementation of a POS service that integrates with the HeyHolo POS Interface (HPI). It demonstrates how to implement the required endpoints using Python with type hints.

## Prerequisites

- Python 3.10+ (uses modern type hints and standard library)

## Installation

No external dependencies required! This example uses only Python's standard library.

## Running the Service

1. Navigate to the example directory:
   ```bash
   cd examples/python
   ```

2. Run the service:
   ```bash
   python3 server.py
   ```

   Or make it executable:
   ```bash
   chmod +x server.py
   ./server.py
   ```

The service will start on port **5000**.

## Testing the Service

You can test the endpoints using `curl` or the provided Postman collection at [published Postman collection](https://developers.heyholo.ai/pos/heyholo-pos-interface.postman_collection.json).

### Authentication
The service expects an `Authorization` header. The example uses a hardcoded token but accepts any value for demonstration purposes (logs a warning if missing).

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
    "location": "loc-1",
    "note": "Allergy: Peanuts",
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

**5. Get Locations**
```bash
curl -H "Authorization: Bearer secret" http://localhost:5000/locations
```

## Implementation Details

- **Framework-less**: Uses Python's native `http.server` module (no Flask or other frameworks).
- **Type Hints**: Fully typed with TypedDict for all API models.
- **Standard Library Only**: No external dependencies required.
- **In-Memory Store**: Uses a dict to store order data (reset on restart).
- **JSON Logging**: Structured logging for easy parsing.
- **Graceful Shutdown**: Handles KeyboardInterrupt (Ctrl+C) for clean shutdown.

## Project Structure

```
server.py      # Main server implementation with request handlers
models.py      # Type definitions using TypedDict
database.py    # In-memory data store
```

## Python Version

Requires Python 3.10+ for:
- Modern type hints (`list[T]`, `dict[K, V]`)
- Union types with `|` operator
- `TypedDict` improvements
