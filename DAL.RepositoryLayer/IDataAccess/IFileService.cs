using Microsoft.AspNetCore.Http;

namespace DAL.RepositoryLayer.IDataAccess
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken);
    }

}
