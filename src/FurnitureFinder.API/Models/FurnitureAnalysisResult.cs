namespace FurnitureFinder.API.Models;

public class FurnitureAnalysisResult
{
    public required AzureVisionResult AzureVisionResult { get; set; }

    public required string OpenAIDescription { get; set; }

    public required string OpenAIConciseDescription { get; set; }
}
