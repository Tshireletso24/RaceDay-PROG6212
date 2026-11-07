namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    /// <summary>Uploads an image to Azure Blob Storage and returns its public URL.</summary>
    /// <param name="folder">A folder prefix inside the container, e.g. "events" or "profiles".</param>
    Task<string> UploadImageAsync(Stream content, string fileName, string contentType, string folder);
}
