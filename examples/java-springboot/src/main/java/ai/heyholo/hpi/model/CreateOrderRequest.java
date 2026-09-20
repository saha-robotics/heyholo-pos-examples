package ai.heyholo.hpi.model;

import java.util.List;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record CreateOrderRequest(
    List<OrderItem> items,
    String location,
    String note,
    Integer numberOfPeople,
    String existingOrderId
) {}
