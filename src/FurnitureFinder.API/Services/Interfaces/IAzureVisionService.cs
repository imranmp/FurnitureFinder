namespace FurnitureFinder.API.Services.Interfaces;

public interface IAzureVisionService
{
    Task<AzureVisionResult> AnalyzeFurnitureAsync(byte[] imageData, CancellationToken cancellationToken = default);
}
