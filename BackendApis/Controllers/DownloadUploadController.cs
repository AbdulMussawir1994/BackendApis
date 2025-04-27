using DAL.DatabaseLayer.ViewModels;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.BaseController;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[AllowAnonymous]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DownloadUploadController : WebBaseController
{
    private readonly IFileUtility _fileUtility;
    private readonly IFilesServiceRepository _serviceRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public DownloadUploadController(IFileUtility fileUtility, IFilesServiceRepository serviceRepository,
                                                                        IEmployeeRepository employeeRepository, ConfigHandler configHandler) : base(configHandler)
    {
        _fileUtility = fileUtility;
        _serviceRepository = serviceRepository;
        _employeeRepository = employeeRepository;
    }

    [HttpPost("UploadFiles")]
    public async Task<IActionResult> UploadMultipleFiles([FromForm] FilesViewModel model)
    {
        var validation = this.ModelValidator(model);
        if (!validation.Status.IsSuccess)
            return Ok(validation);

        var result = await _serviceRepository.UploadFilesAsync(model);
        return Ok(result);
    }

    [HttpPost("UploadBase64Image")]
    public async Task<IActionResult> UploadBase64Image([FromBody] UploadBase64ImageViewModel model)
    {
        var validation = this.ModelValidator(model);
        if (!validation.Status.IsSuccess)
            return Ok(validation);

        var result = await _serviceRepository.UploadBase64ImageAsync(model);
        return Ok(result);
    }

    [HttpPost("UploadImageToBase64")]
    public async Task<IActionResult> UploadImageToBase64([FromForm] UploadPhysicalImageViewModel model)
    {
        var validation = this.ModelValidator(model);
        if (!validation.Status.IsSuccess)
            return Ok(validation);

        var result = await _serviceRepository.UploadImageAndConvertToBase64(model);
        return Ok(result);
    }

    [HttpPost("DownloadFile")]
    public IActionResult DownloadFile([FromBody] DownloadFileViewModel model)
    {
        var validation = this.ModelValidator(model);

        if (!validation.Status.IsSuccess)
            return Ok(validation);

        var absolutePath = _fileUtility.ResolveAbsolutePath(model.FilePath);

        if (!System.IO.File.Exists(absolutePath))
            return NotFound("File not found.");

        var contentType = _fileUtility.GetContentType(absolutePath);

        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, contentType, Path.GetFileName(absolutePath));
    }

    [HttpGet("DownloadFileBase64")]
    public async Task<IActionResult> DownloadFileBase64([FromQuery] DownloadFileViewModel model)
    {
        var validation = this.ModelValidator(model);
        if (!validation.Status.IsSuccess)
            return Ok(validation);

        var result = await _serviceRepository.DownloadFileAsBase64Async(model);
        return Ok(result);
    }

    [HttpPost("DownloadById")]
    public async Task<IActionResult> DownloadEmployeeFile([FromForm] DownloadFileByIdViewModel model)
    {
        var validation = this.ModelValidator(model);
        if (!validation.Status.IsSuccess)
            return Ok(validation);

        var pathResponse = await _employeeRepository.GetEmployeeFilePath(model);

        if (!pathResponse.Status.IsSuccess || string.IsNullOrWhiteSpace(pathResponse.Content))
            return NotFound(pathResponse);

        var fullPath = _fileUtility.ResolveAbsolutePath(pathResponse.Content);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("File not available on disk.");

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = _fileUtility.GetContentType(fullPath);

        return File(stream, contentType, Path.GetFileName(fullPath));
    }

    [HttpGet("GetImage/{*imageUrl}")]
    public async Task<IActionResult> GetImage(string imageUrl)
    {
        var validation = this.ModelValidator(imageUrl);
        if (!validation.Status.IsSuccess)
            return Ok(validation);

        var (stream, contentType, _) = await _serviceRepository.GetImageAsync(imageUrl);

        if (stream is null)
            return NotFound(contentType);

        return File(stream, contentType);
    }

    [HttpGet("DownloadImage/{*imageUrl}")]
    public async Task<IActionResult> DownloadImage(string imageUrl)
    {
        var validation = this.ModelValidator(imageUrl);
        if (!validation.Status.IsSuccess)
            return Ok(validation);

        var (stream, contentType, fileName) = await _serviceRepository.DownloadImageAsync(imageUrl);

        if (stream is null)
            return NotFound(contentType);

        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        return File(stream, contentType);
    }

    //[HttpGet("GetImage/{*imageUrl}")]
    //public IActionResult GetImage(string imageUrl)
    //{
    //    if (string.IsNullOrWhiteSpace(imageUrl))
    //        return BadRequest("Image URL is required.");

    //    var fullPath = _fileUtility.ResolveAbsolutePath(imageUrl);

    //    if (!System.IO.File.Exists(fullPath))
    //        return NotFound("Image not found.");

    //    var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    //    var contentType = _fileUtility.GetContentType(fullPath);

    //    return File(stream, contentType);
    //}

    //[HttpGet("DownloadImage/{*imageUrl}")]
    //public IActionResult DownloadImage(string imageUrl)
    //{
    //    if (string.IsNullOrWhiteSpace(imageUrl))
    //        return BadRequest("Image URL is required.");

    //    var fullPath = _fileUtility.ResolveAbsolutePath(imageUrl);

    //    if (!System.IO.File.Exists(fullPath))
    //        return NotFound("Image not found.");

    //    var contentType = _fileUtility.GetContentType(fullPath);
    //    var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

    //    Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(fullPath)}\"");

    //    return File(stream, contentType);
    //}
}
