package ai.heyholo.hpi.controller;

import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;
import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import ai.heyholo.hpi.model.CancelOrderRequest;
import ai.heyholo.hpi.model.CreateOrderRequest;
import ai.heyholo.hpi.model.CreateOrderResponse;
import ai.heyholo.hpi.model.ErrorResponse;
import ai.heyholo.hpi.model.Location;
import ai.heyholo.hpi.model.OrderStatus;
import ai.heyholo.hpi.model.OrderStatusResponse;
import ai.heyholo.hpi.model.Product;
import ai.heyholo.hpi.model.ProductsResponse;
import ai.heyholo.hpi.model.StoredOrder;
import ai.heyholo.hpi.service.DataService;

@RestController
public class HpiController {

    private static final Logger logger = LoggerFactory.getLogger(HpiController.class);
    private final DataService dataService;

    public HpiController(DataService dataService) {
        this.dataService = dataService;
    }

    @GetMapping("/products")
    public ResponseEntity<ProductsResponse> getProducts(
            @RequestParam(required = false) String cursor,
            @RequestHeader(value = "Authorization", required = false) String authorization) {

        logRequest("GET", "/products", authorization);

        List<Product> products = dataService.getProducts();
        int pageSize = 10;
        int startIndex = 0;

        if (cursor != null && !cursor.isEmpty()) {
            try {
                startIndex = Integer.parseInt(cursor);
            } catch (NumberFormatException e) {
                startIndex = 0;
            }
        }

        List<Product> pageProducts = products.stream()
                .skip(startIndex)
                .limit(pageSize)
                .toList();

        String nextCursor = startIndex + pageSize < products.size()
                ? String.valueOf(startIndex + pageSize)
                : null;

        return ResponseEntity.ok(new ProductsResponse(pageProducts, nextCursor));
    }

    /**
     * Idempotency-Key -> order id, for HPI's create-order deduplication rule.
     *
     * In a real POS this MUST survive a restart: the window it closes is "we accepted the
     * order, the response was lost to a timeout, HeyHolo retried" - and a process that
     * restarts in between would take the retry as a new order. An in-memory map is fine
     * for an example and wrong for production.
     */
    private final Map<String, String> idempotencyKeys = new ConcurrentHashMap<>();

    @PostMapping("/orders")
    public ResponseEntity<?> createOrder(
            @RequestBody CreateOrderRequest request,
            @RequestHeader(value = "Idempotency-Key", required = false) String idempotencyKey,
            @RequestHeader(value = "Authorization", required = false) String authorization) {

        logRequest("POST", "/orders", authorization);

        // HPI rule: every create carries an Idempotency-Key equal to HeyHolo's internal
        // order id. Deduplicating is OPTIONAL in the contract and strongly recommended in
        // practice - without it, a timed-out-but-successful create becomes a real double
        // order on retry.
        if (idempotencyKey != null && !idempotencyKey.isEmpty()) {
            String knownId = idempotencyKeys.get(idempotencyKey);
            if (knownId != null) {
                logger.info("Idempotent replay - returning the original order: {}", knownId);
                return ResponseEntity.ok(new CreateOrderResponse(knownId, OrderStatus.ACCEPTED, false));
            }
        }

        if (request.items() == null || request.items().isEmpty()) {
            return ResponseEntity.badRequest()
                    .body(new ErrorResponse("Items are required"));
        }

        String orderId = "order-" + UUID.randomUUID().toString().substring(0, 8);
        boolean isAddition = request.existingOrderId() != null && !request.existingOrderId().isEmpty();

        StoredOrder order = new StoredOrder(
                orderId,
                OrderStatus.ACCEPTED,
                request.items(),
                request.location(),
                request.note(),
                request.numberOfPeople()
        );

        dataService.saveOrder(order);

        logger.info("✅ Order created: {} with {} items", orderId, request.items().size());
        if (isAddition) {
            logger.info("   Added to existing order: {}", request.existingOrderId());
        }

        if (idempotencyKey != null && !idempotencyKey.isEmpty()) {
            idempotencyKeys.put(idempotencyKey, orderId);
        }

        return ResponseEntity.status(HttpStatus.CREATED)
                .body(new CreateOrderResponse(orderId, OrderStatus.ACCEPTED, isAddition));
    }

    @GetMapping("/orders/{id}")
    public ResponseEntity<?> getOrder(
            @PathVariable String id,
            @RequestHeader(value = "Authorization", required = false) String authorization) {

        logRequest("GET", "/orders/" + id, authorization);

        return dataService.getOrder(id)
                .<ResponseEntity<?>>map(order -> {
                    // Simulate order progression
                    Duration elapsed = Duration.between(order.getCreatedAt(), Instant.now());

                    if (order.getStatus() == OrderStatus.ACCEPTED && elapsed.getSeconds() > 5) {
                        order.setStatus(OrderStatus.IN_PROGRESS);
                    }
                    if (order.getStatus() == OrderStatus.IN_PROGRESS && elapsed.getSeconds() > 30) {
                        order.setStatus(OrderStatus.READY);
                    }
                    if (order.getStatus() == OrderStatus.READY && elapsed.getSeconds() > 60) {
                        order.setStatus(OrderStatus.COMPLETED);
                    }

                    return ResponseEntity.ok(new OrderStatusResponse(order.getId(), order.getStatus()));
                })
                .orElseGet(() -> ResponseEntity.status(HttpStatus.NOT_FOUND)
                        .body(new ErrorResponse("Order not found")));
    }

    @PostMapping("/orders/{id}/cancel")
    public ResponseEntity<?> cancelOrder(
            @PathVariable String id,
            @RequestBody(required = false) CancelOrderRequest request,
            @RequestHeader(value = "Authorization", required = false) String authorization) {

        logRequest("POST", "/orders/" + id + "/cancel", authorization);

        return dataService.getOrder(id)
                .<ResponseEntity<?>>map(order -> {
                    if (order.getStatus() == OrderStatus.COMPLETED || order.getStatus() == OrderStatus.CANCELLED) {
                        return ResponseEntity.badRequest()
                                .body(new ErrorResponse("Cannot cancel order with status: " + order.getStatus().getValue()));
                    }

                    order.setStatus(OrderStatus.CANCELLED);
                    String reason = request != null && request.reason() != null
                            ? request.reason()
                            : "No reason provided";
                    logger.info("❌ Order cancelled: {} - Reason: {}", id, reason);

                    return ResponseEntity.ok(new OrderStatusResponse(order.getId(), order.getStatus()));
                })
                .orElseGet(() -> ResponseEntity.status(HttpStatus.NOT_FOUND)
                        .body(new ErrorResponse("Order not found")));
    }

    @GetMapping("/locations")
    public ResponseEntity<List<Location>> getLocations(
            @RequestHeader(value = "Authorization", required = false) String authorization) {

        logRequest("GET", "/locations", authorization);
        return ResponseEntity.ok(dataService.getLocations());
    }

    private void logRequest(String method, String path, String authorization) {
        logger.info("{} {}", method, path);
        if (authorization == null || authorization.isEmpty()) {
            logger.warn("⚠️  Warning: No Authorization header provided");
        }
    }
}
