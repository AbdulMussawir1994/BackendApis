using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;

namespace DAL.RepositoryLayer.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IEmployeeDbAccess _employeeDbAccess;
        private readonly ConfigHandler _configHandler;

        public EmployeeRepository(ConfigHandler configHandler, IEmployeeDbAccess employeeDbAccess)
        {
            _configHandler = configHandler;
            _employeeDbAccess = employeeDbAccess;
        }

        public Task<MobileResponse<IQueryable<GetEmployeeDto>>> GetEmployeesListAsync(CancellationToken cancellationToken)
        {
            var response = new MobileResponse<IQueryable<GetEmployeeDto>>(_configHandler, "employee");
            var result = _employeeDbAccess.GetEmployees(cancellationToken);

            return Task.FromResult(result.Any()
                ? response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", result)
                : response.SetError("ERR-404", "No employees found."));
        }

        public async Task<MobileResponse<IEnumerable<GetEmployeeDto>>> GetEmployeesList(CancellationToken cancellationToken)
        {
            var response = new MobileResponse<IEnumerable<GetEmployeeDto>>(_configHandler, "employee");
            var result = await _employeeDbAccess.GetEmployeesList(cancellationToken);

            return result.Any()
                ? response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", result)
                : response.SetError("ERR-404", "No employees found.");
        }

        public async Task<MobileResponse<IAsyncEnumerable<GetEmployeeDto>>> GetEmployeesListAsync2(CancellationToken cancellationToken)
        {
            var response = new MobileResponse<IAsyncEnumerable<GetEmployeeDto>>(_configHandler, "employee");
            var employeeStream = _employeeDbAccess.GetEmployeesIAsyncEnumerable(cancellationToken);

            if (!await employeeStream.AnyAsync(cancellationToken))
                return response.SetError("ERR-404", "No employees found.");

            return response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", employeeStream);
        }

        public async Task<MobileResponse<bool>> CreateEmployeeAsync(CreateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<bool>(_configHandler, "employee");
            var result = await _employeeDbAccess.CreateEmployee(model, cancellationToken);

            return result
                ? response.SetSuccess("SUCCESS-200", "Employee created successfully.", true)
                : response.SetError("ERR-500", "Failed to create employee.", false);
        }

        public async Task<MobileResponse<bool>> UpdateEmployeeAsync(UpdateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<bool>(_configHandler, "employee");
            var result = await _employeeDbAccess.UpdateEmployee(model, cancellationToken);
            return result
                ? response.SetSuccess("SUCCESS-200", "Employee updated successfully.", true)
                : response.SetError("ERR-500", "Failed to update employee.", false);
        }

        public async Task<MobileResponse<bool>> DeleteEmployeeByIdAsync(EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<bool>(_configHandler, "employee");
            var result = await _employeeDbAccess.DeleteEmployee(model, cancellationToken);
            return result
                ? response.SetSuccess("SUCCESS-200", "Employee deleted successfully.", true)
                : response.SetError("ERR-404", "Employee not found.", false);
        }

        public async Task<MobileResponse<GetEmployeeDto?>> GetEmployeeByIdAsync(EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<GetEmployeeDto?>(_configHandler, "employee");
            var result = await _employeeDbAccess.GetEmployeeById(model, cancellationToken);
            return result != null
                ? response.SetSuccess("SUCCESS-200", "Employee fetched successfully.", result)
                : response.SetError("ERR-404", "Employee not found.");
        }

        public async Task<MobileResponse<bool>> PatchEmployeeAsync(EmployeeByIdUpdateViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<bool>(_configHandler, "employee");
            var result = await _employeeDbAccess.PatchEmployee(model, cancellationToken);
            return result
                ? response.SetSuccess("SUCCESS-200", "Employee patched successfully.", true)
                : response.SetError("ERR-404", "Employee not found.", false);
        }

    }
}
