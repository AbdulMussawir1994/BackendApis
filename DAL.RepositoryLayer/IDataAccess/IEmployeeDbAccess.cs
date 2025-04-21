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
    }
}
