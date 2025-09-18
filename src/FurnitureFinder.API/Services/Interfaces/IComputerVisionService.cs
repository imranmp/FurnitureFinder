namespace FurnitureFinder.API.Services.Interfaces;

public interface IComputerVisionService
{
    Task<FurnitureAnalysisResult> AnalyzeFurnitureAsync(byte[] imageData, CancellationToken cancellationToken = default);
}
