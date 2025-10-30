using Microsoft.Azure.Functions.Worker;

namespace FurnitureFinder.Functions.Functions;

public class UpdateEmbeddingsFunction(ISearchIndexService searchIndexService,
                                      IEmbeddingService embeddingService,
                                      ILogger<UpdateEmbeddingsFunction> logger)
{
    [Function("UpdateEmbeddingsFunction")]
    public async Task Run([TimerTrigger("%UpdateEmbeddingsSchedule%")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        logger.LogInformation("UpdateEmbeddingsFunction started at: {Time}", DateTime.UtcNow);

        try
        {
            // Get 20 products without embeddings
            var products = await searchIndexService.GetProductsWithoutEmbeddingsAsync(20, cancellationToken);

            if (products.Count == 0)
            {
                logger.LogInformation("No products found without embeddings");
                return;
            }

            logger.LogInformation("Processing {Count} products to generate embeddings", products.Count);

            // Generate embeddings for each product
            await Parallel.ForEachAsync(products, cancellationToken, async (product, ct) =>
            {
                try
                {
                    // Generate embedding for the product summary
                    var embedding = await embeddingService.GenerateEmbeddingAsync(product.ProductSummary, ct);

                    // Update product with embedding
                    product.ProductSummaryVector = embedding;
                    product.VectorRetrieved = true;

                    logger.LogInformation("Generated embedding for product: {ProductId} - {ProductName}", product.Id, product.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error generating embedding for product: {ProductId}", product.Id);
                }
            });

            // Update products in the search index
            var productsToUpdate = products.Where(p => p.VectorRetrieved ?? false).ToList();
            if (productsToUpdate.Any())
            {
                await searchIndexService.MergeOrUploadProductsAsync(productsToUpdate, cancellationToken);
                logger.LogInformation("Successfully updated {Count} products with embeddings", productsToUpdate.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateEmbeddingsFunction");
            throw;
        }

        logger.LogInformation("UpdateEmbeddingsFunction completed at: {Time}", DateTime.UtcNow);
    }
}
