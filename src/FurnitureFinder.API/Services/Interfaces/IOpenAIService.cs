namespace FurnitureFinder.API.Services.Interfaces;

public interface IOpenAIService
{
    Task<string> GetCompetitorProducts(FurnitureAnalysisResult analysis, Uri imageUrl, string conciseDescription, CancellationToken cancellationToken = default);

    Task<string> GetConciseDescription(FurnitureAnalysisResult analysis, Uri imageUrl, CancellationToken cancellationToken = default);

    Task<string> GetImageDescription(FurnitureAnalysisResult analysis, Uri imageUrl, CancellationToken cancellationToken = default);
}
