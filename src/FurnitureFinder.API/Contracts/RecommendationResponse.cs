namespace FurnitureFinder.API.Contracts;

public record RecommendationResponse(FurnitureAnalysisResult Analysis,
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
                                  List<double> Dimensions,
                                  string DetailCategory,
                                  string ReferenceColor,
                                  string ProductFactoryFinish,
                                  List<ProductAttribute> Attributes,
                                  string Material,
                                  string SKU,
                                  string PrimaryFinish,
                                  List<Asset> Assets);

public record ProductAttribute(string AttributeName,
                               string AttributeValues);

public record Asset(int? DivisionId,
                    string AssetId,
                    string AssetUsage,
                    string AssetType);
