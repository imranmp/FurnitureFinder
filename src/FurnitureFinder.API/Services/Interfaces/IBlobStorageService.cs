namespace FurnitureFinder.API.Services.Interfaces;

public interface IBlobStorageService
{
    Task<Uri> UploadImageAndGetSasUrlAsync(byte[] imageData,
                                           string originalFileName,
                                           TimeSpan sasExpiry,
                                           IDictionary<string, string>? metadata = null,
                                           CancellationToken cancellationToken = default);
}