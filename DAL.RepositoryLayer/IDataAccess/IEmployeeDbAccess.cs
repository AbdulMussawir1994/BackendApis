﻿using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.ServiceLayer.Models;
using System.Collections.Frozen;

namespace DAL.RepositoryLayer.IDataAccess
{
    public interface IEmployeeDbAccess
    {
        Task<MobileResponse<bool>> CreateEmployee(CreateEmployeeViewModel model, CancellationToken cancellationToken);
        Task<MobileResponse<string>> CreateEmployee1(CreateEmployeeViewModel model, CancellationToken cancellationToken);
        Task<IEnumerable<GetEmployeeDto>> GetEmployeesList(ViewEmployeeModel model, CancellationToken cancellationToken);
        IQueryable<GetEmployeeDto> GetEmployees(ViewEmployeeModel model);
        IAsyncEnumerable<GetEmployeeDto> GetEmployeesIAsyncEnumerable(ViewEmployeeModel model);
        Task<bool> UpdateEmployee(UpdateEmployeeViewModel model, CancellationToken cancellationToken);
        Task<MobileResponse<string>> UpdateEmployeeAsync(UpdateEmployeeViewModel model, CancellationToken cancellationToken);
        Task<bool> DeleteEmployee(EmployeeIdViewModel model, CancellationToken cancellationToken);
        Task<GetEmployeeDto?> GetEmployeeById(EmployeeIdViewModel model, CancellationToken cancellationToken);
        Task<bool> PatchEmployee(EmployeeByIdUpdateViewModel model, CancellationToken cancellationToken);
        Task<EmployeeListResponse> GetEmployeesCount(ViewEmployeeModel model, CancellationToken cancellationToken);
        Task<string> GetEmployeeFilePathByIdAsync(DownloadFileByIdViewModel model);
        Task<Dictionary<string, List<GetEmployeeDto>>> GetAllEmployeesAsync();
        Task<FrozenDictionary<string, List<GetEmployeeDto>>> GetAllEmployeesAsync1();
        Task<List<GetEmployeeDto>> GetEmployeesKeysetAsync(Guid? lastId, int pageSize, CancellationToken cancellationToken);
    }
}
