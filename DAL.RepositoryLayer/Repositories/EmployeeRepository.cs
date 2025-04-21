using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels.AuthModels;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DAL.RepositoryLayer.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmployeeRepository> _logger;
        private readonly ConfigHandler _configHandler;
        private readonly IEmployeeDbAccess _employeeDbAccess;

        public EmployeeRepository(ILogger<EmployeeRepository> logger, ConfigHandler configHandler, IEmployeeDbAccess employeeDbAccess)
        {
            _logger = logger;
            _configHandler = configHandler;
            _employeeDbAccess = employeeDbAccess;
        }

        public Task<MobileResponse<IQueryable<GetEmployeeDto>>> GetEmployeesListAsync(CancellationToken cancellationToken)
        {
            var response = new MobileResponse<IQueryable<GetEmployeeDto>>(_configHandler, "employee");

            var result = _employeeDbAccess.GetEmployees(cancellationToken); // IQueryable is lazy; no await needed

            if (result.Any()) // Executes the query minimally
            {
                response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", result);
            }
            else
            {
                response.SetError("ERR-404", "No employees found.");
            }

            return Task.FromResult(response);
        }

        public async Task<MobileResponse<IEnumerable<GetEmployeeDto>>> GetEmployeesList(CancellationToken cancellationToken)
        {
            var response = new MobileResponse<IEnumerable<GetEmployeeDto>>(_configHandler, "employee");

            var result = (await _employeeDbAccess.GetEmployeesList(cancellationToken)).ToList();

            if (result.Count is 0)
                return response.SetError("ERR-404", "No employees found.");

            response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", result);

            return response;
        }

        public async Task<MobileResponse<IAsyncEnumerable<GetEmployeeDto>>> GetEmployeesListAsync2(CancellationToken cancellationToken)
        {
            var response = new MobileResponse<IAsyncEnumerable<GetEmployeeDto>>(_configHandler, "employee");

            try
            {
                var employeeStream = _employeeDbAccess.GetEmployeesIAsyncEnumerable(cancellationToken);

                if (!await employeeStream.AnyAsync(cancellationToken))
                    return response.SetError("ERR-404", "No employees found.");

                response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", employeeStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while streaming employee list.");
                response.SetError("ERR-500", "An unexpected error occurred.");
            }

            return response;
        }

        public async Task<MobileResponse<string>> CreateEmployeeAsync(CreateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<string>(_configHandler, "employee");

            var result = await _employeeDbAccess.CreateEmployee(model, cancellationToken);

            return result
                ? response.SetSuccess("SUCCESS-200", "Employee created successfully.", string.Empty)
                : response.SetError("ERR-500", "Failed to create employee.", string.Empty);
        }

        public Task<MobileResponse<bool>> DeleteEmployeeByIdAsync(EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<LoginResponseModel>(_configHandler, "employee");

            throw new NotImplementedException();
        }

        public Task<MobileResponse<GetEmployeeDto>> GetIEmployeeByIdAsync(EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<LoginResponseModel>(_configHandler, "employee");
            throw new NotImplementedException();
        }

        public Task<MobileResponse<GetEmployeeDto>> PatchEmployeeAsync(EmployeeUpdateViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<LoginResponseModel>(_configHandler, "employee");

            throw new NotImplementedException();
        }

        public Task<MobileResponse<GetEmployeeDto>> UpdateEmployeeAsync(CreateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<LoginResponseModel>(_configHandler, "employee");
            throw new NotImplementedException();
        }
    }
}
