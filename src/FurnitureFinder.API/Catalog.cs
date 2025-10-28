using System.Text.Json.Serialization;

namespace FurnitureFinder.API;

public class Catalog
{
    public required List<Product> Products { get; set; } = [];
}

public class Product
{
    public required string Id { get; set; }

    public required string SKU { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public double Price { get; set; } = Random.Shared.NextDouble() * 3000;

    public required string Category { get; set; }

    public required string Subcategory { get; set; }

    public string[] Style { get; set; } = [];

    public required Colors Colors { get; set; }

    public string[] Materials { get; set; } = [];

    [JsonPropertyName("room_types")]
    public string[] RoomTypes { get; set; } = [];

    public string[] Features { get; set; } = [];

    public string[] Tags { get; set; } = [];

    public string[] ColorKeywords => GenerateColorsCollection();

    public string ProductSummary => GenerateProductSummary();

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
        var summary = new StringBuilder();

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
    public required string Primary { get; set; }

    public string? Secondary { get; set; }

    [JsonPropertyName("all_colors")]
    public string[] AllColors { get; set; } = [];
}
