using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;

namespace FurnitureFinder.API.Controllers;

[ApiController]
[Route("[controller]")]
public class FurnitureFinderController : ControllerBase
{
    private readonly ILogger<FurnitureFinderController> _logger;
    private readonly IComputerVisionService _visionService;
    private readonly ISearchService _searchService;
    private readonly IOpenAIService _openAIService;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;
    private readonly AzureConfiguration _azureConfiguration;

    public FurnitureFinderController(ILogger<FurnitureFinderController> logger,
        IComputerVisionService visionService,
        ISearchService searchService,
        IOpenAIService openAIService,
        IOptions<AzureConfiguration> configOptions)
    {
        _logger = logger;
        _visionService = visionService;
        _searchService = searchService;
        _openAIService = openAIService;

        _azureConfiguration = configOptions.Value;

        _blobServiceClient = new BlobServiceClient(_azureConfiguration.BlobStorage.ConnectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient(_azureConfiguration.BlobStorage.ContainerName);
    }

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

            var analysis = await _visionService.AnalyzeFurnitureAsync(imageData);

            // Step 2: store image in blob storage
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.Image.FileName);
            using var imageStream = new MemoryStream(imageData);
            var uploadResult = await _containerClient.UploadBlobAsync(fileName, imageStream);

            // Get a reference to the uploaded blob
            var blobClient = _containerClient.GetBlobClient(fileName);

            // Create a SAS token valid for 1 hour
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = fileName,
                Resource = "b", // b = blob
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Allow read access
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            analysis.ImageUrl = blobClient.GenerateSasUri(sasBuilder);

            // Step 3: Call OpenAPI with results from VisionAI and the image url to get a better description
            analysis.OpenAIDescription = await _openAIService.GetImageDescription(analysis, analysis.ImageUrl);
            analysis.OpenAIConciseDescription = await _openAIService.GetConciseDescription(analysis, analysis.ImageUrl);
            analysis.CompetitorProductsString = await _openAIService.GetCompetitorProducts(analysis, analysis.ImageUrl, analysis.OpenAIConciseDescription);

            if (!string.IsNullOrWhiteSpace(analysis.CompetitorProductsString))
            {
                try
                {
                    var products = JsonSerializer.Deserialize<CompetitorProduct[]>(
                        analysis.CompetitorProductsString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    analysis.CompetitorProducts = products ?? [];
                }
                catch (JsonException)
                {
                    analysis.CompetitorProducts = [];
                }
            }

            // Step 4: Find complementary furniture
            (string semanticQuery, List<ProductSearchResult> recommendations) recommendation = await _searchService.FindComplementaryFurnitureAsync(analysis, request);

            return Ok(
                new RecommendationResponse(analysis, recommendation.semanticQuery)
                {
                    Recommendations = recommendation.recommendations
                }
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error processing request: {ex.Message}");
        }
    }
}
