using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels;
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

        public Task<MobileResponse<IQueryable<GetEmployeeDto>>> GetEmployeesListAsync(ViewEmployeeModel model)
        {
            var response = new MobileResponse<IQueryable<GetEmployeeDto>>(_configHandler, "employee");
            var result = _employeeDbAccess.GetEmployees(model);

            return Task.FromResult(result.Any()
                ? response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", result)
                : response.SetError("ERR-404", "No employees found."));
        }

        public async Task<MobileResponse<IEnumerable<GetEmployeeDto>>> GetEmployeesList(ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<IEnumerable<GetEmployeeDto>>(_configHandler, "employee");
            var result = await _employeeDbAccess.GetEmployeesList(model, cancellationToken);

            return result.Any()
                ? response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", result)
                : response.SetError("ERR-404", "No employees found.", Enumerable.Empty<GetEmployeeDto>());
        }

        public async Task<MobileResponse<EmployeeListResponse>> GetEmployeesPaginationAsync(ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<EmployeeListResponse>(_configHandler, "employee");

            var result = await _employeeDbAccess.GetEmployeesCount(model, cancellationToken);

            return result.List.Any()
                ? response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", result)
                : response.SetError("ERR-404", "No employees found.", new EmployeeListResponse());
        }

        public async Task<MobileResponse<IAsyncEnumerable<GetEmployeeDto>>> GetEmployeesListAsync2(ViewEmployeeModel model)
        {
            var response = new MobileResponse<IAsyncEnumerable<GetEmployeeDto>>(_configHandler, "employee");

            var employeeStream = _employeeDbAccess.GetEmployeesIAsyncEnumerable(model);

            // Check if the stream has at least one record (early validation)
            await foreach (var item in employeeStream)
            {
                return response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", employeeStream);
            }

            return response.SetError("ERR-404", "No employees found.", AsyncEnumerable.Empty<GetEmployeeDto>());
        }

        public async Task<MobileResponse<bool>> CreateEmployeeAsync(CreateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<bool>(_configHandler, "employee");

            // Check if ApplicationUserId is "string" or not a valid GUID
            if (!Guid.TryParse(model.ApplicationUserId, out _))
            {
                return response.SetError("ERR-1001", "Invalid application user ID.", false);
            }

            //var result = await _employeeDbAccess.CreateEmployee(model, cancellationToken);
            var result = await _employeeDbAccess.CreateEmployee1(model, cancellationToken);

            return result.Status.IsSuccess
                ? response.SetSuccess("SUCCESS-200", "Employee created successfully.", true)
                : response.SetError("ERR-500", "Failed to create employee.", false);
        }

        public async Task<MobileResponse<bool>> UpdateEmployeeAsync(UpdateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<bool>(_configHandler, "employee");

            // Check if ApplicationUserId is "string" or not a valid GUID
            if (!Guid.TryParse(model.ApplicationUserId, out _))
            {
                return response.SetError("ERR-1001", "Invalid application user ID.", false);
            }

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

        public async Task<MobileResponse<GetEmployeeDto?>> GetEmployeeByIdAsyncForTest(EmployeeIdViewModel model, CancellationToken token)
        {
            var response = new MobileResponse<GetEmployeeDto?>(_configHandler, "employee");

            try
            {
                var employee = await _employeeDbAccess.GetEmployeeById(model, token);
                if (employee is null)
                    return response.SetError("NOT_FOUND", "Employee not found.");

                return response.SetSuccess("SUCCESS-200", "Employee fetched successfully.", employee);
            }
            catch (Exception ex)
            {
                return response.SetError("ERR-500", $"An unexpected error occurred: {ex.Message}");
            }
        }

        public async Task<MobileResponse<bool>> PatchEmployeeAsync(EmployeeByIdUpdateViewModel model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<bool>(_configHandler, "employee");
            var result = await _employeeDbAccess.PatchEmployee(model, cancellationToken);
            return result
                ? response.SetSuccess("SUCCESS-200", "Employee patched successfully.", true)
                : response.SetError("ERR-404", "Employee not found.", false);
        }
        public async Task<MobileResponse<string>> GetEmployeeFilePath(DownloadFileByIdViewModel model)
        {
            var response = new MobileResponse<string>(_configHandler, "employee");

            var filePath = await _employeeDbAccess.GetEmployeeFilePathByIdAsync(model);

            return string.IsNullOrWhiteSpace(filePath)
                ? response.SetError("ERR-404", "Employee file path not found.", null)
                : response.SetSuccess("SUCCESS-200", "File path retrieved.", filePath);
        }

        public async Task<MobileResponse<Dictionary<string, List<GetEmployeeDto>>>> GetAllEmployeesAsync()
        {
            var response = new MobileResponse<Dictionary<string, List<GetEmployeeDto>>>(_configHandler, "Employee");

            var groupedEmployees = await _employeeDbAccess.GetAllEmployeesAsync();

            if (groupedEmployees == null || !groupedEmployees.Any())
            {
                return response.SetError("ERR-404", "No employees found.", new Dictionary<string, List<GetEmployeeDto>>());
            }

            return response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", groupedEmployees);
        }

        public async Task<MobileResponse<object>> GetEmployeesKeysetAsync(KeysetPaginationRequest model, CancellationToken cancellationToken)
        {
            var response = new MobileResponse<object>(_configHandler, "employee");

            // ✅ Parse and validate LastId
            Guid? lastGuid = null;
            if (!string.IsNullOrWhiteSpace(model.LastId))
            {
                if (!Guid.TryParse(model.LastId, out Guid parsed))
                    return response.SetError("ERR-400", "Invalid LastId format.", null);

                lastGuid = parsed;
            }

            // ✅ Fetch data
            var employees = await _employeeDbAccess.GetEmployeesKeysetAsync(lastGuid, model.PageSize, cancellationToken);

            // ✅ Handle empty result
            if (employees is null || !employees.Any())
            {
                return response.SetError("ERR-404", "No employees found.", new
                {
                    lastId = (Guid?)null,
                    employees = new List<GetEmployeeDto>()
                });
            }

            // ✅ Return paginated result with last ID
            return response.SetSuccess("SUCCESS-200", "Employee list fetched successfully.", new
            {
                lastId = employees[^1].Id, // 🧠 last fetched ID
                employees
            });
        }
    }
}
