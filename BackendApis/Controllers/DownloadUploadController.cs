using DAL.DatabaseLayer.DataContext;
using DAL.RepositoryLayer.IDataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[AllowAnonymous]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DownloadUploadController : ControllerBase
{
    private readonly WebContextDb _context;
    private readonly IFileUtility _fileUtility;

    public DownloadUploadController(WebContextDb context, IFileUtility fileUtility)
    {
        _context = context;
        _fileUtility = fileUtility;
    }

    [HttpPost("Upload")]
    public async Task<IActionResult> UploadFiles([FromForm] IFormFile[] files)
    {
        if (files == null || files.Length == 0)
            return BadRequest("No files provided.");

        var savedFiles = new List<object>();
        var folder = DateTime.UtcNow.ToString("yyyy/MM");

        foreach (var file in files)
        {
            var savedPath = await _fileUtility.SaveFileAsync(file, folder);
            savedFiles.Add(new { file.FileName, path = savedPath });
        }

        return Ok(new { status = true, files = savedFiles });
    }

    [HttpGet("Download")]
    public IActionResult DownloadFile([FromQuery] string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return BadRequest("File path is required.");

        var absolutePath = _fileUtility.ResolveAbsolutePath(filePath);
        if (!System.IO.File.Exists(absolutePath))
            return NotFound("File not found.");

        var contentType = _fileUtility.GetContentType(absolutePath);
        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return File(stream, contentType, Path.GetFileName(absolutePath));
    }

    [HttpGet("DownloadById/{id}")]
    public async Task<IActionResult> DownloadEmployeeFile(Guid id, [FromQuery] string fileType)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee is null) return NotFound("Employee not found.");

        var relativePath = fileType?.ToLowerInvariant() switch
        {
            "image" => employee.ImageUrl,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(relativePath))
            return NotFound("Requested file not found.");

        var fullPath = _fileUtility.ResolveAbsolutePath(relativePath);
        if (!System.IO.File.Exists(fullPath))
            return NotFound("File not available on disk.");

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = _fileUtility.GetContentType(fullPath);

        return File(stream, contentType, Path.GetFileName(fullPath));
    }

    [HttpGet("GetImage/{*imageUrl}")]
    public IActionResult GetImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return BadRequest("Image URL is required.");

        var fullPath = _fileUtility.ResolveAbsolutePath(imageUrl);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("Image not found.");

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = _fileUtility.GetContentType(fullPath);

        return File(stream, contentType);
    }

    [HttpGet("DownloadImage/{*imageUrl}")]
    public IActionResult DownloadImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return BadRequest("Image URL is required.");

        var fullPath = _fileUtility.ResolveAbsolutePath(imageUrl);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("Image not found.");

        var contentType = _fileUtility.GetContentType(fullPath);
        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(fullPath)}\"");

        return File(stream, contentType);
    }
}
