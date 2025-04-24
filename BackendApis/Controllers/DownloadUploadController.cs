using DAL.DatabaseLayer.DataContext;
using DAL.RepositoryLayer.IDataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

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

    [HttpPost("upload")]
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

    //[HttpPost("upload-employee")]
    //public async Task<IActionResult> UploadEmployeeFile([FromForm] IFormFile file, string employeeId, [FromForm] string fileType)
    //{
    //    if (file == null || string.IsNullOrWhiteSpace(fileType))
    //        return BadRequest("File and fileType are required.");

    //    var employee = await _context.Employees.FindAsync(employeeId);
    //    if (employee is null) return NotFound("Employee not found.");

    //    var path = await _fileUtility.SaveFileAsync(file, $"employee/{employeeId}");
    //    switch (fileType.Trim().ToLowerInvariant())
    //    {
    //        case "image": employee.ImageUrl = path; break;
    //        //    case "video": employee.VideoCVURL = path; break;
    //        //  case "audio": employee.AudioCVURL = path; break;
    //        //  case "cv": employee.CVURL = path; break;
    //        default: return BadRequest("Invalid file type.");
    //    }

    //    _context.Employees.Update(employee);
    //    await _context.SaveChangesAsync();

    //    return Ok(new { status = true, path });
    //}

    [HttpGet("download")]
    public IActionResult DownloadFile([FromQuery] string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return BadRequest("File path is required.");

        var absolutePath = _fileUtility.ResolveAbsolutePath(filePath);
        if (!System.IO.File.Exists(absolutePath))
            return NotFound("File not found.");

        var contentType = _fileUtility.GetContentType(absolutePath);
        var fileBytes = System.IO.File.ReadAllBytes(absolutePath);
        var fileName = Path.GetFileName(absolutePath);

        return File(fileBytes, contentType, fileName);
    }

    [HttpGet("DownloadById/{id}")]
    public async Task<IActionResult> DownloadEmployeeFile(Guid id, [FromQuery] string fileType)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee is null) return NotFound("Employee not found.");

        var relativePath = fileType?.ToLowerInvariant() switch
        {
            "image" => employee.ImageUrl,
            // "video" => employee.VideoCVURL,
            //"audio" => employee.AudioCVURL,
            // "cv" => employee.CVURL,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(relativePath))
            return NotFound("Requested file not found.");

        var fullPath = _fileUtility.ResolveAbsolutePath(relativePath);
        if (!System.IO.File.Exists(fullPath))
            return NotFound("File not available on disk.");

        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        memory.Position = 0;

        var contentType = _fileUtility.GetContentType(fullPath);
        var fileName = Path.GetFileName(fullPath);

        return File(memory, contentType, fileName);
    }

    [HttpPut("MultiUploadImage")]
    public async Task<IActionResult> MultiUploadImage([FromForm] IFormFileCollection filecollection, [FromQuery] string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return BadRequest("Product code is required.");

        if (filecollection == null || filecollection.Count == 0)
            return BadRequest("No files received.");

        int passCount = 0;
        var folderPath = Path.Combine(_fileUtility.WebRoot, "files", "2023", productCode);
        Directory.CreateDirectory(folderPath);

        foreach (var file in filecollection)
        {
            if (file.Length == 0) continue;
            var filePath = Path.Combine(folderPath, Path.GetFileName(file.FileName));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            passCount++;
        }

        return Ok(new { status = true, uploaded = passCount });
    }

    [HttpGet("GetImage/{*imageUrl}")]
    public IActionResult GetImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return BadRequest("Image URL is required.");

        var relativePath = Uri.UnescapeDataString(imageUrl).Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(_fileUtility.WebRoot, relativePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("Image not found.");

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fullPath, out var contentType))
            contentType = "application/octet-stream";

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return File(stream, contentType);
    }

    [HttpGet("DownloadImage/{*imageUrl}")]
    public IActionResult DownloadImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return BadRequest("Image URL is required.");

        var relativePath = Uri.UnescapeDataString(imageUrl).Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(_fileUtility.WebRoot, relativePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("File not found.");

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fullPath, out var contentType))
            contentType = "application/octet-stream";

        var fileName = Path.GetFileName(fullPath);
        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        return File(stream, contentType, fileName);
    }

    [HttpGet("DownloadImage1/{*imageUrl}")]
    public IActionResult DownloadImage1(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return BadRequest("Image URL is required.");

        // Normalize path
        var sanitizedPath = Uri.UnescapeDataString(imageUrl)
            .Replace("/", Path.DirectorySeparatorChar.ToString());

        var fullPath = Path.Combine(_fileUtility.WebRoot, sanitizedPath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("Image file not found.");

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fullPath, out var contentType))
            contentType = "application/octet-stream"; // fallback

        var fileBytes = System.IO.File.ReadAllBytes(fullPath);
        var fileName = Path.GetFileName(fullPath);

        // This will trigger a file download in browser
        return File(fileBytes, contentType, fileName);
    }
}