package ai.heyholo.hpi.model;

import java.util.List;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record ProductsResponse(
    List<Product> products,
    String nextCursor
) {}
