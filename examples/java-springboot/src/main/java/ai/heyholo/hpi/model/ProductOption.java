package ai.heyholo.hpi.model;

import java.util.List;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record ProductOption(
    String name,
    Object label,  // String or Map<String, String> for i18n
    Object description,
    String defaultValue,
    List<ProductOptionValue> options,
    Boolean multiple,
    Boolean required
) {
    public ProductOption(String name, Object label, List<ProductOptionValue> options, Boolean required, Boolean multiple) {
        this(name, label, null, null, options, multiple, required);
    }

    public ProductOption(String name, Object label, String defaultValue, List<ProductOptionValue> options, Boolean required, Boolean multiple) {
        this(name, label, null, defaultValue, options, multiple, required);
    }
}
