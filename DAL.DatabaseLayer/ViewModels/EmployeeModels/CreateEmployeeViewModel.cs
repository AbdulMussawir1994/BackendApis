using DAL.DatabaseLayer.DTOs.EmployeeDto;
using Microsoft.AspNetCore.Http;

namespace DAL.DatabaseLayer.ViewModels.EmployeeModels;

public class CreateEmployeeViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public IFormFile? CV { get; set; }
    public IFormFile? Image { get; set; }
    public string ApplicationUserId { get; set; }
}

public class UpdateEmployeeViewModel
{
    public string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public IFormFile? CV { get; set; }
    public IFormFile? Image { get; set; }
    public string ApplicationUserId { get; set; }
}

public class EmployeeIdViewModel
{
    public required string Id { get; set; }
}

public class EmployeeByIdUpdateViewModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}

public class ViewEmployeeModel
{
    public required int PageSize { get; set; } = 0;
    public required int PageNumber { get; set; } = 0;
}

public class EmployeeListResponse
{
    public IEnumerable<GetEmployeeDto> List { get; set; } = Enumerable.Empty<GetEmployeeDto>();
    public int TotalRecords { get; set; }
}

public class KeysetPaginationRequest
{
    public required string? LastId { get; set; }
    public required int PageSize { get; set; } = 10;
}

//public class EmployeeListViewModel
//{
//    public string Id { get; set; }
//    public string UserName { get; set; }
//    public string EmployeeName { get; set; }
//    public int Age { get; set; }
//    public decimal Salary { get; set; }
//    public string Cv { get; set; }
//    public string Image { get; set; }
//    public string AppUserId { get; set; }

//}