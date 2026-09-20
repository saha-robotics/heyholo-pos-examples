package ai.heyholo.hpi.model;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record Category(
    String id,
    Object name,  // String or Map<String, String> for i18n
    String imageUrl
) {
    public Category(String id, Object name) {
        this(id, name, null);
    }
}
