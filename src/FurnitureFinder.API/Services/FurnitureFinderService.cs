namespace FurnitureFinder.API.Services;

public class FurnitureFinderService(IAzureVisionService visionService,
                                    IAzureSearchService searchService,
                                    IAzureOpenAIService openAIService,
                                    IBlobStorageService blobStorageService) : IFurnitureFinderService
{
    public async Task<RecommendationResponse> AnalyzeAndRecommendAsync(RecommendationRequest request, CancellationToken cancellationToken)
    {
        // Step 1: Analyze the uploaded image
        byte[] imageData;
        using (var memoryStream = new MemoryStream())
        {
            await request.Image.CopyToAsync(memoryStream, cancellationToken);
            imageData = memoryStream.ToArray();
        }

        AzureVisionResult azureVisionResult = await visionService.AnalyzeFurnitureAsync(imageData, cancellationToken);

        // Step 2: store image in blob storage
        Uri imageUrl = await blobStorageService.UploadImageAndGetSasUrlAsync(imageData, request.Image.FileName, TimeSpan.FromHours(1), cancellationToken: cancellationToken);

        // Step 3: Call OpenAPI with results from VisionAI and the image url to get a better description
        var openAIDescription = await openAIService.GetImageDescription(azureVisionResult, imageUrl, cancellationToken);
        var openAIConciseDescription = await openAIService.GetConciseDescription(azureVisionResult, imageUrl, cancellationToken);

        // Step 4: Find complementary furniture
        (string semanticQuery, List<ProductSearchResult> recommendations) =
            await searchService.FindComplementaryFurnitureAsync(azureVisionResult, openAIConciseDescription, request, cancellationToken);

        // TODO: Step 5: Generate Image for the recommendations (Optional)

        return new RecommendationResponse(azureVisionResult, openAIDescription, openAIConciseDescription, semanticQuery)
        {
            Recommendations = recommendations
        };
    }
}
