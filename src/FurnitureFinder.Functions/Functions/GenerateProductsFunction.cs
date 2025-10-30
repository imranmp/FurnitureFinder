using Microsoft.Azure.Functions.Worker;

namespace FurnitureFinder.Functions.Functions;

public class GenerateProductsFunction(IProductGeneratorService productGeneratorService,
                                      ISearchIndexService searchIndexService,
                                      ILogger<GenerateProductsFunction> logger)
{
    [Function("GenerateProductsFunction")]
    public async Task Run([TimerTrigger("%GenerateProductsSchedule%")] TimerInfo timerInfo, CancellationToken cancellationToken)
    {
        logger.LogInformation("GenerateProductsFunction started at: {Time}", DateTime.UtcNow);

        try
        {
            // Generate 5 new furniture products
            var products = await productGeneratorService.GenerateProductsAsync(5, cancellationToken);

            if (products.Count == 0)
            {
                logger.LogWarning("No products were generated");
                return;
            }

            logger.LogInformation("Generated {Count} new furniture products", products.Count);

            // Upload products to the search index
            await searchIndexService.MergeOrUploadProductsAsync(products, cancellationToken);

            logger.LogInformation("Successfully uploaded {Count} new products to search index", products.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GenerateProductsFunction");
            throw;
        }

        logger.LogInformation("GenerateProductsFunction completed at: {Time}", DateTime.UtcNow);
    }
}
