package ai.heyholo.hpi.service;

import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.springframework.stereotype.Service;

import ai.heyholo.hpi.model.Category;
import ai.heyholo.hpi.model.Location;
import ai.heyholo.hpi.model.Product;
import ai.heyholo.hpi.model.ProductOption;
import ai.heyholo.hpi.model.ProductOptionValue;
import ai.heyholo.hpi.model.StoredOrder;

@Service
public class DataService {

    private final Map<String, StoredOrder> orders = new ConcurrentHashMap<>();

    private static final List<Product> PRODUCTS = createProducts();
    private static final List<Location> LOCATIONS = createLocations();

    public List<Product> getProducts() {
        return PRODUCTS;
    }

    public List<Location> getLocations() {
        return LOCATIONS;
    }

    public void saveOrder(StoredOrder order) {
        orders.put(order.getId(), order);
    }

    public Optional<StoredOrder> getOrder(String id) {
        return Optional.ofNullable(orders.get(id));
    }

    private static List<Product> createProducts() {
        return List.of(
            new Product(
                "prod-1",
                Map.of("en", "Classic Burger", "tr", "Klasik Burger"),
                Map.of("en", "Juicy beef patty with lettuce, tomato, and special sauce", "tr", "Marul, domates ve özel soslu dana köfte"),
                89.90,
                "TRY",
                new Category("cat-1", Map.of("en", "Burgers", "tr", "Burgerler")),
                List.of(
                    new ProductOption(
                        "opt-doneness",
                        Map.of("en", "Doneness", "tr", "Pişirme Derecesi"),
                        "medium",
                        List.of(
                            new ProductOptionValue("rare", Map.of("en", "Rare", "tr", "Az Pişmiş"), 0.0),
                            new ProductOptionValue("medium", Map.of("en", "Medium", "tr", "Orta"), 0.0),
                            new ProductOptionValue("well-done", Map.of("en", "Well Done", "tr", "Çok Pişmiş"), 0.0)
                        ),
                        true,
                        false
                    ),
                    new ProductOption(
                        "opt-extras",
                        Map.of("en", "Extras", "tr", "Ekstralar"),
                        List.of(
                            new ProductOptionValue("extra-cheese", Map.of("en", "Extra Cheese", "tr", "Ekstra Peynir"), 10.0),
                            new ProductOptionValue("bacon", Map.of("en", "Bacon", "tr", "Pastırma"), 15.0),
                            new ProductOptionValue("avocado", "Avocado", 12.0)
                        ),
                        false,
                        true
                    )
                )
            ),
            new Product(
                "prod-2",
                "French Fries",
                "Crispy golden fries",
                29.90,
                "TRY",
                new Category("cat-2", "Sides"),
                List.of(
                    new ProductOption(
                        "opt-size",
                        "Size",
                        "medium",
                        List.of(
                            new ProductOptionValue("small", "Small", -5.0),
                            new ProductOptionValue("medium", "Medium", 0.0),
                            new ProductOptionValue("large", "Large", 10.0)
                        ),
                        true,
                        false
                    )
                )
            ),
            new Product(
                "prod-3",
                Map.of("en", "Chicken Nuggets", "tr", "Tavuk Nugget"),
                Map.of("en", "Crispy chicken nuggets (6 pieces)", "tr", "Çıtır tavuk nugget (6 adet)"),
                45.00,
                new Category("cat-2", "Sides")
            ),
            new Product(
                "prod-4",
                Map.of("en", "Caesar Salad", "tr", "Sezar Salata"),
                "Fresh romaine lettuce with Caesar dressing",
                65.00,
                "TRY",
                new Category("cat-3", Map.of("en", "Salads", "tr", "Salatalar")),
                List.of(
                    new ProductOption(
                        "opt-protein",
                        "Add Protein",
                        List.of(
                            new ProductOptionValue("chicken", Map.of("en", "Grilled Chicken", "tr", "Izgara Tavuk"), 25.0),
                            new ProductOptionValue("shrimp", Map.of("en", "Shrimp", "tr", "Karides"), 35.0)
                        ),
                        false,
                        false
                    )
                )
            ),
            new Product(
                "prod-5",
                "Cola",
                "Refreshing cola",
                15.00,
                new Category("cat-4", Map.of("en", "Beverages", "tr", "İçecekler"))
            )
        );
    }

    private static List<Location> createLocations() {
        return List.of(
            new Location("loc-table-1", "Table 1"),
            new Location("loc-table-2", "Table 2"),
            new Location("loc-table-3", "Table 3"),
            new Location("loc-table-4", "Table 4"),
            new Location("loc-table-5", "Table 5"),
            new Location("loc-room-101", "Room 101"),
            new Location("loc-room-102", "Room 102"),
            new Location("loc-terrace", "Terrace")
        );
    }
}

class ConcurrentHashMap<K, V> extends java.util.concurrent.ConcurrentHashMap<K, V> {}
