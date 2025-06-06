﻿using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.ServiceLayer.Models;
using System.Collections.Frozen;

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
        Task<MobileResponse<string>> GetEmployeeFilePath(DownloadFileByIdViewModel model);
        Task<MobileResponse<Dictionary<string, List<GetEmployeeDto>>>> GetAllEmployeesAsync();
        Task<MobileResponse<FrozenDictionary<string, List<GetEmployeeDto>>>> GetAllEmployeesAsync1();
        Task<MobileResponse<object>> GetEmployeesKeysetAsync(KeysetPaginationRequest model, CancellationToken cancellationToken);
    }
}
