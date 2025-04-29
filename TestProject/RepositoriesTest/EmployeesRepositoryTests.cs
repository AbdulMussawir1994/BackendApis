using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.Repositories;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace TestProject.RepositoriesTests;

public class EmployeesRepositoryTests
{
    private readonly EmployeeRepository _repository;
    private readonly Mock<IEmployeeDbAccess> _dbMock;
    private readonly ConfigHandler _configHandler;

    public EmployeesRepositoryTests()
    {
        _dbMock = new Mock<IEmployeeDbAccess>();

        var configMock = new Mock<IConfiguration>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var httpClient = new HttpClient();

        _configHandler = new ConfigHandler(configMock.Object, httpClient, httpClientFactoryMock.Object);

        _repository = new EmployeeRepository(_configHandler, _dbMock.Object);
    }

    [Fact]
    public async Task GetEmployeesListAsync_ShouldReturnSuccess()
    {
        var employees = new List<GetEmployeeDto> { new() { EmployeeName = "John" } }.AsQueryable();

        _dbMock.Setup(x => x.GetEmployees(It.IsAny<ViewEmployeeModel>()))
            .Returns(employees);

        var result = await _repository.GetEmployeesListAsync(new ViewEmployeeModel
        {
            PageNumber = 1,
            PageSize = 10
        });

        result.Status.IsSuccess.Should().BeTrue();
        result.Content.Should().NotBeNull();
    }


    [Fact]
    public async Task CreateEmployeeAsync_ShouldReturnSuccess()
    {
        var model = new CreateEmployeeViewModel { Name = "John", ApplicationUserId = Guid.NewGuid().ToString() };

        _dbMock.Setup(x => x.CreateEmployee1(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MobileResponse<string>(_configHandler, "employee").SetSuccess("SUCCESS-200", "Created", null));

        var result = await _repository.CreateEmployeeAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEmployeeAsync_ShouldReturnSuccess()
    {
        var model = new UpdateEmployeeViewModel { Name = "Update", ApplicationUserId = Guid.NewGuid().ToString() };

        _dbMock.Setup(x => x.UpdateEmployee(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _repository.UpdateEmployeeAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_ShouldReturnSuccess()
    {
        var model = new EmployeeIdViewModel { Id = Guid.NewGuid().ToString() };

        _dbMock.Setup(x => x.GetEmployeeById(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetEmployeeDto { EmployeeName = "Test" });

        var result = await _repository.GetEmployeeByIdAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeTrue();
    }
}
