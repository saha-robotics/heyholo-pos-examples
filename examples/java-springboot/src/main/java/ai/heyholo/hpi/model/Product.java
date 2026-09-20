package ai.heyholo.hpi.model;

import java.util.List;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record Product(
    String id,
    Object name,  // String or Map<String, String> for i18n
    Object description,
    Object ingredients,
    Object unit,
    String imageUrl,
    double price,
    String currency,
    Category category,
    List<ProductOption> options,
    List<String> crossSellIds
) {
    public Product(String id, Object name, Object description, double price, Category category) {
        this(id, name, description, null, null, null, price, null, category, null, null);
    }

    public Product(String id, Object name, Object description, double price, String currency, Category category, List<ProductOption> options) {
        this(id, name, description, null, null, null, price, currency, category, options, null);
    }
}
