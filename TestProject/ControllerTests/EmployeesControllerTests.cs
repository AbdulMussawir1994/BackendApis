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
}
