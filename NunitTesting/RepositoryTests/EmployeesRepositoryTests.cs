using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.Repositories;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace NunitTesting.RepositoryTests;

[TestFixture]
public class EmployeesRepositoryTests
{
    private EmployeeRepository _repository;
    private Mock<IEmployeeDbAccess> _dbMock;
    private ConfigHandler _configHandler;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IEmployeeDbAccess>();
        var configMock = new Mock<IConfiguration>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var httpClient = new HttpClient();
        _configHandler = new ConfigHandler(configMock.Object, httpClient, httpClientFactoryMock.Object);
        _repository = new EmployeeRepository(_configHandler, _dbMock.Object);
    }

    [Test]
    public async Task GetEmployeesListAsync_ShouldReturnSuccess()
    {
        var employees = new List<GetEmployeeDto> { new() { EmployeeName = "John" } }.AsQueryable();
        _dbMock.Setup(x => x.GetEmployees(It.IsAny<ViewEmployeeModel>())).Returns(employees);

        var result = await _repository.GetEmployeesListAsync(new ViewEmployeeModel { PageNumber = 1, PageSize = 10 });

        result.Status.IsSuccess.Should().BeTrue();
        result.Content.Should().NotBeNull();
        result.Status.StatusMessage.Should().Be("Employee list fetched successfully.");
    }

    [Test]
    public async Task EmployeesList_ShouldReturnNotFound_WhenNoEmployeesExist()
    {
        _dbMock.Setup(x => x.GetEmployees(It.IsAny<ViewEmployeeModel>())).Returns(Enumerable.Empty<GetEmployeeDto>().AsQueryable());

        var result = await _repository.GetEmployeesListAsync(new ViewEmployeeModel { PageNumber = 1, PageSize = 10 });

        result.Status.IsSuccess.Should().BeFalse();
        result.Status.Code.Should().Be("ERR-404");
        result.Status.StatusMessage.Should().Be("No employees found.");
        result.Content.Should().BeNull();
    }

    [Test]
    public async Task CreateEmployeeAsync_ShouldReturnSuccess()
    {
        var model = new CreateEmployeeViewModel { Name = "John", ApplicationUserId = Guid.NewGuid().ToString() };

        _dbMock.Setup(x => x.CreateEmployee1(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MobileResponse<string>(_configHandler, "employee").SetSuccess("SUCCESS-200", "Created", null));

        var result = await _repository.CreateEmployeeAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UpdateEmployeeAsync_ShouldReturnSuccess()
    {
        var model = new UpdateEmployeeViewModel { Name = "Update", ApplicationUserId = Guid.NewGuid().ToString() };

        _dbMock.Setup(x => x.UpdateEmployee(model, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _repository.UpdateEmployeeAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task GetEmployeeByIdAsync_ShouldReturnSuccess()
    {
        var model = new EmployeeIdViewModel { Id = Guid.NewGuid().ToString() };
        _dbMock.Setup(x => x.GetEmployeeById(model, It.IsAny<CancellationToken>())).ReturnsAsync(new GetEmployeeDto { EmployeeName = "Test" });

        var result = await _repository.GetEmployeeByIdAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CreateEmployeeAsync_ShouldReturnError_WhenApplicationUserIdInvalid()
    {
        var model = new CreateEmployeeViewModel { Name = "Invalid", ApplicationUserId = "not-a-guid" };

        var result = await _repository.CreateEmployeeAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("Invalid application user ID.");
    }

    [Test]
    public async Task GetEmployeeByIdAsync_ShouldReturnError_WhenEmployeeNotFound()
    {
        var model = new EmployeeIdViewModel { Id = Guid.NewGuid().ToString() };
        _dbMock.Setup(x => x.GetEmployeeById(model, It.IsAny<CancellationToken>())).ReturnsAsync((GetEmployeeDto?)null);

        var result = await _repository.GetEmployeeByIdAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("Employee not found.");
    }

    [Test]
    public async Task UpdateEmployeeAsync_ShouldReturnError_WhenApplicationUserIdIsInvalid()
    {
        var model = new UpdateEmployeeViewModel { Name = "Name", ApplicationUserId = "bad-guid" };

        var result = await _repository.UpdateEmployeeAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("Invalid application user ID.");
    }

    [Test]
    public async Task GetEmployeeByIdAsync_ShouldHandleException_Gracefully()
    {
        var model = new EmployeeIdViewModel { Id = Guid.NewGuid().ToString() };

        _dbMock.Setup(x => x.GetEmployeeById(It.IsAny<EmployeeIdViewModel>(), It.IsAny<CancellationToken>()))
               .Throws(new Exception("Database error"));

        var result = await _repository.GetEmployeeByIdAsyncForTest(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Contain("unexpected error");
        result.Status.Code.Should().Be("ERR-500");
    }
}
