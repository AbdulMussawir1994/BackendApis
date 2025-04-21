using DAL.RepositoryLayer.IDataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DAL.RepositoryLayer.DataAccess
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken)
        {
            var uploadPath = Path.Combine(_env.WebRootPath, folder);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);

            return $"/{folder}/{fileName}"; // return relative path
        }
    }
}
