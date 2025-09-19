using Microsoft.AspNetCore.Mvc;

namespace FurnitureFinder.API.Controllers;

[ApiController]
[Route("[controller]")]
public class FurnitureFinderController(ILogger<FurnitureFinderController> _logger,
                                       IAzureVisionService _visionService,
                                       IAzureSearchService _searchService,
                                       IAzureOpenAIService _openAIService,
                                       IBlobStorageService _blobStorageService)
    : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<RecommendationResponse>> AnalyzeAndRecommend(
        [FromForm] RecommendationRequest request)
    {
        try
        {
            if (request.Image == null || request.Image.Length == 0)
            {
                return BadRequest("Image is required");
            }

            // Step 1: Analyze the uploaded image
            byte[] imageData;
            using (var memoryStream = new MemoryStream())
            {
                await request.Image.CopyToAsync(memoryStream);
                imageData = memoryStream.ToArray();
            }

            AzureVisionResult azureVisionResult = await _visionService.AnalyzeFurnitureAsync(imageData);

            // Step 2: store image in blob storage
            Uri imageUrl = await _blobStorageService.UploadImageAndGetSasUrlAsync(imageData, request.Image.FileName, TimeSpan.FromHours(1));

            // Step 3: Call OpenAPI with results from VisionAI and the image url to get a better description
            FurnitureAnalysisResult analysisResult = new()
            {
                AzureVisionResult = azureVisionResult,
                OpenAIDescription = await _openAIService.GetImageDescription(azureVisionResult, imageUrl),
                OpenAIConciseDescription = await _openAIService.GetConciseDescription(azureVisionResult, imageUrl)
            };

            // Step 4: Find complementary furniture
            (string semanticQuery, List<ProductSearchResult> recommendations) = await _searchService.FindComplementaryFurnitureAsync(analysisResult, request);

            return Ok(
                new RecommendationResponse(analysisResult, semanticQuery)
                {
                    Recommendations = recommendations
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing furniture recommendation request.");
            return StatusCode(500, $"Error processing request: {ex.Message}");
        }
    }
}
