using DAL.DatabaseLayer.DataContext;
using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.Models;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.RepositoryLayer.IDataAccess;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace DAL.RepositoryLayer.DataAccess
{
    public class EmployeeDbAccess : IEmployeeDbAccess
    {
        private readonly WebContextDb _db;
        private readonly IFileService _fileService;

        public EmployeeDbAccess(WebContextDb db, IFileService fileService)
        {
            _db = db;
            _fileService = fileService;
        }

        public async Task<bool> CreateEmployee(CreateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var employee = model.Adapt<Employee>();
            employee.CvUrl = model.CV != null ? await _fileService.SaveFileAsync(model.CV, "cvs", cancellationToken) : string.Empty;
            employee.ImageUrl = model.Image != null ? await _fileService.SaveFileAsync(model.Image, "images", cancellationToken) : string.Empty;

            await _db.Employees.AddAsync(employee, cancellationToken);
            return await _db.SaveChangesAsync(cancellationToken) > 0;
        }

        public IQueryable<GetEmployeeDto> GetEmployees(CancellationToken cancellationToken) =>
            _db.Employees
                .AsNoTracking()
                .Include(e => e.ApplicationUser)
                .AsSplitQuery()
                .Select(e => new GetEmployeeDto
                {
                    Id = e.Id,
                    EmployeeName = e.Name,
                    Age = e.Age,
                    Salary = e.Salary,
                    Image = e.ImageUrl,
                    AppUserId = e.ApplicationUserId,
                    UserName = e.ApplicationUser.UserName
                });

        public async Task<IEnumerable<GetEmployeeDto>> GetEmployeesList(CancellationToken cancellationToken) =>
            await GetEmployees(cancellationToken).ToListAsync(cancellationToken);

        public IAsyncEnumerable<GetEmployeeDto> GetEmployeesIAsyncEnumerable(CancellationToken cancellationToken) =>
            GetEmployees(cancellationToken).AsAsyncEnumerable();

        public async Task<bool> UpdateEmployee(UpdateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var employee = await _db.Employees.FindAsync([model.Id], cancellationToken);

            if (employee is null)
                return false;

            employee.Name = model.Name;
            employee.Age = model.Age;
            employee.Salary = model.Salary;

            if (model.CV != null)
                employee.CvUrl = await _fileService.SaveFileAsync(model.CV, "cvs", cancellationToken);

            if (model.Image != null)
                employee.ImageUrl = await _fileService.SaveFileAsync(model.Image, "images", cancellationToken);

            employee.UpdatedDate = DateTime.UtcNow;

            _db.Employees.Update(employee);
            return await _db.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<bool> DeleteEmployee(EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            var employee = await _db.Employees.Where(e => e.Id == model.Id && !e.IsDeleted).FirstOrDefaultAsync(cancellationToken);

            if (employee is null)
                return false;

            employee.IsDeleted = true;
            employee.UpdatedDate = DateTime.UtcNow;

            _db.Entry(employee).Property(e => e.IsDeleted).IsModified = true;
            _db.Entry(employee).Property(e => e.UpdatedDate).IsModified = true;
            //_db.Employees.Update(employee);
            return await _db.SaveChangesAsync(cancellationToken) > 0;
        }


        public async Task<GetEmployeeDto?> GetEmployeeById(EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            return await _db.Employees
                .AsNoTracking()
                .Include(e => e.ApplicationUser)
                .Where(e => e.Id == model.Id && !e.IsDeleted)
                .AsSplitQuery()
                .Select(e => new GetEmployeeDto
                {
                    Id = e.Id,
                    EmployeeName = e.Name,
                    Age = e.Age,
                    Salary = e.Salary,
                    AppUserId = e.ApplicationUserId,
                    UserName = e.ApplicationUser.UserName
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> PatchEmployee(EmployeeByIdUpdateViewModel model, CancellationToken cancellationToken)
        {
            var employee = await _db.Employees
                .Where(e => e.Id == model.Id && !e.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (employee is null || string.Equals(employee.Name?.Trim(), model.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            employee.Name = model.Name;
            employee.UpdatedDate = DateTime.UtcNow;

            _db.Entry(employee).Property(e => e.Name).IsModified = true;
            _db.Entry(employee).Property(e => e.UpdatedDate).IsModified = true;

            return await _db.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}
