using Azure;
using Azure.AI.Vision.ImageAnalysis;

namespace FurnitureFinder.API.Services;

public class AzureVisionService(IOptions<VisionConfig> visionConfig)
    : IAzureVisionService
{
    private readonly ImageAnalysisClient _client = new(
            new Uri(visionConfig.Value.Endpoint),
            new AzureKeyCredential(visionConfig.Value.Key));

    public async Task<AzureVisionResult> AnalyzeFurnitureAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        var imageSource = BinaryData.FromBytes(imageData);

        var result = await _client.AnalyzeAsync(imageSource,
            VisualFeatures.Caption | VisualFeatures.Objects | VisualFeatures.Tags | VisualFeatures.DenseCaptions,
            cancellationToken: cancellationToken);

        return new AzureVisionResult
        {
            Description = result.Value.Caption.Text,
            Tags = result.Value.Tags.Values.Select(t => t.Name),
            OtherDescriptions = result.Value.DenseCaptions.Values.Select(t => t.Text)
        };
    }
}
