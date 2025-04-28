using DAL.DatabaseLayer.ViewModels;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;

namespace DAL.RepositoryLayer.Repositories
{
    public class FilesServiceRepository : IFilesServiceRepository
    {
        private readonly IFileUtility _fileUtility;
        private readonly ConfigHandler _configHandler;

        public FilesServiceRepository(IFileUtility fileUtility, ConfigHandler configHandler)
        {
            _fileUtility = fileUtility;
            _configHandler = configHandler;
        }

        public async Task<MobileResponse<object>> UploadFilesAsync(FilesViewModel model)
        {
            var response = new MobileResponse<object>(_configHandler, "FilesService");

            var folder = DateTime.UtcNow.ToString("yyyy/MM");
            var savedFiles = new List<object>();

            foreach (var file in model.Document)
            {
                var saveResult = await _fileUtility.SaveFileInternalAsync(file, folder);

                if (!saveResult.Status.IsSuccess)
                    return response.SetError("ERR-500", saveResult.Status.StatusMessage, false);

                savedFiles.Add(new { file.FileName, Path = saveResult.Content });
            }

            return savedFiles.Count > 0
                ? response.SetSuccess("SUCCESS-200", "Files uploaded successfully.", savedFiles)
                : response.SetError("ERR-500", "No files were saved.", false);
        }

        public async Task<MobileResponse<object>> UploadBase64ImageAsync(UploadBase64ImageViewModel model)
        {
            var response = new MobileResponse<object>(_configHandler, "FilesService");

            var folder = DateTime.UtcNow.ToString("yyyy/MM");
            var saveResult = await _fileUtility.SaveBase64FileAsync(model.Base64String, model.FileName, folder);

            if (!saveResult.Status.IsSuccess)
                return response.SetError("ERR-500", $"Image upload failed: {saveResult.Status.StatusMessage}", false);

            var result = new
            {
                FileName = model.FileName,
                Path = saveResult.Content
            };

            return response.SetSuccess("SUCCESS-200", "Image uploaded successfully.", result);
        }

        public async Task<MobileResponse<object>> DownloadFileAsBase64Async(DownloadFileViewModel model)
        {
            var response = new MobileResponse<object>(_configHandler, "FilesService");

            try
            {
                await using var stream = new FileStream(model.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);

                string base64File = Convert.ToBase64String(memory.ToArray());

                var result = new
                {
                    FileName = Path.GetFileName(model.FilePath),
                    FileType = _fileUtility.GetContentType(model.FilePath),
                    Base64 = base64File
                };

                return response.SetSuccess("SUCCESS-200", "File fetched successfully.", result);
            }
            catch (Exception ex)
            {
                return response.SetError("ERR-500", $"Failed to read file: {ex.Message}", false);
            }
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadImageAsync(string imageUrl)
        {
            // Same as GetImageAsync (can extend if you want extra logging for downloads separately)
            return await GetImageAsync(imageUrl);
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)> GetImageAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return (null, null, null);

            var fullPath = _fileUtility.ResolveAbsolutePath(imageUrl);

            if (!System.IO.File.Exists(fullPath))
                return (null, null, null);

            var options = new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.Read,
                BufferSize = 81920, // Default optimal
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            };

            var stream = new FileStream(fullPath, options);
            var contentType = _fileUtility.GetContentType(fullPath);
            var fileName = Path.GetFileName(fullPath);

            return (stream, contentType, fileName);
        }

        public async Task<MobileResponse<object>> UploadImageAndConvertToBase64(UploadPhysicalImageViewModel model)
        {
            var response = new MobileResponse<object>(_configHandler, "FilesService");

            var fileResult = await _fileUtility.UploadImageAndConvertToBase64Async(model);

            if (!fileResult.Status.IsSuccess)
                return response.SetError("ERR-500", "File conversion failed.", false);

            return response.SetSuccess("SUCCESS-200", "File converted to Base64 successfully.", fileResult.Content);
        }


        public string? GetImageContentType(byte[] imageBytes)
        {
            if (imageBytes.Length < 4)
                return null;

            // JPEG magic number
            if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                return "image/jpeg";

            // PNG magic number
            if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                return "image/png";

            // GIF magic number
            if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
                return "image/gif";

            // BMP magic number
            if (imageBytes[0] == 0x42 && imageBytes[1] == 0x4D)
                return "image/bmp";

            return null;
        }
    }
}
