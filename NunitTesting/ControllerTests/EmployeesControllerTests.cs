using BackendApis.Controllers;
using DAL.DatabaseLayer.DTOs.EmployeeDto;
using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TestProject.Mocks;

namespace NunitTesting.ControllerTests
{
    [TestFixture]
    public class EmployeesControllerTests
    {
        private EmployeeTController _controller;
        private Mock<IEmployeeRepository> _repositoryMock;
        private ConfigHandler _configHandler;

        [SetUp]
        public void SetUp()
        {
            _repositoryMock = new Mock<IEmployeeRepository>();
            _configHandler = MockConfigHandler.Create();
            _controller = new EmployeeTController(_repositoryMock.Object);
        }

        [Test]
        public async Task GetEmployees_WithValidInput_ReturnsOkResultWithEmptyList()
        {
            // Arrange
            var response = new MobileResponse<IEnumerable<GetEmployeeDto>>(_configHandler, "employee")
                .SetSuccess("SUCCESS-200", "Fetched", new List<GetEmployeeDto>());

            _repositoryMock
                .Setup(x => x.GetEmployeesList(It.IsAny<ViewEmployeeModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var viewEmployeeModel = new ViewEmployeeModel { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _controller.GetEmployees(viewEmployeeModel, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var value = okResult.Value as MobileResponse<IEnumerable<GetEmployeeDto>>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.Status.IsSuccess, Is.True);
            Assert.That(value.Content, Is.Empty);
        }

        [Test]
        public async Task GetEmployees_WhenNoEmployeesFound_ReturnsErrorResponse()
        {
            // Arrange
            var response = new MobileResponse<IEnumerable<GetEmployeeDto>>(_configHandler, "employee")
                .SetError("ERR-404", "No employees found.", Enumerable.Empty<GetEmployeeDto>());

            _repositoryMock
                .Setup(x => x.GetEmployeesList(It.IsAny<ViewEmployeeModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var viewEmployeeModel = new ViewEmployeeModel { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _controller.GetEmployees(viewEmployeeModel, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var value = okResult.Value as MobileResponse<IEnumerable<GetEmployeeDto>>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.Status.IsSuccess, Is.False);
            Assert.That(value.Status.StatusMessage, Is.EqualTo("No employees found."));
            Assert.That(value.Content, Is.Empty);
        }

        [Test]
        public async Task GetAllEmployees_WhenEmployeesExist_ReturnsGroupedEmployees()
        {
            // Arrange
            var groupedData = new Dictionary<string, List<GetEmployeeDto>>
            {
                { "IT", new List<GetEmployeeDto> { new GetEmployeeDto { EmployeeName = "John" } } },
                { "HR", new List<GetEmployeeDto> { new GetEmployeeDto { EmployeeName = "Jane" } } }
            };

            var response = new MobileResponse<Dictionary<string, List<GetEmployeeDto>>>(_configHandler, "Employee")
                .SetSuccess("SUCCESS-200", "Employee list fetched successfully.", groupedData);

            _repositoryMock
                .Setup(x => x.GetAllEmployeesAsync())
                .ReturnsAsync(response);

            // Act
            var result = await _controller.GetAllEmployees();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var value = okResult.Value as MobileResponse<Dictionary<string, List<GetEmployeeDto>>>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.Status.IsSuccess, Is.True);
            Assert.That(value.Status.Code, Is.EqualTo("SUCCESS-200"));
            Assert.That(value.Status.StatusMessage, Is.EqualTo("Employee list fetched successfully."));
            Assert.That(value.Content.ContainsKey("IT"), Is.True);
            Assert.That(value.Content["IT"].First().EmployeeName, Is.EqualTo("John"));
        }

        [Test]
        public async Task GetAllEmployees_WhenNoEmployeesExist_ReturnsErrorResponse()
        {
            // Arrange
            var response = new MobileResponse<Dictionary<string, List<GetEmployeeDto>>>(_configHandler, "Employee")
                .SetError("ERR-404", "No employees found.", new Dictionary<string, List<GetEmployeeDto>>());

            _repositoryMock
                .Setup(x => x.GetAllEmployeesAsync())
                .ReturnsAsync(response);

            // Act
            var result = await _controller.GetAllEmployees();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var value = okResult.Value as MobileResponse<Dictionary<string, List<GetEmployeeDto>>>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.Status.IsSuccess, Is.False);
            Assert.That(value.Status.Code, Is.EqualTo("ERR-404"));
            Assert.That(value.Status.StatusMessage, Is.EqualTo("No employees found."));
            Assert.That(value.Content, Is.Empty);
        }

        [Test]
        public async Task CreateEmployee_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            var response = new MobileResponse<bool>(_configHandler, "employee")
                .SetSuccess("SUCCESS-200", "Created", true);

            _repositoryMock
                .Setup(x => x.CreateEmployeeAsync(It.IsAny<CreateEmployeeViewModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var createModel = new CreateEmployeeViewModel();

            // Act
            var result = await _controller.CreateEmployee(createModel, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var value = okResult.Value as MobileResponse<bool>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.Status.IsSuccess, Is.True);
            Assert.That(value.Content, Is.True);
        }

        [Test]
        public async Task UpdateEmployee_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            var response = new MobileResponse<bool>(_configHandler, "employee")
                .SetSuccess("SUCCESS-200", "Updated", true);

            _repositoryMock
                .Setup(x => x.UpdateEmployeeAsync(It.IsAny<UpdateEmployeeViewModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var updateModel = new UpdateEmployeeViewModel();

            // Act
            var result = await _controller.UpdateEmployee(updateModel, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var value = okResult.Value as MobileResponse<bool>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.Status.IsSuccess, Is.True);
            Assert.That(value.Content, Is.True);
        }

        [Test]
        public async Task GetEmployeeByIdAsync_WithValidId_ReturnsEmployee()
        {
            // Arrange
            var employeeDto = new GetEmployeeDto { EmployeeName = "John Doe" };
            var response = new MobileResponse<GetEmployeeDto?>(_configHandler, "employee")
                .SetSuccess("SUCCESS-200", "Found", employeeDto);

            _repositoryMock
                .Setup(x => x.GetEmployeeByIdAsync(It.IsAny<EmployeeIdViewModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var idModel = new EmployeeIdViewModel { Id = "1" };

            // Act
            var result = await _controller.GetEmployeeByIdAsync(idModel, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);

            var value = okResult.Value as MobileResponse<GetEmployeeDto?>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.Status.IsSuccess, Is.True);
            Assert.That(value.Content, Is.Not.Null);
            Assert.That(value.Content.Value.EmployeeName, Is.EqualTo("John Doe"));
        }
    }
}
