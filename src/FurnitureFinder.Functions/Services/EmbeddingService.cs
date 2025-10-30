using Azure;
using Azure.AI.OpenAI;
using OpenAI.Embeddings;

namespace FurnitureFinder.Functions.Services;

public class EmbeddingService(IOptions<OpenAIConfig> openAIConfig,
                              ILogger<EmbeddingService> logger) : IEmbeddingService
{
    private readonly AzureOpenAIClient _client = new(new Uri(openAIConfig.Value.Endpoint),
                                                     new AzureKeyCredential(openAIConfig.Value.Key));

    private readonly string _embeddingDeploymentName = openAIConfig.Value.EmbeddingDeploymentName;
    private readonly int _embeddingDimensions = openAIConfig.Value.EmbeddingDimensions;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var embeddingClient = _client.GetEmbeddingClient(_embeddingDeploymentName);

            var embeddingOptions = new EmbeddingGenerationOptions
            {
                Dimensions = _embeddingDimensions,
                EndUserId = "furniture-finder-app"
            };

            var embeddingResult = await embeddingClient.GenerateEmbeddingAsync(text, embeddingOptions, cancellationToken);

            return embeddingResult.Value.ToFloats().ToArray();

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating embedding for text: {Text}", text);
            throw;
        }
    }
}
