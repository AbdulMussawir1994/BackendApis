using DAL.DatabaseLayer.ViewModels;
using DAL.ServiceLayer.Models;

namespace DAL.RepositoryLayer.IRepositories
{
    public interface IFilesServiceRepository
    {
        Task<MobileResponse<object>> UploadFilesAsync(FilesViewModel files);
        Task<MobileResponse<object>> DownloadFileAsBase64Async(DownloadFileViewModel model);
        Task<MobileResponse<object>> UploadBase64ImageAsync(UploadBase64ImageViewModel model);
        Task<(Stream FileStream, string ContentType, string FileName)> GetImageAsync(string imageUrl);
        Task<(Stream FileStream, string ContentType, string FileName)> DownloadImageAsync(string imageUrl);
        Task<MobileResponse<object>> UploadImageAndConvertToBase64(UploadPhysicalImageViewModel model);
    }
}
