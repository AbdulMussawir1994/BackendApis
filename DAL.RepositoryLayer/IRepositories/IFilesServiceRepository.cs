using DAL.DatabaseLayer.ViewModels;
using DAL.ServiceLayer.Models;

namespace DAL.RepositoryLayer.IRepositories
{
    public interface IFilesServiceRepository
    {
        Task<MobileResponse<object>> UploadFilesAsync(FilesViewModel files);
        Task<MobileResponse<object>> DownloadFileAsBase64Async(DownloadFileViewModel model);
    }
}
