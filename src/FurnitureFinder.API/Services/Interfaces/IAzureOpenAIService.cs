namespace FurnitureFinder.API.Services.Interfaces;

public interface IAzureOpenAIService
{
    Task<string> GetConciseDescription(AzureVisionResult analysis, Uri imageUrl, CancellationToken cancellationToken = default);

    Task<string> GetImageDescription(AzureVisionResult analysis, Uri imageUrl, CancellationToken cancellationToken = default);
}
