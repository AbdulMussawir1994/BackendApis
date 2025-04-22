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