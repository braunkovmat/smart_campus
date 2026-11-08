using System.Text.Json.Serialization;

namespace CampusMarket.Client.Models;

// Товар каталога (схема CatalogItem из openapi.yaml)
public class CatalogItem
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("category")] public string? Category { get; set; }
    [JsonPropertyName("price")] public double Price { get; set; }
    [JsonPropertyName("stock")] public int Stock { get; set; }
}

// Ответ GET /catalog/items (схема CatalogItemList)
public class CatalogItemList
{
    [JsonPropertyName("items")] public List<CatalogItem> Items { get; set; } = new();
    [JsonPropertyName("total")] public int Total { get; set; }
}
