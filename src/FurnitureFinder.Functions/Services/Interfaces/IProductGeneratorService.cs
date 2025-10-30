namespace FurnitureFinder.Functions.Services.Interfaces;

public interface IProductGeneratorService
{
    /// <summary>
    /// Generate synthetic furniture products using AI
    /// </summary>
    Task<List<Product>> GenerateProductsAsync(int count, CancellationToken cancellationToken = default);
}
