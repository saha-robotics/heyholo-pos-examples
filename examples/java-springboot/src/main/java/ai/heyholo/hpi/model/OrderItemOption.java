package ai.heyholo.hpi.model;

import java.util.List;

public record OrderItemOption(
    String name,
    List<String> values
) {}
