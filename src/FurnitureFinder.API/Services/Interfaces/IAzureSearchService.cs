
namespace FurnitureFinder.API.Services.Interfaces;

public interface IAzureSearchService
{
    Task<(string, List<ProductSearchResult>)> FindComplementaryFurnitureAsync(AzureVisionResult azureVisionResult,
                                                                              string openAIConciseDescription,
                                                                              RecommendationRequest request,
                                                                              CancellationToken cancellationToken = default);
}
