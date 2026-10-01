# HeyHolo POS Interface (HPI) — example implementations

Reference implementations of the **HeyHolo POS Interface (HPI)** in five languages. HPI is the REST
contract a point-of-sale system implements so HeyHolo can sync its catalog and place, poll and
cancel orders against it.

> **The specification lives at [developers.heyholo.ai/en/docs/pos](https://developers.heyholo.ai/en/docs/pos).**
>
> That is the normative contract and the only place it is maintained. This repository deliberately
> does **not** ship a copy of the spec: two copies of a contract drift, and the one that drifts is
> always the one someone is reading. Code here, rules there.

Also published alongside the docs:

- [Postman collection](https://developers.heyholo.ai/pos/heyholo-pos-interface.postman_collection.json) — drives all five endpoints against your server, with tests that assert the contract rules
- [Single-file reference server](https://developers.heyholo.ai/pos/hpi-reference-server.mjs) — dependency-free Node, if you want one file rather than a project

## The four rules these examples exist to demonstrate

Every example implements the endpoints. What makes them worth reading is that they also implement
the four rules integrations most often get wrong — each one a real failure, not a style preference:

1. **The cursor paginates one sync run; it is not "changes since last sync."** Every run must return
   the full sellable catalog, because any product missing from a completed sync is treated as removed
   from the menu. A delta response deletes the rest of the venue's menu. (At most 30 pages per run.)
2. **Deduplicate on `Idempotency-Key`.** Every create carries one, equal to HeyHolo's internal order
   id. Without dedup, a create that succeeds and then times out on the way back becomes a real double
   order when HeyHolo retries. Optional in the contract; strongly recommended in the field.
3. **404 means gone, 5xx means try again.** A 404 on `GET /orders/{id}` tells HeyHolo the order no
   longer exists and it is marked cancelled. Use 5xx for a lock, a database hiccup, anything transient.
4. **A cancel response returns a known status.** Free-form text is read as `unknown` and shown to the
   venue operator as an *unconfirmed* cancellation — the order is not assumed cancelled.

## Overview

These implementations show how to build a compliant API that supports:

- Product synchronization with categories and options
- Order creation and management
- Order status tracking
- Order cancellation
- Location management

## Available Examples

This repository includes working implementations in the following languages:

| Language | Framework | Location | Status |
|----------|-----------|----------|--------|
| **Node.js/TypeScript** | Native HTTP | [`examples/nodejs/`](examples/nodejs/) | ✅ Complete |
| **Python** | Standard Library | [`examples/python/`](examples/python/) | ✅ Complete |
| **Go** | Standard Library | [`examples/go/`](examples/go/) | ✅ Complete |
| **.NET** | .NET 9 Minimal APIs | [`examples/dotnet/`](examples/dotnet/) | ✅ Complete |
| **Java** | Spring Boot 3.4 | [`examples/java-springboot/`](examples/java-springboot/) | ✅ Complete |

## Quick Start

Each example implementation can be run independently. Choose the language you're most comfortable with:

### Node.js/TypeScript

```bash
cd examples/nodejs
npm install
npm run dev
```

Server runs on `http://localhost:5000`

### Python

```bash
cd examples/python
python3 server.py
```

Server runs on `http://localhost:5000`

### Go

```bash
cd examples/go
go run .
```

Server runs on `http://localhost:5000`

### .NET

```bash
cd examples/dotnet
dotnet run
```

Server runs on `http://localhost:5000`

### Java (Spring Boot)

```bash
cd examples/java-springboot
mvn spring-boot:run
```

Server runs on `http://localhost:5000`

## API Endpoints

All implementations support the following endpoints:

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/products` | Retrieve product catalog with pagination |
| `POST` | `/orders` | Create a new order |
| `GET` | `/orders/{id}` | Get order status |
| `POST` | `/orders/{id}/cancel` | Cancel an order |
| `GET` | `/locations` | Get available locations (tables/rooms) |

## Authentication

All examples use header-based authentication. By default, they expect an `Authorization` header:

```bash
curl -H "Authorization: Bearer your-secret-token" http://localhost:5000/products
```

The examples log warnings for missing authentication but accept any token for demonstration purposes.

## Testing

### 📦 Using Postman (Recommended)

A complete **Postman collection** is published alongside the docs: [heyholo-pos-interface.postman_collection.json](https://developers.heyholo.ai/pos/heyholo-pos-interface.postman_collection.json). Its tests assert the contract rules, not just the status codes.

**Import it into Postman to test all endpoints with pre-configured requests.**

### Using cURL

**Get Products:**
```bash
curl -H "Authorization: Bearer secret" http://localhost:5000/products
```

**Create Order:**
```bash
curl -X POST http://localhost:5000/orders \
  -H "Authorization: Bearer secret" \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {
        "product_id": "prod-1",
        "quantity": 2,
        "options": [
          {
            "name": "opt-size",
            "values": ["val-large"]
          }
        ]
      }
    ],
    "location": "loc-table-1",
    "note": "Extra napkins please",
    "number_of_people": 2
  }'
```

**Get Order Status:**
```bash
curl -H "Authorization: Bearer secret" http://localhost:5000/orders/order-1
```

**Cancel Order:**
```bash
curl -X POST http://localhost:5000/orders/order-1/cancel \
  -H "Authorization: Bearer secret" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Customer requested"}'
```

**Get Locations:**
```bash
curl -H "Authorization: Bearer secret" http://localhost:5000/locations
```

## Documentation

- **[HPI specification](https://developers.heyholo.ai/en/docs/pos)** — the normative contract: endpoints, data models, order statuses and configuration
- **[Node.js Example](examples/nodejs/README.md)** - TypeScript implementation details
- **[Python Example](examples/python/README.md)** - Python implementation details
- **[Go Example](examples/go/README.md)** - Go implementation details
- **[.NET Example](examples/dotnet/README.md)** - .NET 9 implementation details

### 1. Product Synchronization
- Pagination with cursor-based navigation
- Multi-language support (translatable fields)
- Product categories and hierarchies
- Product options and variants
- Cross-sell recommendations
- Custom pricing per option

### 2. Order Management
- Order creation with multiple items
- Support for product options and modifiers
- Order notes and special instructions
- Location-based ordering (table/room assignments)
- Guest count tracking
- Adding items to existing orders

### 3. Order Status Tracking
- Real-time order status updates
- Standard status values: `accepted`, `pending`, `in_progress`, `ready`, `completed`, `cancelled`, `failed`
- Status-based robot dispatch triggers

### 4. Multi-language Support
All text fields support either:
- Simple strings (uses default language)
- Language maps: `{"en": "Burger", "tr": "Burger", "de": "Burger"}`

Supported translatable fields:
- Product/category names and descriptions
- Ingredients
- Units
- Option labels

## Data Models

### Product Structure
```json
{
  "id": "prod-102",
  "name": {"en": "Cheeseburger", "tr": "Peynirli Burger"},
  "description": "Delicious burger with cheese",
  "price": 150.0,
  "currency": "TRY",
  "category": {
    "id": "cat-1",
    "name": "Burgers"
  },
  "options": [
    {
      "name": "opt-size",
      "label": {"en": "Size", "tr": "Boyut"},
      "required": true,
      "multiple": false,
      "options": [
        {
          "value": "val-small",
          "label": "Small",
          "additional_price": 0
        },
        {
          "value": "val-large",
          "label": "Large",
          "additional_price": 20.0
        }
      ]
    }
  ]
}
```

### Order Structure
```json
{
  "items": [
    {
      "product_id": "prod-102",
      "quantity": 2,
      "note": "No onions",
      "options": [
        {
          "name": "opt-size",
          "values": ["val-large"]
        }
      ]
    }
  ],
  "location": "loc-table-1",
  "note": "Rush order",
  "number_of_people": 4,
  "existing_order_id": "order-999"
}
```

## Implementation Notes

### Error Handling
Add proper error responses:
- `400 Bad Request` - Invalid input
- `401 Unauthorized` - Missing/invalid authentication
- `404 Not Found` - Resource not found
- `409 Conflict` - Order already exists
- `500 Internal Server Error` - Server errors

## Integration Configuration

To integrate with HeyHolo, you'll need to provide:

1. **Base URL**: Your API root (e.g., `https://api.yourpos.com/heyholo/v1`)
2. **Auth Header Name**: Header for authentication (e.g., `Authorization`)
3. **Auth Header Secret**: The secret value (e.g., `Bearer your-secret-key`)
4. **Default Language**: Language code for string translations (default: `en`)
5. **Default Currency**: ISO 4217 currency code (default: `TRY`)

## License

See [LICENSE](LICENSE) for details.

---

**Note**: These are reference implementations for demonstration purposes. They should be adapted with proper security, error handling, and production-ready practices before deployment.