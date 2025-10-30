namespace FurnitureFinder.Functions.Services.Interfaces;

public interface IEmbeddingService
{
    /// <summary>
    /// Generate embeddings for the provided text using Azure OpenAI
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
