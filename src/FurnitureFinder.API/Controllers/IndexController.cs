using Microsoft.AspNetCore.Mvc;

namespace FurnitureFinder.API.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController(IIndexService _indexService)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Create(CancellationToken cancellationToken = default)
    {
        await _indexService.CreateIndexAsync(cancellationToken);

        return Created();
    }


    [HttpPost("seed")]
    public async Task<ActionResult> Populate(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        await _indexService.MergeOrUploadProductsAsync(products, cancellationToken);

        return Ok();
    }
}
