using DAL.DatabaseLayer.DataContext;
using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.Models;
using DAL.DatabaseLayer.ViewModels;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.RepositoryLayer.IDataAccess;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DAL.RepositoryLayer.DataAccess
{
    public class EmployeeDbAccess : IEmployeeDbAccess
    {
        private readonly WebContextDb _db;
        private readonly IFileUtility _fileUtility;
        private readonly ConfigHandler _configHandler;

        public EmployeeDbAccess(WebContextDb db, IFileUtility fileUtility, ConfigHandler configHandler)
        {
            _db = db;
            _fileUtility = fileUtility;
            _configHandler = configHandler;
        }

        public async Task<bool> CreateEmployee(CreateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var employee = model.Adapt<Employee>();
            var folder = DateTime.UtcNow.ToString("yyyy/MM");

            // Prepare file upload tasks
            var cvTask = model.CV != null ? _fileUtility.SaveFileInternalAsync(model.CV, folder) : Task.FromResult<MobileResponse<string>>(null);
            var imageTask = model.Image != null ? _fileUtility.SaveFileInternalAsync(model.Image, folder) : Task.FromResult<MobileResponse<string>>(null);

            // Run both uploads in parallel
            var uploadResults = await Task.WhenAll(cvTask, imageTask);

            var cvResult = uploadResults[0];
            var imageResult = uploadResults[1];

            employee.CvUrl = (cvResult != null && cvResult.Status.IsSuccess) ? cvResult.Content : string.Empty;
            employee.ImageUrl = (imageResult != null && imageResult.Status.IsSuccess) ? imageResult.Content : string.Empty;

            await _db.Employees.AddAsync(employee, cancellationToken);
            return await _db.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<MobileResponse<string>> CreateEmployee1(CreateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<string>(_configHandler, "EmployeeDbAccess");
            var employee = model.Adapt<Employee>();
            var folder = DateTime.UtcNow.ToString("yyyy/MM");

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Parallel upload tasks
                var cvTask = model.CV != null
                    ? _fileUtility.SaveFileInternalAsync(model.CV, folder)
                    : Task.FromResult<MobileResponse<string>>(null);

                var imageTask = model.Image != null
                    ? _fileUtility.UploadImageAndConvertToBase64Async(new UploadPhysicalImageViewModel { ImageFile = model.Image })
                    : Task.FromResult<MobileResponse<object>>(null);

                await Task.WhenAll(cvTask, imageTask);

                var cvResult = await cvTask;
                var imageResult = await imageTask;

                // Validate CV
                if (cvResult != null && !cvResult.Status.IsSuccess)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return response.SetError("ERR-400", $"CV Upload Failed: {cvResult.Status.StatusMessage}", null);
                }

                // Validate Image
                if (imageResult != null && !imageResult.Status.IsSuccess)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return response.SetError("ERR-400", $"Image Upload Failed: {imageResult.Status.StatusMessage}", null);
                }

                // Set CV URL
                employee.CvUrl = cvResult?.Content ?? string.Empty;

                // Set Image Base64
                if (imageResult?.Content != null)
                {
                    var imageJson = JsonSerializer.Serialize(imageResult.Content);
                    var imgFile = JsonSerializer.Deserialize<Base64FileResult>(imageJson);
                    employee.ImageUrl = imgFile?.Base64 ?? string.Empty;
                }

                // Insert into Database
                await _db.Employees.AddAsync(employee, cancellationToken);
                var saveResult = await _db.SaveChangesAsync(cancellationToken);

                if (saveResult == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return response.SetError("ERR-500", "Failed to save employee into database.", null);
                }

                await transaction.CommitAsync(cancellationToken);
                return response.SetSuccess("SUCCESS-200", "Employee created successfully.", null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return response.SetError("ERR-500", $"Unexpected error: {ex.Message}", null);
            }
        }

        public IQueryable<GetEmployeeDto> GetEmployees(ViewEmployeeModel model)
        {
            if (model.PageSize <= 0 || model.PageNumber <= 0)
                return Enumerable.Empty<GetEmployeeDto>().AsQueryable();

            return _db.Employees
                .AsNoTrackingWithIdentityResolution()
                .Where(e => e.IsActive)
                .Include(e => e.ApplicationUser)
                .AsSplitQuery()
                .OrderBy(e => e.Id) // Necessary for Skip/Take stability
                .Skip((model.PageNumber - 1) * model.PageSize)
                .Take(model.PageSize)
                .Select(e => new GetEmployeeDto
                {
                    Id = e.Id.ToString().ToLower(),
                    EmployeeName = e.Name,
                    Age = e.Age,
                    Salary = e.Salary,
                    Image = e.ImageUrl,
                    Cv = e.CvUrl,
                    AppUserId = e.ApplicationUserId,
                    UserName = e.ApplicationUser.UserName
                });
        }

        public async Task<IEnumerable<GetEmployeeDto>> GetEmployeesList(ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            if (model.PageSize <= 0 || model.PageNumber <= 0)
                return Enumerable.Empty<GetEmployeeDto>();

            return await _db.Employees
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(e => e.ApplicationUser)
                .AsSplitQuery() // Optional: use only if you include navigations
                .OrderBy(e => e.Id) // Always apply ordering when using Skip/Take
                .Select(e => new GetEmployeeDto
                {
                    Id = e.Id.ToString().ToLower(),
                    EmployeeName = e.Name,
                    Age = e.Age,
                    Salary = e.Salary,
                    Image = e.ImageUrl,
                    Cv = e.CvUrl,
                    AppUserId = e.ApplicationUserId,
                    UserName = e.ApplicationUser.UserName
                })
                .Skip((model.PageNumber - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<EmployeeListResponse> GetEmployeesCount(ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            if (model.PageSize <= 0 || model.PageNumber <= 0)
                return new EmployeeListResponse();

            var query = _db.Employees
                .AsNoTracking()
                .Where(e => e.IsActive)
                .Include(e => e.ApplicationUser)
                .AsSplitQuery();

            int totalCount = await query.CountAsync(cancellationToken);

            var employees = await query
                .OrderBy(e => e.Id)
                .Skip((model.PageNumber - 1) * model.PageSize)
                .Take(model.PageSize)
                .Select(e => new GetEmployeeDto
                {
                    Id = e.Id.ToString().ToLower(),
                    EmployeeName = e.Name,
                    Age = e.Age,
                    Salary = e.Salary,
                    Image = e.ImageUrl,
                    Cv = e.CvUrl,
                    AppUserId = e.ApplicationUserId,
                    UserName = e.ApplicationUser.UserName
                })
                .ToListAsync(cancellationToken);

            return new EmployeeListResponse
            {
                List = employees,
                TotalRecords = totalCount
            };
        }

        public IAsyncEnumerable<GetEmployeeDto> GetEmployeesIAsyncEnumerable(ViewEmployeeModel model)
        {
            if (model.PageSize <= 0 || model.PageNumber <= 0)
                return AsyncEnumerable.Empty<GetEmployeeDto>();

            return _db.Employees
                .AsNoTrackingWithIdentityResolution()
                .Where(e => e.IsActive)
                .Include(e => e.ApplicationUser)
                .AsSplitQuery()
                .OrderBy(e => e.Id)
                .Skip((model.PageNumber - 1) * model.PageSize)
                .Take(model.PageSize)
                .Select(e => new GetEmployeeDto
                {
                    Id = e.Id.ToString().ToLower(),
                    EmployeeName = e.Name,
                    Age = e.Age,
                    Salary = e.Salary,
                    Image = e.ImageUrl,
                    Cv = e.CvUrl,
                    AppUserId = e.ApplicationUserId,
                    UserName = e.ApplicationUser.UserName
                })
                .AsAsyncEnumerable();
        }

        public async Task<bool> UpdateEmployee(UpdateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id.ToString().ToLower() == model.Id && e.IsActive, cancellationToken);

            if (employee is null)
                return false;

            var folder = DateTime.UtcNow.ToString("yyyy/MM");

            employee.Name = model.Name?.Trim();
            employee.Age = model.Age;
            employee.Salary = model.Salary;

            // Prepare parallel upload tasks
            var cvTask = model.CV != null ? _fileUtility.SaveFileInternalAsync(model.CV, folder) : Task.FromResult<MobileResponse<string>>(null);
            var imageTask = model.Image != null ? _fileUtility.SaveFileInternalAsync(model.Image, folder) : Task.FromResult<MobileResponse<string>>(null);

            // Run uploads in parallel
            var uploadResults = await Task.WhenAll(cvTask, imageTask);

            var cvResult = uploadResults[0];
            var imageResult = uploadResults[1];

            if (cvResult != null && cvResult.Status.IsSuccess)
                employee.CvUrl = cvResult.Content;

            if (imageResult != null && imageResult.Status.IsSuccess)
                employee.ImageUrl = imageResult.Content;

            employee.UpdatedDate = DateTime.UtcNow;

            _db.Employees.Update(employee);
            return await _db.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<bool> DeleteEmployee(EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id.ToString().ToLower() == model.Id && e.IsActive, cancellationToken);
            if (employee is null) return false;

            employee.IsActive = false;
            employee.UpdatedDate = DateTime.UtcNow;

            _db.Entry(employee).Property(e => e.IsActive).IsModified = true;
            _db.Entry(employee).Property(e => e.UpdatedDate).IsModified = true;
            return await _db.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<GetEmployeeDto?> GetEmployeeById(EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            return await _db.Employees
                .AsNoTracking()
                .Include(e => e.ApplicationUser)
                .Where(e => e.Id.ToString().ToLower() == model.Id && e.IsActive)
                .AsSplitQuery()
                .Select(e => new GetEmployeeDto
                {
                    Id = e.Id.ToString().ToLower(),
                    EmployeeName = e.Name,
                    Age = e.Age,
                    Salary = e.Salary,
                    Cv = e.CvUrl,
                    Image = e.ImageUrl,
                    AppUserId = e.ApplicationUserId,
                    UserName = e.ApplicationUser.UserName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> PatchEmployee(EmployeeByIdUpdateViewModel model, CancellationToken cancellationToken)
        {
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id.ToString().ToLower() == model.Id && e.IsActive, cancellationToken);

            if (employee is null || string.Equals(employee.Name?.Trim(), model.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            employee.Name = model.Name;
            employee.UpdatedDate = DateTime.UtcNow;

            _db.Entry(employee).Property(e => e.Name).IsModified = true;
            _db.Entry(employee).Property(e => e.UpdatedDate).IsModified = true;

            return await _db.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<string> GetEmployeeFilePathByIdAsync(DownloadFileByIdViewModel model)
        {
            return await _db.Employees
                .AsNoTracking()
                .Where(e => e.Id.ToString().ToLower() == model.Id && e.IsActive)
                .Select(e => model.FileType.ToLowerInvariant() == "cv" ? e.CvUrl : e.ImageUrl)
                .FirstOrDefaultAsync();
        }
    }
}
