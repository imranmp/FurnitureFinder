using Azure;
using Azure.AI.Vision.ImageAnalysis;

namespace FurnitureFinder.API.Services;

public class ComputerVisionService(IOptions<AzureConfiguration> configOptions)
    : IComputerVisionService
{
    private readonly ImageAnalysisClient _client = new(
            new Uri(configOptions.Value.ComputerVision.Endpoint),
            new AzureKeyCredential(configOptions.Value.ComputerVision.Key));

    public async Task<FurnitureAnalysisResult> AnalyzeFurnitureAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        var imageSource = BinaryData.FromBytes(imageData);

        var result = await _client.AnalyzeAsync(imageSource,
            VisualFeatures.Caption | VisualFeatures.Objects | VisualFeatures.Tags | VisualFeatures.DenseCaptions,
            cancellationToken: cancellationToken);

        return new FurnitureAnalysisResult
        {
            Description = result.Value.Caption.Text,
            Tags = result.Value.Tags.Values.Select(t => t.Name),
            OtherDescriptions = result.Value.DenseCaptions.Values.Select(t => t.Text)
        };
    }
}
