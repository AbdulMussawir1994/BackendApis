using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DAL.DatabaseLayer.ViewModels;

public class FilesViewModel
{
    [Required(ErrorMessage = "Upload document is required.")]
    public IFormFile[] Document { get; set; }
}

public class DownloadFileViewModel
{
    [Required(ErrorMessage = "Path is required.")]
    public string FilePath { get; set; }
}

public class DownloadFileByIdViewModel
{
    [Required(ErrorMessage = "Id is required.")]
    public string Id { get; set; }

    [Required(ErrorMessage = "File type is required.")]
    [RegularExpression("cv|image", ErrorMessage = "Invalid Type, Allowed Types 'cv' or 'image")]
    public string FileType { get; set; }
}

public class UploadBase64ImageViewModel
{
    [Required(ErrorMessage = "Base64 string is required.")]
    public string Base64String { get; set; }

    [Required(ErrorMessage = "File name is required.")]
    public string FileName { get; set; }
}

public class UploadPhysicalImageViewModel
{
    [Required(ErrorMessage = "Image file is required.")]
    public IFormFile ImageFile { get; set; }
}

public class Base64FileResult
{
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public string Base64 { get; set; }
}


