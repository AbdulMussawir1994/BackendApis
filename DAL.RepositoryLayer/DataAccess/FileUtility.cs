using DAL.RepositoryLayer.IDataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace DAL.RepositoryLayer.DataAccess;

public class FileUtility : IFileUtility
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".gif", ".pdf", ".doc", ".docx", ".mp4", ".mp3", ".png", ".xlsx", ".xls", ".txt" };

    public FileUtility(IWebHostEnvironment env) => _env = env;

    public string WebRoot => _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

    public bool IsExtensionAllowed(string extension) => _allowedExtensions.Contains(extension.ToLowerInvariant());

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Invalid file.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!IsExtensionAllowed(extension))
            throw new InvalidOperationException("File extension is not allowed.");

        var safeName = Path.GetFileNameWithoutExtension(file.FileName).Replace(" ", "_");
        var fileName = $"{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";
        var folderPath = Path.Combine(WebRoot, folderName);

        Directory.CreateDirectory(folderPath);
        var fullPath = Path.Combine(folderPath, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream);

        return Path.Combine(folderName, fileName).Replace("\\", "/");
    }

    public string ResolveAbsolutePath(string relativePath)
    {
        var sanitizedPath = Uri.UnescapeDataString(relativePath).TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        return Path.Combine(WebRoot, sanitizedPath);
    }

    public string GetContentType(string path)
    {
        var provider = new FileExtensionContentTypeProvider();
        return provider.TryGetContentType(path, out var contentType) ? contentType : "application/octet-stream";
    }
}
