//using BackendApis.Controllers;
//using DAL.DatabaseLayer.DTOs.EmployeeDto;
//using DAL.DatabaseLayer.Models;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Distributed;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Moq;

//namespace TestProject.RepositoriesTest
//{
//    public class SampleTesting
//    {
//        private readonly Mock<IConfiguration> _mockIconfiguration;
//        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
//        private readonly Mock<ILogger<EmployeeLayer>> _mockLogger;
//        private readonly Mock<ILogger<EmployeesController>> _mockLoggerController;
//        private readonly IEmployeeLayer _employeeService;
//        private readonly DbContextClass _dbContext;
//        private readonly EmployeesController _controller;
//        private readonly Mock<IEmployeeLayer> _mockEmployeeLayer;
//        private readonly Mock<IDistributedCache> _mockIDistributedCache;

//        private readonly List<Employee> _employeeSeedData;

//        public EmployeeTesting()
//        {
//            var options = new DbContextOptionsBuilder<DbContextClass>()
//                .UseInMemoryDatabase(databaseName: "EmployeeTestDB")
//                .Options;

//            //  _dbContext = new DbContextClass(options);
//            _mockLogger = new Mock<ILogger<EmployeeLayer>>();
//            _mockLoggerController = new Mock<ILogger<EmployeesController>>();
//            _mockIconfiguration = new Mock<IConfiguration>();
//            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
//            _mockIDistributedCache = new Mock<IDistributedCache>();
//            _mockEmployeeLayer = new Mock<IEmployeeLayer>();
//            _controller = new EmployeesController(_mockEmployeeLayer.Object, _mockLoggerController.Object, _mockIDistributedCache.Object);

//            _employeeService = new EmployeeLayer(_mockIconfiguration.Object, _mockLogger.Object, _dbContext);

//            // Seed test data once to avoid multiple database calls
//            _employeeSeedData = new List<Employee>
//        {
//            new Employee { EmployeeName = "Raheel", Age = 29, Salary = 100000, ApplicationUserId = "a27bcbae-b26c-4ce9-ab22-fc9c1f85d2d5" },
//            new Employee { EmployeeName = "Mussawir", Age = 25, Salary = 150000 , ApplicationUserId = "a27bcbae-b26c-4ce9-ab22-fc9c1f85d2d5"},
//            new Employee { EmployeeName = "Azhaan", Age = 35, Salary = 60000, ApplicationUserId = "a27bcbae-b26c-4ce9-ab22-fc9c1f85d2d5" }
//        };

//            _dbContext.Employees.AddRange(_employeeSeedData);
//            _dbContext.SaveChanges();
//        }

//        //[Fact]
//        //public async Task GetByIdAsync_ShouldThrowOperationCanceledException_WhenTokenIsCancelled()
//        //{
//        //    // Arrange
//        //    Employee expectedEmployee = _employeeSeedData.First(e => e.EmployeeName == "Raheel");

//        //    // Create a CancellationTokenSource and cancel it
//        //    using (var cts = new CancellationTokenSource())
//        //    {
//        //        cts.Cancel(); // Cancel the operation
//        //        var cancellationToken = cts.Token;

//        //        // Act & Assert
//        //        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
//        //        {
//        //            await _employeeService.GetIdByAsync(expectedEmployee.EmployeeId, cancellationToken);
//        //        });
//        //    }
//        //}

//        [Fact]
//        public async Task GetByIdAsync_ShouldReturnEmployee_WhenEmployeeExists()
//        {
//            // Arrange
//            Employee expectedEmployee = _employeeSeedData.First(e => e.EmployeeName == "Raheel");
//            CancellationToken cancellationToken = CancellationToken.None;

//            // Act
//            var result = await _employeeService.GetIdByAsync(expectedEmployee.EmployeeId, cancellationToken);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(expectedEmployee.EmployeeName, result.Value.EmployeeName);
//        }

//        [Fact]
//        public async Task GetByIdAsync_ShouldHandleNoEmployeeFound()
//        {
//            // Arrange
//            int nonExistentEmployeeId = 2;
//            int age = 25;
//            CancellationToken cancellationToken = CancellationToken.None;

//            // Act
//            var result = await _employeeService.GetIdByAsync(nonExistentEmployeeId, cancellationToken);

//            // Assert
//            Assert.NotNull(result);
//            Assert.Equal(age, result.Value.Age);
//        }

//        [Fact]
//        public async Task GetByIdAsync_ShouldReturnNotFound_WhenEmployeeDoesNotExist()
//        {
//            // Arrange
//            int nonExistentEmployeeId = 0;
//            CancellationToken cancellationToken = CancellationToken.None;

//            // Act
//            var result = await _employeeService.GetIdByAsync(nonExistentEmployeeId, cancellationToken);

//            // Assert
//            //var actionResult = Assert.IsType<ActionResult<ServiceResponse<GetEmployeeDto>>>(result);
//            //  var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);  // Assert that NotFound is returned
//            //var serviceResponse = Assert.IsType<ServiceResponse<GetEmployeeDto>>(notFoundResult.Value);
//            //Assert.False(serviceResponse.Status);  // Verify that success flag is false
//            Assert.Equal("Employee not found.", result.Message);  // Check the message
//        }

//        [Fact]
//        public async Task GetEmployees_ShouldReturnOk_WhenEmployeesExist()
//        {
//            // Arrange
//            var employeeDtos = new List<GetEmployeeDto>
//    {
//        new GetEmployeeDto { EmployeeId = 1, EmployeeName = "John Doe" },
//        new GetEmployeeDto { EmployeeId = 2, EmployeeName = "Jane Doe" }
//    };

//            var serviceResponse = new ServiceResponse<IEnumerable<GetEmployeeDto>>
//            {
//                Value = employeeDtos,
//                Status = true
//            };

//            _mockEmployeeLayer.Setup(x => x.GetListAsync(It.IsAny<CancellationToken>()))
//                .ReturnsAsync(serviceResponse);

//            // Act
//            var result = await _controller.GetEmployees(CancellationToken.None);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var actualData = Assert.IsAssignableFrom<ServiceResponse<IEnumerable<GetEmployeeDto>>>(okResult.Value);
//            actualData.Value.Should().BeEquivalentTo(employeeDtos);
//        }

//        [Fact]
//        public async Task GetEmployees_ShouldReturnOk_WhenEmployeesExist1()
//        {
//            // Arrange
//            var employeeDtos = new List<GetEmployeeDto>
//    {
//        new GetEmployeeDto { EmployeeId = 1, EmployeeName = "John Doe" },
//        new GetEmployeeDto { EmployeeId = 2, EmployeeName = "Jane Doe" }
//    };

//            var serviceResponse = new ServiceResponse<IEnumerable<GetEmployeeDto>>
//            {
//                Value = employeeDtos,
//                Status = true
//            };

//            _mockEmployeeLayer.Setup(x => x.GetListAsync(It.IsAny<CancellationToken>()))
//                .ReturnsAsync(serviceResponse);

//            // Act
//            var result = await _controller.GetEmployees(CancellationToken.None);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var actualData = Assert.IsAssignableFrom<ServiceResponse<IEnumerable<GetEmployeeDto>>>(okResult.Value);
//            actualData.Value.Should().BeEquivalentTo(employeeDtos);
//        }


//        [Fact]
//        public async Task AddEmployee_ShouldReturnOk_WhenEmployeeIsAddedSuccessfully()
//        {
//            // Arrange
//            var createEmployeeDto = new CreateEmployeeDto
//            {
//                EmployeeName = "John Doe",
//                Age = 30,
//                Salary = 5000, // Ensure that the required Salary field is initialized
//                ApplicationUserId = "" // Assuming this is also a required field
//            };

//            var expectedEmployeeDto = new GetEmployeeDto
//            {
//                EmployeeId = 1,
//                EmployeeName = "John Doe",
//                Age = 30,
//                Salary = 5000 // Add Salary in the expected output
//            };

//            var serviceResponse = new ServiceResponse<GetEmployeeDto>
//            {
//                Value = expectedEmployeeDto,
//                Status = true,
//                Message = "Employee added successfully."
//            };

//            _mockEmployeeLayer
//                .Setup(x => x.AddNewEmployeeAsync(It.IsAny<CreateEmployeeDto>(), It.IsAny<CancellationToken>()))
//                .ReturnsAsync(serviceResponse);

//            // Act
//            var result = await _controller.AddEmployee(createEmployeeDto, CancellationToken.None);

//            // Assert
//            var okResult = result.Result as OkObjectResult;
//            okResult.Should().NotBeNull(); // Ensure the result is not null
//            okResult!.StatusCode.Should().Be(StatusCodes.Status200OK); // Assert the status code is 200 OK

//            var actualResponse = okResult.Value as ServiceResponse<GetEmployeeDto>;
//            actualResponse.Should().NotBeNull(); // Ensure the response is not null
//            actualResponse!.Status.Should().BeTrue(); // Ensure the status is true
//            actualResponse.Value.Should().BeEquivalentTo(expectedEmployeeDto); // Compare the actual and expected result
//        }

//        [Fact]
//        public async Task UpdateEmployee_ShouldReturnOk_WhenEmployeeIsUpdatedSuccessfully()
//        {
//            // Arrange
//            var updateEmployeeDto = new CreateEmployeeDto { EmployeeId = 1, EmployeeName = "John Doe", Age = 30, Salary = 100000 };
//            var serviceResponse = new ServiceResponse<GetEmployeeDto>
//            {
//                Value = new GetEmployeeDto { EmployeeId = 1, EmployeeName = "John Doe" },
//                Status = true
//            };

//            _mockEmployeeLayer.Setup(x => x.UpdateEmployeeAsync(It.IsAny<CreateEmployeeDto>(), It.IsAny<CancellationToken>()))
//                .ReturnsAsync(serviceResponse);

//            // Act
//            var result = await _controller.UpdateEmployee(updateEmployeeDto, CancellationToken.None);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var actualResponse = Assert.IsAssignableFrom<ServiceResponse<GetEmployeeDto>>(okResult.Value);
//            actualResponse.Status.Should().BeTrue();
//            actualResponse.Value.EmployeeName.Should().Be("John Doe");
//        }

//        [Fact]
//        public async Task GetEmployeeById_ShouldReturnOk_WhenEmployeeExists()
//        {
//            // Arrange
//            var employeeDto = new GetEmployeeDto { EmployeeId = 1, EmployeeName = "John Doe" };
//            var serviceResponse = new ServiceResponse<GetEmployeeDto>
//            {
//                Value = employeeDto,
//                Status = true
//            };

//            _mockEmployeeLayer.Setup(x => x.GetIdByAsync(1, It.IsAny<CancellationToken>()))
//                .ReturnsAsync(serviceResponse);

//            // Act
//            var result = await _controller.GetEmployeeByIdAsync(1, CancellationToken.None);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var actualResponse = Assert.IsAssignableFrom<ServiceResponse<GetEmployeeDto>>(okResult.Value);
//            actualResponse.Status.Should().BeTrue();
//            actualResponse.Value.EmployeeName.Should().Be("John Doe");
//        }

//        [Fact]
//        public async Task DeleteEmployee_ShouldReturnOk_WhenEmployeeIsDeletedSuccessfully()
//        {
//            // Arrange
//            var serviceResponse = new ServiceResponse<bool>
//            {
//                Value = true,
//                Status = true
//            };

//            _mockEmployeeLayer.Setup(x => x.DeleteByIdAsync(1, It.IsAny<CancellationToken>()))
//                .ReturnsAsync(serviceResponse);

//            // Act
//            var result = await _controller.DeleteEmployeeAsync(1, CancellationToken.None);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result.Result);
//            var actualResponse = Assert.IsAssignableFrom<ServiceResponse<bool>>(okResult.Value);
//            actualResponse.Status.Should().BeTrue();
//        }
//    }
//}
