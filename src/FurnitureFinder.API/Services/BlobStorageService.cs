using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace FurnitureFinder.API.Services;

public class BlobStorageService(IOptions<BlobStorageConfig> blobStorageConfig)
    : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient = new(blobStorageConfig.Value.ConnectionString, blobStorageConfig.Value.ContainerName);

    public async Task<Uri> UploadImageAndGetSasUrlAsync(byte[] imageData,
                                                        string originalFileName,
                                                        TimeSpan sasExpiry,
                                                        IDictionary<string, string>? metadata = null,
                                                        CancellationToken cancellationToken = default)
    {
        if (imageData == null || imageData.Length == 0)
        {
            throw new ArgumentException("Image data is empty.", nameof(imageData));
        }

        string fileName = GenerateUniqueFileName(originalFileName);
        var blobClient = _containerClient.GetBlobClient(fileName);

        using var imageStream = new MemoryStream(imageData);

        var uploadOptions = new BlobUploadOptions
        {
            Metadata = metadata,
            Tags = new Dictionary<string, string>()
            {
                { "temporary", "true" }
            }
        };

        //Once expiry is GA, update to use that instead of setting up a lifecycle management policy

        await blobClient.UploadAsync(imageStream, uploadOptions, cancellationToken);

        return GenerateBlobSasUri(blobClient, sasExpiry);
    }

    private static string GenerateUniqueFileName(string originalFileName)
    {
        string extension = Path.GetExtension(originalFileName);
        return $"temp/{Guid.NewGuid()}{extension}";
    }

    private static Uri GenerateBlobSasUri(BlobClient blobClient, TimeSpan expiry)
    {
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder);
    }
}