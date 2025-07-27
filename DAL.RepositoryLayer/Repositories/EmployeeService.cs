using DAL.DatabaseLayer.Models;
using DAL.RepositoryLayer.IRepositories;

namespace DAL.RepositoryLayer.Repositories;

public class EmployeeService
{
    private readonly IUnitOfWork _unitOfWork;

    public EmployeeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Employee>> GetEmployeesByNameAsync(string name)
    {
        return await _unitOfWork.Repository<Employee>().FindAsync(e => e.Name == name);
    }

    public async Task<(IEnumerable<Employee>, int)> GetPagedEmployeesAsync(int page, int size)
    {
        return await _unitOfWork.Repository<Employee>().GetPagedAsync(page, size);
    }

    public async Task<Employee> AddEmployeeAsync(Employee employee)
    {
        var repo = _unitOfWork.Repository<Employee>();
        await repo.AddAsync(employee);
        await _unitOfWork.SaveChangesAsync();
        return employee;
    }
}