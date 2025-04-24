using DAL.RepositoryLayer.IDataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace DAL.RepositoryLayer.DataAccess;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;

    public FileService(IWebHostEnvironment env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
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

    private static string GetSafeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(name.Where(c => !invalidChars.Contains(c))).Trim();
    }
}