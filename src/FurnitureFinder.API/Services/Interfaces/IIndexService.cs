namespace FurnitureFinder.API.Services.Interfaces;

public interface IIndexService
{
    Task CreateIndexAsync(CancellationToken cancellationToken = default);

    Task MergeOrUploadProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);
}