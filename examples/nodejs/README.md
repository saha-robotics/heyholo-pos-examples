# Node.js/TypeScript Example Service for HeyHolo POS Integration

This is a reference implementation of a POS service that integrates with the HeyHolo POS Interface (HPI). It demonstrates how to implement the required endpoints using Node.js with TypeScript.

## Prerequisites

- Node.js 18+ (uses native ES modules and modern APIs)
- npm or yarn

## Installation

1. Navigate to the example directory:
   ```bash
   cd examples/nodejs
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

## Running the Service

### Development Mode (with hot reload)
```bash
npm run dev
```

### Production Mode
```bash
npm run build
npm start
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

- **Framework-less**: Uses Node.js native `http` module (no Express or other frameworks).
- **TypeScript**: Fully typed with strict mode enabled.
- **ES Modules**: Uses modern ES module syntax.
- **In-Memory Store**: Uses a Map to store order data (reset on restart).
- **Type Safety**: Comprehensive type definitions for all API models.
- **Graceful Shutdown**: Handles SIGINT for clean server shutdown.

## Project Structure

```
src/
├── server.ts      # Main server implementation
├── types.ts       # TypeScript type definitions
└── database.ts    # In-memory data store
```
