#!/usr/bin/env python3
import json
import random
from datetime import datetime
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse, parse_qs
from typing import Any

from models import (
    CreateOrderRequest,
    OrderResponse,
    CancelOrderRequest,
    CancelOrderResponse,
    ProductsResponse,
)
from database import products, orders, locations, idempotency_keys

PORT = 5000
AUTH_TOKEN = "secret-token"  # In a real app, validate this properly


def log(level: str, message: str, **meta: Any) -> None:
    """Logger helper"""
    print(
        json.dumps(
            {"time": datetime.now().isoformat(), "level": level, "message": message, **meta}
        )
    )


class POSHandler(BaseHTTPRequestHandler):
    """Request handler for POS service"""

    def log_message(self, format: str, *args: Any) -> None:
        """Suppress default logging"""
        pass

    def _authenticate(self) -> bool:
        """Authentication middleware"""
        auth_header = self.headers.get("Authorization")
        if not auth_header:
            log("WARN", "Missing Authorization header")
            return False
        # In a real app, validate the token properly
        return True

    def _send_json(self, status: int, data: Any) -> None:
        """Send JSON response"""
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(json.dumps(data).encode())

    def _send_error_response(self, status: int, message: str) -> None:
        """Send error response"""
        self.send_response(status)
        self.send_header("Content-Type", "text/plain")
        self.end_headers()
        self.wfile.write(message.encode())

    def _parse_body(self) -> Any:
        """Parse JSON body"""
        content_length = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(content_length)
        return json.loads(body.decode()) if body else {}

    def do_GET(self) -> None:
        """Handle GET requests"""
        start = datetime.now()
        parsed = urlparse(self.path)
        log("INFO", "Request started", method="GET", path=parsed.path)

        if not self._authenticate():
            self._send_error_response(401, "Unauthorized")
            return

        if parsed.path == "/products":
            self._handle_products(parsed)
        elif parsed.path.startswith("/orders/"):
            order_id = parsed.path.split("/")[2]
            if order_id and not parsed.path.endswith("/cancel"):
                self._handle_get_order(order_id)
            else:
                self._send_error_response(404, "Not Found")
        elif parsed.path == "/locations":
            self._handle_locations()
        else:
            self._send_error_response(404, "Not Found")

        duration = (datetime.now() - start).total_seconds() * 1000
        log("INFO", "Request completed", method="GET", path=parsed.path, duration=duration)

    def do_POST(self) -> None:
        """Handle POST requests"""
        start = datetime.now()
        parsed = urlparse(self.path)
        log("INFO", "Request started", method="POST", path=parsed.path)

        if not self._authenticate():
            self._send_error_response(401, "Unauthorized")
            return

        if parsed.path == "/orders":
            self._handle_create_order()
        elif parsed.path.endswith("/cancel"):
            order_id = parsed.path.split("/")[2]
            self._handle_cancel_order(order_id)
        else:
            self._send_error_response(404, "Not Found")

        duration = (datetime.now() - start).total_seconds() * 1000
        log("INFO", "Request completed", method="POST", path=parsed.path, duration=duration)

    def _handle_products(self, parsed: Any) -> None:
        """Handle GET /products"""
        query_params = parse_qs(parsed.query)
        cursor = query_params.get("cursor", [None])[0]
        log("INFO", "Fetching products", cursor=cursor)

        # Simple pagination simulation
        resp_products = products
        next_cursor = None

        if cursor is None or cursor == "":
            next_cursor = "end"
        elif cursor == "end":
            resp_products = []

        response: ProductsResponse = {
            "products": resp_products,
            "next_cursor": next_cursor,
        }

        self._send_json(200, response)

    def _handle_create_order(self) -> None:
        """Handle POST /orders"""
        try:
            # HPI rule: every create carries an Idempotency-Key equal to HeyHolo's internal
            # order id. Deduplicating is OPTIONAL in the contract and strongly recommended in
            # practice — without it, a timed-out-but-successful create becomes a real double
            # order on retry. Checked before the body is parsed: a replay needs no parsing.
            idempotency_key = self.headers.get("Idempotency-Key")
            if idempotency_key and idempotency_key in idempotency_keys:
                known = orders.get(idempotency_keys[idempotency_key])
                if known:
                    log("INFO", "Idempotent replay - returning the original order",
                        id=known["id"], key=idempotency_key)
                    self._send_json(200, {
                        "id": known["id"],
                        "status": known["status"],
                        "is_addition": False,
                    })
                    return

            body: CreateOrderRequest = self._parse_body()

            if body.get("existing_order_id"):
                existing = orders.get(body["existing_order_id"])
                if not existing:
                    self._send_error_response(404, "Existing order not found")
                    return

                # Append items to existing order
                existing["items"].extend(body["items"])
                if body.get("note"):
                    existing["note"] = body["note"]

                order_id = existing["id"]
                status = existing["status"]
                is_addition = True
                log("INFO", "Added items to order", id=order_id, items_added=len(body["items"]))
            else:
                # Create new order
                order_id = f"ord-{random.randint(0, 99999)}"
                status = "accepted"

                orders[order_id] = {
                    "id": order_id,
                    "status": status,
                    "items": body["items"],
                    "location": body.get("location", ""),
                    "note": body.get("note", ""),
                    "number_of_people": body.get("number_of_people", 0),
                    "created_at": datetime.now(),
                }

                is_addition = False
                log("INFO", "Order created", id=order_id, items_count=len(body["items"]))

            if idempotency_key:
                idempotency_keys[idempotency_key] = order_id

            response: OrderResponse = {
                "id": order_id,
                "status": status,
                "is_addition": is_addition,
            }

            self._send_json(201, response)
        except (json.JSONDecodeError, KeyError, TypeError) as e:
            # Only a malformed body is the caller's fault.
            log("ERROR", "Invalid create order request", error=str(e))
            self._send_error_response(400, "Invalid request body")
        except Exception as e:
            # Anything else is ours and may be transient: 5xx makes HeyHolo retry.
            log("ERROR", "Failed to create order", error=str(e))
            self._send_error_response(500, "Internal error")

    def _handle_get_order(self, order_id: str) -> None:
        """Handle GET /orders/{id}"""
        order = orders.get(order_id)
        if not order:
            self._send_error_response(404, "Order not found")
            return

        response: OrderResponse = {
            "id": order["id"],
            "status": order["status"],
            "is_addition": False,
        }

        self._send_json(200, response)

    def _handle_cancel_order(self, order_id: str) -> None:
        """Handle POST /orders/{id}/cancel"""
        try:
            body: CancelOrderRequest = self._parse_body()
            order = orders.get(order_id)

            if not order:
                self._send_error_response(404, "Order not found")
                return

            order["status"] = "cancelled"
            log("INFO", "Order cancelled", id=order_id, reason=body.get("reason"))

            response: CancelOrderResponse = {"status": "cancelled"}
            self._send_json(200, response)
        except Exception as e:
            log("ERROR", "Failed to cancel order", error=str(e))
            self._send_error_response(400, "Invalid request body")

    def _handle_locations(self) -> None:
        """Handle GET /locations"""
        self._send_json(200, locations)


def main() -> None:
    """Main entry point"""
    server = HTTPServer(("", PORT), POSHandler)
    log("INFO", "Starting POS Service Example", port=PORT)

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        log("INFO", "Shutting down server...")
        server.shutdown()
        log("INFO", "Server exited")


if __name__ == "__main__":
    main()
