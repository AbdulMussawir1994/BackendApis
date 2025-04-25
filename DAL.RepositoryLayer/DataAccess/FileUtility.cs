using DAL.RepositoryLayer.IDataAccess;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace DAL.RepositoryLayer.DataAccess;

public class FileUtility : IFileUtility
{
    private readonly ConfigHandler _configHandler;
    private readonly IWebHostEnvironment _env;


    private static readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".gif", ".pdf", ".doc", ".docx", ".mp4", ".mp3", ".png", ".xlsx", ".xls", ".txt" };
    const long maxAllowedSize = 5 * 1024 * 1024; // 5 MB

    public FileUtility(IWebHostEnvironment env, ConfigHandler configHandler)
    {
        _env = env;
        _configHandler = configHandler;
    }

    public string WebRoot => _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

    public bool IsExtensionAllowed(string extension) => _allowedExtensions.Contains(extension.ToLowerInvariant());

    public async Task<MobileResponse<string>> SaveFileInternalAsync(IFormFile file, string folderName)
    {
        var response = new MobileResponse<string>(_configHandler, "FilesService");

        if (file == null || file.Length == 0)
            return response.SetError("ERR-400", "Invalid file.", null);

        const long maxAllowedSize = 5 * 1024 * 1024;
        if (file.Length > maxAllowedSize)
            return response.SetError("ERR-400", "File size exceeds 5MB.", null);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!IsExtensionAllowed(extension))
            return response.SetError("ERR-400", $"File extension '{extension}' is not allowed.", null);

        try
        {
            var safeName = Path.GetFileNameWithoutExtension(file.FileName).Replace(" ", "_");
            var fileName = $"{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";
            var folderPath = Path.Combine(WebRoot, folderName);

            Directory.CreateDirectory(folderPath);
            var fullPath = Path.Combine(folderPath, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream);

            var relativePath = Path.Combine(folderName, fileName).Replace("\\", "/");
            return response.SetSuccess("SUCCESS-200", "File saved successfully.", relativePath);
        }
        catch (Exception ex)
        {
            return response.SetError("ERR-500", $"Failed to save file: {ex.Message}", null);
        }
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
