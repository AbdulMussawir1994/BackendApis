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

        public async Task<MobileResponse<object>> DownloadFileAsBase64Async(DownloadFileViewModel model)
        {
            var response = new MobileResponse<object>(_configHandler, "FilesService");

            var absolutePath = _fileUtility.ResolveAbsolutePath(model.FilePath);

            if (!System.IO.File.Exists(absolutePath))
                return response.SetError("ERR-404", "File not found.", false);

            try
            {
                await using var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);

                string base64File = Convert.ToBase64String(memory.ToArray());

                var result = new
                {
                    FileName = Path.GetFileName(absolutePath),
                    FileType = _fileUtility.GetContentType(absolutePath),
                    Base64 = base64File
                };

                return response.SetSuccess("SUCCESS-200", "File fetched successfully.", result);
            }
            catch (Exception ex)
            {
                return response.SetError("ERR-500", $"Failed to read file: {ex.Message}", false);
            }
        }

    }
}
