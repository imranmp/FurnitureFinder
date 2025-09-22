namespace FurnitureFinder.API.Contracts;

public record RecommendationResponse(AzureVisionResult AzureVisionResult,
                                     string OpenAIDescription,
                                     string OpenAIConciseDescription,
                                     string SemanticQuery)
{
    public List<ProductSearchResult> Recommendations { get; set; } = [];
}

public record ProductSearchResult(string Id,
                                  string TopCategory,
                                  string Category,
                                  string AdvertisingCopy,
                                  string DescriptionTitle,
                                  string Decor,
                                  string Theme,
                                  string PrimaryColor,
                                  string DetailCategory,
                                  string ReferenceColor,
                                  string ProductFactoryFinish,
                                  string Material,
                                  string SKU,
                                  string PrimaryFinish);

public record AzureVisionResult(string Description)
{
    public IEnumerable<string> Tags { get; set; } = [];

    public IEnumerable<string> OtherDescriptions { get; set; } = [];
}