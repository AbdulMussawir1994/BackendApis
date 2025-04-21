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

            if (model.CV != null)
                employee.CvUrl = await _fileService.SaveFileAsync(model.CV, "cvs", cancellationToken);
            else
                employee.CvUrl = string.Empty;

            if (model.Image != null)
                employee.ImageUrl = await _fileService.SaveFileAsync(model.Image, "images", cancellationToken);
            else
                employee.ImageUrl = string.Empty;

            await _db.Employees.AddAsync(employee, cancellationToken);
            return await _db.SaveChangesAsync(cancellationToken) > 0;
        }

        public IQueryable<GetEmployeeDto> GetEmployees(CancellationToken cancellationToken)
        {
            return _db.Employees
               .AsNoTracking()
               .Include(e => e.ApplicationUser)
               .AsSplitQuery()
               .Select(e => new GetEmployeeDto
               {
                   Id = e.EmployeeId,
                   EmployeeName = e.Name,
                   Age = e.Age,
                   Salary = e.Salary,
                   Image = e.ImageUrl,
                   AppUserId = e.ApplicationUserId,
                   UserName = e.ApplicationUser.UserName
               });
        }

        public async Task<IEnumerable<GetEmployeeDto>> GetEmployeesList(CancellationToken cancellationToken)
        {
            return await _db.Employees
                .AsNoTrackingWithIdentityResolution()
                .Include(e => e.ApplicationUser)
                .AsSplitQuery()
                .Select(e => new GetEmployeeDto
                {
                    Id = e.EmployeeId,
                    EmployeeName = e.Name,
                    Age = e.Age,
                    Salary = e.Salary,
                    AppUserId = e.ApplicationUserId,
                    UserName = e.ApplicationUser.UserName
                })
                .ToListAsync(cancellationToken);
        }

        public IAsyncEnumerable<GetEmployeeDto> GetEmployeesIAsyncEnumerable(CancellationToken cancellationToken)
        {
            return _db.Employees
                .AsNoTrackingWithIdentityResolution()
                .Include(e => e.ApplicationUser)
                .AsSplitQuery()
                .Select(e => new GetEmployeeDto
                {
                    Id = e.EmployeeId,
                    EmployeeName = e.Name,
                    Age = e.Age,
                    Salary = e.Salary,
                    AppUserId = e.ApplicationUserId,
                    UserName = e.ApplicationUser.UserName
                })
                .AsAsyncEnumerable(); // ✅ Keep this, remove WithCancellation
        }
    }
}
