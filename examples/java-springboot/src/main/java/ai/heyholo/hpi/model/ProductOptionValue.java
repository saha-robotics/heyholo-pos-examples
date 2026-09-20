package ai.heyholo.hpi.model;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record ProductOptionValue(
    String value,
    Object label,  // String or Map<String, String> for i18n
    Object ingredients,
    Double additionalPrice
) {
    public ProductOptionValue(String value, Object label, double additionalPrice) {
        this(value, label, null, additionalPrice);
    }

    public ProductOptionValue(String value, Object label) {
        this(value, label, null, 0.0);
    }
}
