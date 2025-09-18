namespace FurnitureFinder.API.Services.Interfaces;

public interface ISearchService
{
    Task<(string, List<ProductSearchResult>)> FindComplementaryFurnitureAsync(
        FurnitureAnalysisResult analysis,
        RecommendationRequest request,
        CancellationToken cancellationToken = default);
}
