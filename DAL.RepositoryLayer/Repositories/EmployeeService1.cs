using DAL.DatabaseLayer.Models;
using DAL.RepositoryLayer.IRepositories;

namespace DAL.RepositoryLayer.Repositories;

public class EmployeeService1
{
    private readonly IRepository<Employee> _employeeRepository;

    public EmployeeService1(IRepository<Employee> employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<Employee>> GetEmployeesAsync()
    {
        return await _employeeRepository.GetAllAsync();
    }

    public async Task<Employee?> GetEmployeeAsync(int id)
    {
        return await _employeeRepository.GetByIdAsync(id);
    }

    public async Task<Employee> AddEmployeeAsync(Employee employee)
    {
        return await _employeeRepository.AddAsync(employee);
    }

    public async Task<int> UpdateEmployeeAsync(Employee employee)
    {
        return await _employeeRepository.UpdateAsync(employee);
    }

    public async Task<int> DeleteEmployeeAsync(Employee employee)
    {
        return await _employeeRepository.DeleteAsync(employee);
    }
}
