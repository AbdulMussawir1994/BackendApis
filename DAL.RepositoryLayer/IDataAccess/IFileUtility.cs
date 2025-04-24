using Microsoft.AspNetCore.Http;

namespace DAL.RepositoryLayer.IDataAccess;

public interface IFileUtility
{
    string WebRoot { get; }
    Task<string> SaveFileAsync(IFormFile file, string folderName);
    string ResolveAbsolutePath(string relativePath);
    string GetContentType(string path);
}
