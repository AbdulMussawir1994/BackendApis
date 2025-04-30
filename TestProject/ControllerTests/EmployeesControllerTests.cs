using BackendApis.Controllers;
using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TestProject.Mocks;

namespace TestProject.ControllerTests;

public class EmployeesControllerTests
{
    private readonly EmployeesController _controller;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly ConfigHandler _configHandler;

    public EmployeesControllerTests()
    {
        _repositoryMock = new Mock<IEmployeeRepository>();
        _configHandler = MockConfigHandler.Create(); // ✅ Just one line now
        _controller = new EmployeesController(_configHandler, _repositoryMock.Object);
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnOk()
    {
        var response = new MobileResponse<IEnumerable<GetEmployeeDto>>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Fetched", new List<GetEmployeeDto>());

        _repositoryMock.Setup(x => x.GetEmployeesList(It.IsAny<ViewEmployeeModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var viewEmployeeModel = new ViewEmployeeModel
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _controller.GetEmployees(viewEmployeeModel, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateEmployee_ShouldReturnOk()
    {
        var response = new MobileResponse<bool>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Created", true);

        _repositoryMock.Setup(x => x.CreateEmployeeAsync(It.IsAny<CreateEmployeeViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CreateEmployee(new CreateEmployeeViewModel(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateEmployee_ShouldReturnOk()
    {
        var response = new MobileResponse<bool>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Updated", true);

        _repositoryMock.Setup(x => x.UpdateEmployeeAsync(It.IsAny<UpdateEmployeeViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.UpdateEmployee(new UpdateEmployeeViewModel(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_ShouldReturnOk()
    {
        var response = new MobileResponse<GetEmployeeDto?>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Found", new GetEmployeeDto());

        _repositoryMock.Setup(x => x.GetEmployeeByIdAsync(It.IsAny<EmployeeIdViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetEmployeeByIdAsync(new EmployeeIdViewModel { Id = "1" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllEmployees_ShouldReturnOk()
    {
        var response = new MobileResponse<Dictionary<string, List<GetEmployeeDto>>>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Grouped Employees", new Dictionary<string, List<GetEmployeeDto>>());

        _repositoryMock.Setup(x => x.GetAllEmployeesAsync())
            .ReturnsAsync(response);

        var result = await _controller.GetAllEmployees();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnOk1()
    {
        // Arrange
        var expectedResponse = new MobileResponse<IEnumerable<GetEmployeeDto>>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Fetched", new List<GetEmployeeDto>());

        _repositoryMock
            .Setup(repo => repo.GetEmployeesList(It.IsAny<ViewEmployeeModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var requestModel = new ViewEmployeeModel
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _controller.GetEmployees(requestModel, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateEmployee_ShouldReturnOk1()
    {
        // Arrange
        var expectedResponse = new MobileResponse<bool>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Created", true);

        _repositoryMock
            .Setup(repo => repo.CreateEmployeeAsync(It.IsAny<CreateEmployeeViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var newEmployee = new CreateEmployeeViewModel
        {
            Name = "John Doe",
            ApplicationUserId = Guid.NewGuid().ToString()
        };

        // Act
        var result = await _controller.CreateEmployee(newEmployee, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UpdateEmployee_ShouldReturnOk1()
    {
        // Arrange
        var expectedResponse = new MobileResponse<bool>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Updated", true);

        _repositoryMock
            .Setup(repo => repo.UpdateEmployeeAsync(It.IsAny<UpdateEmployeeViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var updateModel = new UpdateEmployeeViewModel
        {
            Id = Guid.NewGuid().ToString().ToLower(),
            Name = "Updated Name",
            ApplicationUserId = Guid.NewGuid().ToString()
        };

        // Act
        var result = await _controller.UpdateEmployee(updateModel, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_ShouldReturnOk1()
    {
        // Arrange
        var expectedResponse = new MobileResponse<GetEmployeeDto?>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Found", new GetEmployeeDto());

        _repositoryMock
            .Setup(repo => repo.GetEmployeeByIdAsync(It.IsAny<EmployeeIdViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var employeeIdModel = new EmployeeIdViewModel { Id = Guid.NewGuid().ToString() };

        // Act
        var result = await _controller.GetEmployeeByIdAsync(employeeIdModel, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetAllEmployees_ShouldReturnOk1()
    {
        // Arrange
        var expectedResponse = new MobileResponse<Dictionary<string, List<GetEmployeeDto>>>(_configHandler, "employee")
            .SetSuccess("SUCCESS-200", "Grouped Employees", new Dictionary<string, List<GetEmployeeDto>>());

        _repositoryMock
            .Setup(repo => repo.GetAllEmployeesAsync())
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAllEmployees();

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expectedResponse);
    }


    [Fact]
    public async Task GetEmployees_ShouldReturnError_WhenRepositoryFails()
    {
        // Arrange
        var failedResponse = new MobileResponse<IEnumerable<GetEmployeeDto>>(_configHandler, "employee")
            .SetError("ERR-500", "Failed to fetch", null);

        _repositoryMock.Setup(repo => repo.GetEmployeesList(It.IsAny<ViewEmployeeModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        var model = new ViewEmployeeModel { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _controller.GetEmployees(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(failedResponse);
    }

    [Fact]
    public async Task CreateEmployee_ShouldReturnError_WhenRepositoryFails()
    {
        // Arrange
        var failedResponse = new MobileResponse<bool>(_configHandler, "employee")
            .SetError("ERR-400", "Failed to create", false);

        _repositoryMock.Setup(repo => repo.CreateEmployeeAsync(It.IsAny<CreateEmployeeViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        var model = new CreateEmployeeViewModel { Name = "Error Test", ApplicationUserId = Guid.NewGuid().ToString() };

        // Act
        var result = await _controller.CreateEmployee(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(failedResponse);
    }


    [Fact]
    public async Task UpdateEmployee_ShouldReturnError_WhenRepositoryFails()
    {
        // Arrange
        var failedResponse = new MobileResponse<bool>(_configHandler, "employee")
            .SetError("ERR-404", "Employee not found", false);

        _repositoryMock.Setup(repo => repo.UpdateEmployeeAsync(It.IsAny<UpdateEmployeeViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        var model = new UpdateEmployeeViewModel { Id = Guid.NewGuid().ToString(), Name = "Fail", ApplicationUserId = Guid.NewGuid().ToString() };

        // Act
        var result = await _controller.UpdateEmployee(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(failedResponse);
    }


    [Fact]
    public async Task GetEmployeeByIdAsync_ShouldReturnError_WhenNotFound()
    {
        // Arrange
        var failedResponse = new MobileResponse<GetEmployeeDto?>(_configHandler, "employee")
            .SetError("ERR-404", "Employee not found", null);

        _repositoryMock.Setup(repo => repo.GetEmployeeByIdAsync(It.IsAny<EmployeeIdViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        var model = new EmployeeIdViewModel { Id = "not-found-id" };

        // Act
        var result = await _controller.GetEmployeeByIdAsync(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(failedResponse);
    }


    [Fact]
    public async Task GetAllEmployees_ShouldReturnError_WhenEmpty()
    {
        // Arrange
        var failedResponse = new MobileResponse<Dictionary<string, List<GetEmployeeDto>>>(_configHandler, "employee")
            .SetError("ERR-404", "No employees found", new Dictionary<string, List<GetEmployeeDto>>());

        _repositoryMock.Setup(repo => repo.GetAllEmployeesAsync())
            .ReturnsAsync(failedResponse);

        // Act
        var result = await _controller.GetAllEmployees();

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(failedResponse);
    }

}
