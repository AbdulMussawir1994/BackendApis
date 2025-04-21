using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;

namespace DAL.RepositoryLayer.IDataAccess
{
    public interface IEmployeeDbAccess
    {
        Task<bool> CreateEmployee(CreateEmployeeViewModel model, CancellationToken cancellationToken);
        Task<IEnumerable<GetEmployeeDto>> GetEmployeesList(CancellationToken cancellationToken);
        IQueryable<GetEmployeeDto> GetEmployees(CancellationToken cancellationToken);
        IAsyncEnumerable<GetEmployeeDto> GetEmployeesIAsyncEnumerable(CancellationToken cancellationToken);
        Task<bool> UpdateEmployee(UpdateEmployeeViewModel model, CancellationToken cancellationToken);
        Task<bool> DeleteEmployee(EmployeeIdViewModel model, CancellationToken cancellationToken);
        Task<GetEmployeeDto?> GetEmployeeById(EmployeeIdViewModel model, CancellationToken cancellationToken);
        Task<bool> PatchEmployee(EmployeeByIdUpdateViewModel model, CancellationToken cancellationToken);
    }
}
