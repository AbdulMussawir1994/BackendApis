using DAL.DatabaseLayer.ViewModels;
using DAL.ServiceLayer.Models;
using Microsoft.AspNetCore.Http;

namespace DAL.RepositoryLayer.IDataAccess;

public interface IFileUtility
{
    string WebRoot { get; }
    string GetContentType(string path);
    string ResolveAbsolutePath(string relativePath);
    Task<MobileResponse<string>> SaveFileInternalAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);
    Task<MobileResponse<string>> SaveBase64FileAsync(string base64String, string fileName, string folderName, CancellationToken cancellationToken = default);
    Task<MobileResponse<object>> UploadImageAndConvertToBase64Async(UploadPhysicalImageViewModel model, CancellationToken cancellationToken = default);

}
