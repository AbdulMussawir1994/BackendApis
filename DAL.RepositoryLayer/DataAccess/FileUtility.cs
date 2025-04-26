using DAL.DatabaseLayer.ViewModels;
using DAL.RepositoryLayer.IDataAccess;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using System.Globalization;

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

            // Fallback to current directory + wwwroot if WebRootPath is null
            var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folderPath = Path.Combine(rootPath, folderName);

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

    public async Task<MobileResponse<string>> SaveBase64FileAsync(string base64String, string fileName, string folderName)
    {
        var response = new MobileResponse<string>(_configHandler, "FilesService");

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg"; // Default extension

        if (!IsExtensionAllowed(extension))
            return response.SetError("ERR-400", $"File extension '{extension}' is not allowed.", null);

        // Remove "data:image/png;base64," if exists
        var base64Content = base64String.Contains(",") ? base64String[(base64String.IndexOf(",") + 1)..] : base64String;

        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(base64Content);
        }
        catch (FormatException)
        {
            return response.SetError("ERR-400", "Invalid Base64 string format.", null);
        }

        // ✅ Validate file size
        const int maxFileSizeInBytes = 5 * 1024 * 1024;
        if (fileBytes.Length > maxFileSizeInBytes)
            return response.SetError("ERR-400", "File size exceeds 5MB limit.", null);

        var safeName = Path.GetFileNameWithoutExtension(fileName)?.Replace(" ", "_") ?? Guid.NewGuid().ToString();
        var generatedFileName = $"{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";
        var folderPath = Path.Combine(WebRoot, folderName);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fullPath = Path.Combine(folderPath, generatedFileName);

        await File.WriteAllBytesAsync(fullPath, fileBytes);

        var relativePath = Path.Combine(folderName, generatedFileName).Replace("\\", "/");

        return response.SetSuccess("SUCCESS-200", "File saved successfully.", relativePath);
    }
    public async Task<string> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null.", nameof(file));

        // Fallback to current directory + wwwroot if WebRootPath is null
        var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadPath = Path.Combine(rootPath, folder);

        Directory.CreateDirectory(uploadPath); // Safe and idempotent

        var safeOriginalName = GetSafeFileName(Path.GetFileNameWithoutExtension(file.FileName));
        var extension = Path.GetExtension(file.FileName);
        var timestamp = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var fileName = $"{safeOriginalName}-{timestamp}{extension}";

        var fullPath = Path.Combine(uploadPath, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        // Return relative path (for front-end or storage references)
        return $"/{folder}/{fileName}".Replace("\\", "/");
    }

    public async Task<MobileResponse<object>> UploadPhysicalImageAndConvertToBase64Async(UploadPhysicalImageViewModel model)
    {
        var response = new MobileResponse<object>(_configHandler, "FilesService");

        if (model.ImageFile == null || model.ImageFile.Length == 0)
            return response.SetError("ERR-400", "Invalid or empty image file.", null);

        using var memoryStream = new MemoryStream();
        await model.ImageFile.CopyToAsync(memoryStream);

        var fileBytes = memoryStream.ToArray();

        const int maxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB
        if (fileBytes.Length > maxFileSizeInBytes)
            return response.SetError("ERR-400", "File size exceeds 5MB limit.", null);

        var base64String = Convert.ToBase64String(fileBytes);
        var contentType = model.ImageFile.ContentType;

        var result = new
        {
            FileName = model.ImageFile.FileName,
            ContentType = contentType,
            Base64 = base64String
        };

        return response.SetSuccess("SUCCESS-200", "File processed successfully.", result);
    }

    private static string GetSafeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(name.Where(c => !invalidChars.Contains(c))).Trim();
    }

}
