using FurnitureFinder.Shared.Models;

namespace FurnitureFinder.Shared.Services.Interfaces;

public interface ISearchIndexService
{
    /// <summary>
    /// Create or update the search index with the specified configuration
    /// </summary>
    Task CreateIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get products from the search index that are missing vector embeddings
    /// </summary>
    Task<List<Product>> GetProductsWithoutEmbeddingsAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update products in the search index
    /// </summary>
    Task MergeOrUploadProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);
}
