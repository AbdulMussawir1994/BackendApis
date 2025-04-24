using DAL.RepositoryLayer.IDataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace DAL.RepositoryLayer.DataAccess;

// FileUtility.cs - Clean Utility Class

public class FileUtility : IFileUtility
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".gif", ".pdf", ".doc", ".docx", ".mp4", ".mp3", ".png", ".xlsx", ".xls", ".txt" };

    public FileUtility(IWebHostEnvironment env) => _env = env;

    public string WebRoot => Path.Combine(
        _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
        "images"
    );

    public bool IsExtensionAllowed(string extension) => _allowedExtensions.Contains(extension.ToLowerInvariant());

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Invalid file.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!IsExtensionAllowed(ext))
            throw new InvalidOperationException("File extension is not allowed.");

        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}";
        var uploadPath = Path.Combine(WebRoot, "files", folderName);

        Directory.CreateDirectory(uploadPath);
        var fullPath = Path.Combine(uploadPath, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream);

        return $"/files/{folderName}/{fileName}".Replace("\\", "/");
    }

    public string ResolveAbsolutePath(string relativePath)
    {
        var sanitized = relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        return Path.Combine(WebRoot, sanitized);
    }

    public string GetContentType(string path)
    {
        var provider = new FileExtensionContentTypeProvider();
        return provider.TryGetContentType(path, out var contentType) ? contentType : "application/octet-stream";
    }
}