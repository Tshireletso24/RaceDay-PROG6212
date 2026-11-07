using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Stores images in Azure Blob Storage. The connection string comes from configuration
/// (user-secrets locally, an environment variable elsewhere) and is never committed to GitHub.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _config;
    private BlobContainerClient? _container;

    public BlobStorageService(IConfiguration config) => _config = config;

    private async Task<BlobContainerClient> GetContainerAsync()
    {
        if (_container != null) return _container;

        var connectionString = _config["AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("AzureBlob:ConnectionString is not configured.");

        var containerName = _config["AzureBlob:ContainerName"] ?? "raceday-images";
        var container = new BlobContainerClient(connectionString, containerName);

        // Public read access for blobs, so the browser can display the images by URL
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        _container = container;
        return container;
    }

    public async Task<string> UploadImageAsync(Stream content, string fileName, string contentType, string folder)
    {
        var container = await GetContainerAsync();

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
        return blob.Uri.ToString();
    }
}
