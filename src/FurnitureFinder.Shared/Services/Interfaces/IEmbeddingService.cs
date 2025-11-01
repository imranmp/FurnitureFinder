namespace FurnitureFinder.Shared.Services.Interfaces;

public interface IEmbeddingService
{
    /// <summary>
    /// Generate embeddings for the given text
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
