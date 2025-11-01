using Microsoft.AspNetCore.Mvc;
using FurnitureFinder.Shared.Services.Interfaces;

namespace FurnitureFinder.API.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController(ISearchIndexService _searchIndexService)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Create(CancellationToken cancellationToken = default)
    {
        await _searchIndexService.CreateIndexAsync(cancellationToken);

        return Created();
    }


    [HttpPost("seed")]
    public async Task<ActionResult> Populate(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        await _searchIndexService.MergeOrUploadProductsAsync(products, cancellationToken);

        return Ok();
    }
}
