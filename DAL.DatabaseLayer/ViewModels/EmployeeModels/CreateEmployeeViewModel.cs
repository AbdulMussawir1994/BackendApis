using Microsoft.AspNetCore.Http;

namespace DAL.DatabaseLayer.ViewModels.EmployeeModels;

public class CreateEmployeeViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public IFormFile? CV { get; set; }
    public IFormFile? Image { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
}

public class EmployeeIdViewModel
{
    public int Id { get; set; }
}

public class EmployeeUpdateViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
}