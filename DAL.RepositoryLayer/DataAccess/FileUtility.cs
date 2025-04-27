using DAL.DatabaseLayer.ViewModels;
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

    private static readonly string[] _allowedExtensions =
    {
        ".jpg", ".jpeg", ".gif", ".pdf", ".doc", ".docx",
        ".mp4", ".mp3", ".png", ".xlsx", ".xls", ".txt"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public FileUtility(IWebHostEnvironment env, ConfigHandler configHandler)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _configHandler = configHandler ?? throw new ArgumentNullException(nameof(configHandler));
    }

    public string WebRoot => _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

    public bool IsExtensionAllowed(string extension) =>
        _allowedExtensions.Contains(extension.ToLowerInvariant());

    private bool IsFileSizeValid(long size) =>
        size > 0 && size <= MaxFileSizeBytes;

    public async Task<MobileResponse<string>> SaveFileInternalAsync(IFormFile file, string folderName)
    {
        var response = new MobileResponse<string>(_configHandler, "FilesService");

        if (file == null || !IsFileSizeValid(file.Length))
            return response.SetError("ERR-400", "Invalid file size.", null);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!IsExtensionAllowed(extension))
            return response.SetError("ERR-400", $"Extension '{extension}' is not allowed.", null);

        try
        {
            var safeName = GetSafeFileNameWithoutSpaces(Path.GetFileNameWithoutExtension(file.FileName));
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
            return response.SetError("ERR-500", $"Saving failed: {ex.Message}", null);
        }
    }

    public async Task<MobileResponse<string>> SaveFileInternalAsyncFunction(IFormFile file, string folderName)
    {
        var response = new MobileResponse<string>(_configHandler, "FilesService");

        const long maxAllowedSize = 5 * 1024 * 1024;
        if (file.Length > maxAllowedSize)
            return response.SetError("ERR-400", "File size exceeds 5MB.", null);

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (!IsExtensionAllowed(extension))
            return response.SetError("ERR-400", $"File extension '{extension}' is not allowed.", null);

        try
        {
            var safeName = Path.GetFileNameWithoutExtension(file.FileName)
                .Replace(" ", "_")
                .Replace(".", "_");

            var fileName = $"{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";
            var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folderPath = Path.Combine(rootPath, folderName);

            Directory.CreateDirectory(folderPath);
            var fullPath = Path.Combine(folderPath, fileName);

            await using var inputStream = file.OpenReadStream();
            await using var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920]; // 80KB buffer
            int bytesRead;
            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await outputStream.WriteAsync(buffer, 0, bytesRead);
            }

            var relativePath = Path.Combine(folderName, fileName).Replace("\\", "/");

            return response.SetSuccess("SUCCESS-200", "File saved successfully.", relativePath);
        }
        catch (Exception ex)
        {
            return response.SetError("ERR-500", $"Failed to save file: {ex.Message}", null);
        }
    }


    public async Task<MobileResponse<string>> SaveBase64FileAsync(string base64String, string fileName, string folderName)
    {
        var response = new MobileResponse<string>(_configHandler, "FilesService");

        try
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? ".jpg";
            if (!IsExtensionAllowed(extension))
                return response.SetError("ERR-400", $"Extension '{extension}' is not allowed.", null);

            var base64Content = base64String.Contains(",")
                ? base64String[(base64String.IndexOf(",") + 1)..]
                : base64String;

            var fileBytes = Convert.FromBase64String(base64Content);

            if (!IsFileSizeValid(fileBytes.Length))
                return response.SetError("ERR-400", "File exceeds maximum allowed size (5MB).", null);

            var safeName = GetSafeFileNameWithoutSpaces(Path.GetFileNameWithoutExtension(fileName));
            var generatedName = $"{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";
            var folderPath = Path.Combine(WebRoot, folderName);

            Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, generatedName);
            await File.WriteAllBytesAsync(fullPath, fileBytes);

            var relativePath = Path.Combine(folderName, generatedName).Replace("\\", "/");
            return response.SetSuccess("SUCCESS-200", "Base64 file saved successfully.", relativePath);
        }
        catch (Exception ex)
        {
            return response.SetError("ERR-500", $"Base64 decoding failed: {ex.Message}", null);
        }
    }

    public string ResolveAbsolutePath(string relativePath)
    {
        var sanitized = Uri.UnescapeDataString(relativePath).TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
        return Path.Combine(WebRoot, sanitized);
    }

    public string GetContentType(string path)
    {
        var provider = new FileExtensionContentTypeProvider();
        return provider.TryGetContentType(path, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    public async Task<MobileResponse<object>> UploadImageAndConvertToBase64Async(UploadPhysicalImageViewModel model)
    {
        var response = new MobileResponse<object>(_configHandler, "FilesService");

        if (model.ImageFile == null || model.ImageFile.Length == 0)
            return response.SetError("ERR-400", "Invalid image file.", null);

        await using var memoryStream = new MemoryStream();
        await model.ImageFile.CopyToAsync(memoryStream);

        var fileBytes = memoryStream.ToArray();

        if (!IsFileSizeValid(fileBytes.Length))
            return response.SetError("ERR-400", "Image exceeds allowed size.", null);

        var base64String = Convert.ToBase64String(fileBytes);

        return response.SetSuccess("SUCCESS-200", "Image converted successfully.", new
        {
            model.ImageFile.FileName,
            model.ImageFile.ContentType,
            Base64 = base64String
        });
    }

    private static string GetSafeFileNameWithoutSpaces(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(name.Where(c => !invalidChars.Contains(c))).Replace(" ", "_").Trim();
    }
}
