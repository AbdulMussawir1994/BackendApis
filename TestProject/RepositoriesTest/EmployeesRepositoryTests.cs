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
    public async Task EmployeesList_ShouldReturnNotFound_WhenNoEmployeesExist()
    {
        // Arrange
        var emptyEmployees = Enumerable.Empty<GetEmployeeDto>().AsQueryable();

        _dbMock.Setup(x => x.GetEmployees(It.IsAny<ViewEmployeeModel>()))
               .Returns(emptyEmployees);

        var request = new ViewEmployeeModel
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetEmployeesListAsync(request);

        // Assert
        result.Status.IsSuccess.Should().BeFalse();
        result.Status.Code.Should().Be("ERR-404");
        result.Status.StatusMessage.Should().Be("No employees found.");
        result.Content.Should().BeNull(); // or BeNullOrEmpty() if you allow empty list
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

    [Fact]
    public async Task GetEmployeesListAsync_ShouldReturnSuccess1()
    {
        // Arrange
        var expectedEmployees = new List<GetEmployeeDto> { new() { EmployeeName = "John" } }.AsQueryable();

        var viewModel = new ViewEmployeeModel
        {
            PageNumber = 1,
            PageSize = 10
        };

        _dbMock.Setup(x => x.GetEmployees(It.IsAny<ViewEmployeeModel>()))
               .Returns(expectedEmployees);

        // Act
        var result = await _repository.GetEmployeesListAsync(viewModel);

        // Assert
        result.Status.IsSuccess.Should().BeTrue();
        result.Content.Should().NotBeNull();
        result.Content.Should().BeEquivalentTo(expectedEmployees);
    }

    [Fact]
    public async Task CreateEmployeeAsync_ShouldReturnSuccess1()
    {
        // Arrange
        var model = new CreateEmployeeViewModel
        {
            Name = "John",
            ApplicationUserId = Guid.NewGuid().ToString()
        };

        var expectedResponse = new MobileResponse<string>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Employee created successfully", null);

        _dbMock.Setup(x => x.CreateEmployee1(model, It.IsAny<CancellationToken>()))
               .ReturnsAsync(expectedResponse);

        // Act
        var result = await _repository.CreateEmployeeAsync(model, CancellationToken.None);

        // Assert
        result.Status.IsSuccess.Should().BeTrue();
        result.Status.StatusMessage.Should().Be("Employee created successfully.");
    }

    [Fact]
    public async Task UpdateEmployeeAsync_ShouldReturnSuccess1()
    {
        // Arrange
        var model = new UpdateEmployeeViewModel
        {
            Name = "Update",
            ApplicationUserId = Guid.NewGuid().ToString()
        };

        _dbMock.Setup(x => x.UpdateEmployee(model, It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        // Act
        var result = await _repository.UpdateEmployeeAsync(model, CancellationToken.None);

        // Assert
        result.Status.IsSuccess.Should().BeTrue();
        result.Status.StatusMessage.Should().Be("Employee updated successfully.");
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_ShouldReturnSuccess1()
    {
        // Arrange
        var model = new EmployeeIdViewModel { Id = Guid.NewGuid().ToString() };
        var expectedEmployee = new GetEmployeeDto { EmployeeName = "Test" };

        _dbMock.Setup(x => x.GetEmployeeById(model, It.IsAny<CancellationToken>()))
               .ReturnsAsync(expectedEmployee);

        // Act
        var result = await _repository.GetEmployeeByIdAsync(model, CancellationToken.None);

        // Assert
        result.Status.IsSuccess.Should().BeTrue();
        result.Content.Value.EmployeeName.Should().Be("Test");
    }

    [Fact]
    public async Task GetEmployeesListAsync_ShouldReturnError_WhenNoEmployeesFound()
    {
        // Arrange
        var viewModel = new ViewEmployeeModel { PageNumber = 1, PageSize = 10 };
        var emptyResult = Enumerable.Empty<GetEmployeeDto>().AsQueryable();

        _dbMock.Setup(x => x.GetEmployees(viewModel)).Returns(emptyResult);

        // Act
        var result = await _repository.GetEmployeesListAsync(viewModel);

        // Assert
        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("No employees found.");
        result.Content.Should().BeNull();
    }

    [Fact]
    public async Task CreateEmployeeAsync_ShouldReturnError_WhenApplicationUserIdInvalid()
    {
        // Arrange
        var model = new CreateEmployeeViewModel
        {
            Name = "Invalid",
            ApplicationUserId = "not-a-guid" // ❌ Invalid
        };

        // Act
        var result = await _repository.CreateEmployeeAsync(model, CancellationToken.None);

        // Assert
        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("Invalid application user ID.");
    }

    [Fact]
    public async Task UpdateEmployeeAsync_ShouldReturnError_WhenUpdateFails()
    {
        // Arrange
        var model = new UpdateEmployeeViewModel
        {
            Name = "Fail",
            ApplicationUserId = Guid.NewGuid().ToString()
        };

        _dbMock.Setup(x => x.UpdateEmployee(model, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _repository.UpdateEmployeeAsync(model, CancellationToken.None);

        // Assert
        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("Failed to update employee.");
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_ShouldReturnError_WhenEmployeeNotFound()
    {
        // Arrange
        var model = new EmployeeIdViewModel { Id = Guid.NewGuid().ToString() };

        _dbMock.Setup(x => x.GetEmployeeById(model, It.IsAny<CancellationToken>())).ReturnsAsync((GetEmployeeDto?)null);

        // Act
        var result = await _repository.GetEmployeeByIdAsync(model, CancellationToken.None);

        // Assert
        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("Employee not found.");
    }

    [Fact]
    public async Task CreateEmployeeAsync_ShouldReturnError_WhenModelIsNull()
    {
        // Act
        var result = await _repository.CreateEmployeeAsync(null, CancellationToken.None);

        // Assert
        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("Model cannot be null.");
    }

    [Fact]
    public async Task CreateEmployeeAsync_ShouldReturnError_WhenNameIsEmpty()
    {
        var model = new CreateEmployeeViewModel { Name = "", ApplicationUserId = Guid.NewGuid().ToString() };

        var result = await _repository.CreateEmployeeAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("Employee name is required.");
    }

    [Fact]
    public async Task UpdateEmployeeAsync_ShouldReturnError_WhenApplicationUserIdIsInvalid()
    {
        var model = new UpdateEmployeeViewModel { Name = "Name", ApplicationUserId = "bad-guid" };

        var result = await _repository.UpdateEmployeeAsync(model, CancellationToken.None);

        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("Invalid application user ID.");
    }

    [Fact]
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

    [Fact]
    public async Task CreateEmployeeAsync_ShouldReturnError_WhenEmployeeAlreadyExists()
    {
        // Arrange
        var model = new CreateEmployeeViewModel
        {
            Name = "John",
            ApplicationUserId = Guid.NewGuid().ToString()
        };

        var response = new MobileResponse<string>(_configHandler, "employee");

        _dbMock.Setup(x => x.CreateEmployee1(It.IsAny<CreateEmployeeViewModel>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(response);

        // Act
        var result = await _repository.CreateEmployeeAsync(model, CancellationToken.None);

        // Assert
        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("Failed to create employee.");
    }

    [Fact]
    public async Task GetEmployeesListAsync_ShouldHandleEmptyPageGracefully()
    {
        var model = new ViewEmployeeModel { PageNumber = 999, PageSize = 10 };
        _dbMock.Setup(x => x.GetEmployees(model)).Returns(Enumerable.Empty<GetEmployeeDto>().AsQueryable());

        var result = await _repository.GetEmployeesListAsync(model);

        result.Status.IsSuccess.Should().BeFalse();
        result.Status.StatusMessage.Should().Be("No employees found.");
    }


}
