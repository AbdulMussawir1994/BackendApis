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



