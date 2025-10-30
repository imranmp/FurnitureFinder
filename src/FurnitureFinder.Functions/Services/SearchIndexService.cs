using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace FurnitureFinder.Functions.Services;

public class SearchIndexService(IOptions<SearchConfig> searchConfig,
                                ILogger<SearchIndexService> logger) : ISearchIndexService
{
    private readonly SearchClient _searchClient = new(new Uri(searchConfig.Value.Endpoint),
                                                      searchConfig.Value.IndexName,
                                                      new AzureKeyCredential(searchConfig.Value.Key));

    public async Task<List<Product>> GetProductsWithoutEmbeddingsAsync(int count, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchOptions = new SearchOptions
            {
                Filter = "vectorRetrieved eq false or vectorRetrieved eq null",
                Size = count
            };

            var searchResult = await _searchClient.SearchAsync<Product>("*", searchOptions, cancellationToken);

            var products = new List<Product>();

            await foreach (var result in searchResult.Value.GetResultsAsync()
                .AsPages(pageSizeHint: 50))
            {
                foreach (var product in result.Values)
                {
                    products.Add(product.Document);
                }
            }

            logger.LogInformation("Retrieved {Count} products without embeddings", products.Count);

            return products;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving products without embeddings");
            throw;
        }
    }

    public async Task MergeOrUploadProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        try
        {
            var batch = IndexDocumentsBatch.MergeOrUpload(products);
            IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            var failedDocuments = result.Results.Where(r => !r.Succeeded).ToList();
            foreach (var failed in failedDocuments)
            {
                logger.LogWarning("Failed to update document with key: {Key}, Error: {ErrorMessage}",
                    failed.Key, failed.ErrorMessage);
            }

            logger.LogInformation("Successfully updated {SuccessCount} products out of {TotalCount}",
                result.Results.Count(r => r.Succeeded), products.Count());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating products");
            throw;
        }
    }
}
