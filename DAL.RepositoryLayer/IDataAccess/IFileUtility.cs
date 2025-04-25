using DAL.ServiceLayer.Models;
using Microsoft.AspNetCore.Http;

namespace DAL.RepositoryLayer.IDataAccess;

public interface IFileUtility
{
    string WebRoot { get; }
    Task<MobileResponse<string>> SaveFileInternalAsync(IFormFile file, string folderName);
    string ResolveAbsolutePath(string relativePath);
    string GetContentType(string path);
}
