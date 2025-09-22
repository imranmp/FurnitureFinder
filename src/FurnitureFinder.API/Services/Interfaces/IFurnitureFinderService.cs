
namespace FurnitureFinder.API.Services.Interfaces;

public interface IFurnitureFinderService
{
    Task<RecommendationResponse> AnalyzeAndRecommendAsync(RecommendationRequest request, CancellationToken cancellationToken);
}
