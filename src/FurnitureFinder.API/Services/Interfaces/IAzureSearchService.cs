namespace FurnitureFinder.API.Services.Interfaces;

public interface IAzureSearchService
{
    Task<(string, List<ProductSearchResult>)> FindComplementaryFurnitureAsync(
        FurnitureAnalysisResult analysis,
        RecommendationRequest request,
        CancellationToken cancellationToken = default);
}
