namespace RaceDay.Api.Services;

public static class ImageValidator
{
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };

    /// <summary>Returns an error message, or null if the file is a valid image upload.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return "Please choose an image file.";
        if (file.Length > MaxBytes)
            return "The image must be 5 MB or smaller.";
        if (!AllowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return "Only JPG, PNG, WebP or GIF images are allowed.";
        return null;
    }
}
