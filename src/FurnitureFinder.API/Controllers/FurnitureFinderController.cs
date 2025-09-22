using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FurnitureFinder.API.Controllers;

[ApiController]
[Route("[controller]")]
public class FurnitureFinderController(IFurnitureFinderService _furnitureFinderService,
                                       IValidator<RecommendationRequest> _validator) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<RecommendationResponse>> AnalyzeAndRecommend(
        [FromForm] RecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Execute FluentValidation
        await _validator.ValidateAndThrowAsync(request, cancellationToken: cancellationToken);

        RecommendationResponse results = await _furnitureFinderService.AnalyzeAndRecommendAsync(request, cancellationToken);

        return Ok(results);
    }
}
