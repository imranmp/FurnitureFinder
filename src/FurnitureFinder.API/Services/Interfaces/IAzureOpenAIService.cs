namespace FurnitureFinder.API.Services.Interfaces;

public interface IAzureOpenAIService
{
    Task<string> GetCompetitorProducts(FurnitureAnalysisResult analysis, Uri imageUrl, string conciseDescription, CancellationToken cancellationToken = default);

    Task<string> GetConciseDescription(AzureVisionResult analysis, Uri imageUrl, CancellationToken cancellationToken = default);

    Task<string> GetImageDescription(AzureVisionResult analysis, Uri imageUrl, CancellationToken cancellationToken = default);
}
