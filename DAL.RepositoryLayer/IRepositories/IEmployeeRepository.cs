using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.ServiceLayer.Models;

namespace DAL.RepositoryLayer.IRepositories
{
    public interface IEmployeeRepository
    {
        Task<MobileResponse<bool>> CreateEmployeeAsync(CreateEmployeeViewModel model, CancellationToken cancellationToken);
        Task<MobileResponse<bool>> DeleteEmployeeByIdAsync(EmployeeIdViewModel model, CancellationToken cancellationToken);
        Task<MobileResponse<GetEmployeeDto?>> GetEmployeeByIdAsync(EmployeeIdViewModel model, CancellationToken cancellationToken);
        Task<MobileResponse<IQueryable<GetEmployeeDto>>> GetEmployeesListAsync(ViewEmployeeModel model);
        Task<MobileResponse<IEnumerable<GetEmployeeDto>>> GetEmployeesList(ViewEmployeeModel model, CancellationToken cancellationToken);
        Task<MobileResponse<IAsyncEnumerable<GetEmployeeDto>>> GetEmployeesListAsync2(ViewEmployeeModel model);
        Task<MobileResponse<bool>> PatchEmployeeAsync(EmployeeByIdUpdateViewModel model, CancellationToken cancellationToken);
        Task<MobileResponse<bool>> UpdateEmployeeAsync(UpdateEmployeeViewModel model, CancellationToken cancellationToken);
        Task<MobileResponse<EmployeeListResponse>> GetEmployeesPaginationAsync(ViewEmployeeModel model, CancellationToken cancellationToken);
    }
}
