using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace BackendApis.Helpers;


public static class Base64ImageConverter
{
    public static async Task<string> SaveBase64ImageAsync(string base64Data, string fileName, string folderName, string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
            throw new ArgumentException("Base64 string is empty.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg"; // Default extension

        // Validate allowed extensions
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        if (!allowedExtensions.Contains(extension.ToLowerInvariant()))
            throw new InvalidOperationException($"File extension '{extension}' is not allowed.");

        // Prepare and clean base64 string
        var base64Content = base64Data.Contains(",") ? base64Data.Split(',')[1] : base64Data;
        var fileBytes = Convert.FromBase64String(base64Content);

        // Validate file size (example: Max 5MB)
        const int maxFileSize = 5 * 1024 * 1024;
        if (fileBytes.Length > maxFileSize)
            throw new InvalidOperationException("File size exceeds 5MB limit.");

        // Load image
        using var image = Image.Load(fileBytes);

        // ✅ Resize image if width is larger than 1024px
        if (image.Width > 1024)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(1024, 0) // Maintain aspect ratio
            }));
        }

        var safeName = Path.GetFileNameWithoutExtension(fileName)?.Replace(" ", "_") ?? Guid.NewGuid().ToString();
        var generatedFileName = $"{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";
        var folderPath = Path.Combine(webRootPath, folderName);

        Directory.CreateDirectory(folderPath); // Ensure folder exists

        var fullPath = Path.Combine(folderPath, generatedFileName);

        // Save image with compression (JPEG quality = 80%)
        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            await image.SaveAsync(fullPath, new JpegEncoder { Quality = 80 });
        }
        else
        {
            await image.SaveAsync(fullPath); // Default for PNG, GIF
        }

        // Return relative path
        return Path.Combine(folderName, generatedFileName).Replace("\\", "/");
    }
}