using System.Collections.Concurrent;

namespace HeyHoloHpiExample;

public static class Database
{
    private static readonly ConcurrentDictionary<string, Order> Orders = new();

    private static readonly List<Product> Products = new()
    {
        new Product
        {
            Id = "prod-1",
            Name = new Dictionary<string, string>
            {
                { "en", "Classic Burger" },
                { "tr", "Klasik Burger" }
            },
            Description = new Dictionary<string, string>
            {
                { "en", "Juicy beef patty with lettuce, tomato, and our special sauce" },
                { "tr", "Marul, domates ve özel sosumuzla sulu dana köftesi" }
            },
            Ingredients = new Dictionary<string, string>
            {
                { "en", "Beef, Lettuce, Tomato, Special Sauce, Bun" },
                { "tr", "Dana Eti, Marul, Domates, Özel Sos, Ekmek" }
            },
            Price = 89.90m,
            Currency = "TRY",
            Category = new Category
            {
                Id = "cat-1",
                Name = new Dictionary<string, string>
                {
                    { "en", "Burgers" },
                    { "tr", "Burgerler" }
                }
            },
            Options = new List<ProductOption>
            {
                new ProductOption
                {
                    Name = "opt-doneness",
                    Label = new Dictionary<string, string>
                    {
                        { "en", "Doneness" },
                        { "tr", "Pişirme Derecesi" }
                    },
                    Default = "medium",
                    Required = true,
                    Multiple = false,
                    Options = new List<ProductOptionValue>
                    {
                        new ProductOptionValue
                        {
                            Value = "rare",
                            Label = new Dictionary<string, string>
                            {
                                { "en", "Rare" },
                                { "tr", "Az Pişmiş" }
                            },
                            AdditionalPrice = 0
                        },
                        new ProductOptionValue
                        {
                            Value = "medium",
                            Label = new Dictionary<string, string>
                            {
                                { "en", "Medium" },
                                { "tr", "Orta" }
                            },
                            AdditionalPrice = 0
                        },
                        new ProductOptionValue
                        {
                            Value = "well-done",
                            Label = new Dictionary<string, string>
                            {
                                { "en", "Well Done" },
                                { "tr", "Çok Pişmiş" }
                            },
                            AdditionalPrice = 0
                        }
                    }
                },
                new ProductOption
                {
                    Name = "opt-extras",
                    Label = new Dictionary<string, string>
                    {
                        { "en", "Extras" },
                        { "tr", "Ekstralar" }
                    },
                    Required = false,
                    Multiple = true,
                    Options = new List<ProductOptionValue>
                    {
                        new ProductOptionValue
                        {
                            Value = "extra-cheese",
                            Label = new Dictionary<string, string>
                            {
                                { "en", "Extra Cheese" },
                                { "tr", "Ekstra Peynir" }
                            },
                            AdditionalPrice = 10.00m
                        },
                        new ProductOptionValue
                        {
                            Value = "bacon",
                            Label = new Dictionary<string, string>
                            {
                                { "en", "Bacon" },
                                { "tr", "Pastırma" }
                            },
                            AdditionalPrice = 15.00m
                        },
                        new ProductOptionValue
                        {
                            Value = "avocado",
                            Label = "Avocado",
                            AdditionalPrice = 12.00m
                        }
                    }
                }
            },
            CrossSellIds = new List<string> { "prod-2", "prod-5" }
        },
        new Product
        {
            Id = "prod-2",
            Name = "French Fries",
            Description = "Crispy golden fries",
            Price = 29.90m,
            Currency = "TRY",
            Category = new Category
            {
                Id = "cat-2",
                Name = "Sides"
            },
            Options = new List<ProductOption>
            {
                new ProductOption
                {
                    Name = "opt-size",
                    Label = "Size",
                    Default = "medium",
                    Required = true,
                    Multiple = false,
                    Options = new List<ProductOptionValue>
                    {
                        new ProductOptionValue
                        {
                            Value = "small",
                            Label = "Small",
                            AdditionalPrice = -5.00m
                        },
                        new ProductOptionValue
                        {
                            Value = "medium",
                            Label = "Medium",
                            AdditionalPrice = 0
                        },
                        new ProductOptionValue
                        {
                            Value = "large",
                            Label = "Large",
                            AdditionalPrice = 10.00m
                        }
                    }
                }
            }
        },
        new Product
        {
            Id = "prod-3",
            Name = new Dictionary<string, string>
            {
                { "en", "Chicken Nuggets" },
                { "tr", "Tavuk Nugget" }
            },
            Description = new Dictionary<string, string>
            {
                { "en", "Crispy chicken nuggets (6 pieces)" },
                { "tr", "Çıtır tavuk nugget (6 adet)" }
            },
            Price = 45.00m,
            Currency = "TRY",
            Category = new Category
            {
                Id = "cat-2",
                Name = "Sides"
            }
        },
        new Product
        {
            Id = "prod-4",
            Name = new Dictionary<string, string>
            {
                { "en", "Caesar Salad" },
                { "tr", "Sezar Salata" }
            },
            Description = "Fresh romaine lettuce with Caesar dressing",
            Price = 65.00m,
            Currency = "TRY",
            Category = new Category
            {
                Id = "cat-3",
                Name = new Dictionary<string, string>
                {
                    { "en", "Salads" },
                    { "tr", "Salatalar" }
                }
            },
            Options = new List<ProductOption>
            {
                new ProductOption
                {
                    Name = "opt-protein",
                    Label = "Add Protein",
                    Required = false,
                    Multiple = false,
                    Options = new List<ProductOptionValue>
                    {
                        new ProductOptionValue
                        {
                            Value = "chicken",
                            Label = new Dictionary<string, string>
                            {
                                { "en", "Grilled Chicken" },
                                { "tr", "Izgara Tavuk" }
                            },
                            AdditionalPrice = 25.00m
                        },
                        new ProductOptionValue
                        {
                            Value = "shrimp",
                            Label = new Dictionary<string, string>
                            {
                                { "en", "Shrimp" },
                                { "tr", "Karides" }
                            },
                            AdditionalPrice = 35.00m
                        }
                    }
                }
            }
        },
        new Product
        {
            Id = "prod-5",
            Name = "Cola",
            Description = "Refreshing cola",
            Price = 15.00m,
            Currency = "TRY",
            Unit = "330ml",
            Category = new Category
            {
                Id = "cat-4",
                Name = new Dictionary<string, string>
                {
                    { "en", "Beverages" },
                    { "tr", "İçecekler" }
                }
            }
        }
    };

    private static readonly List<Location> Locations = new()
    {
        new Location { Id = "loc-table-1", Name = "Table 1" },
        new Location { Id = "loc-table-2", Name = "Table 2" },
        new Location { Id = "loc-table-3", Name = "Table 3" },
        new Location { Id = "loc-table-4", Name = "Table 4" },
        new Location { Id = "loc-table-5", Name = "Table 5" },
        new Location { Id = "loc-room-101", Name = "Room 101" },
        new Location { Id = "loc-room-102", Name = "Room 102" },
        new Location { Id = "loc-terrace", Name = "Terrace" }
    };

    public static List<Product> GetProducts() => Products;

    public static void SaveOrder(Order order)
    {
        Orders[order.Id] = order;
    }

    public static Order? GetOrder(string id)
    {
        Orders.TryGetValue(id, out var order);
        return order;
    }

    public static List<Location> GetLocations() => Locations;
}
