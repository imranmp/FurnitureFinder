using System.Text.Json.Serialization;

namespace FurnitureFinder.Functions.Models;

public class Product
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("sku")]
    public required string SKU { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("price")]
    public double Price { get; set; } = Random.Shared.NextDouble() * 3000;

    [JsonPropertyName("category")]
    public required string Category { get; set; }

    [JsonPropertyName("subcategory")]
    public required string Subcategory { get; set; }

    [JsonPropertyName("style")]
    public string[] Style { get; set; } = [];

    [JsonPropertyName("colors")]
    public required Colors Colors { get; set; }

    [JsonPropertyName("materials")]
    public string[] Materials { get; set; } = [];

    [JsonPropertyName("room_types")]
    public string[] RoomTypes { get; set; } = [];

    [JsonPropertyName("features")]
    public string[] Features { get; set; } = [];

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = [];

    [JsonPropertyName("colorKeywords")]
    public string[] ColorKeywords => GenerateColorsCollection();

    [JsonPropertyName("productSummary")]
    public string ProductSummary => GenerateProductSummary();

    [JsonPropertyName("productSummaryVector")]
    public float[] ProductSummaryVector { get; set; } = [];

    [JsonPropertyName("vectorRetrieved")]
    public bool? VectorRetrieved { get; set; }

    private string[] GenerateColorsCollection()
    {
        var results = new List<string>
        {
            Colors?.Primary ?? "",
            Colors?.Secondary ?? ""
        }
        .Concat(Colors?.AllColors ?? [])
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .Distinct()
        .ToList();

        return [.. results];
    }

    private string GenerateProductSummary()
    {
        var summary = new System.Text.StringBuilder();

        summary.AppendLine($"{Name}. {Description}");
        summary.AppendLine($"Category: {Category} > {Subcategory}.");
        summary.AppendLine($"Style: {string.Join(", ", Style)}.");
        summary.AppendLine($"Colors: {string.Join(", ", Colors?.AllColors ?? [])}.");
        summary.AppendLine($"Materials: {string.Join(", ", Materials)}.");
        summary.AppendLine($"Suitable for: {string.Join(", ", RoomTypes)}.");
        summary.AppendLine($"Features: {string.Join(", ", Features)}.");
        summary.AppendLine($"Tags: {string.Join(", ", Tags)}.");

        return summary.ToString().Trim();
    }
}

public class Colors
{
    [JsonPropertyName("primary")]
    public required string Primary { get; set; }

    [JsonPropertyName("secondary")]
    public string? Secondary { get; set; }

    [JsonPropertyName("all_colors")]
    public string[] AllColors { get; set; } = [];
}
