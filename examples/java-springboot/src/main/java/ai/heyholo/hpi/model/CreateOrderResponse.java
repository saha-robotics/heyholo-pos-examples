package ai.heyholo.hpi.model;

public record CreateOrderResponse(
    String id,
    OrderStatus status,
    boolean isAddition
) {}
