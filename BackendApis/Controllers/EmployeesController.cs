using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.BaseController;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApis.Controllers
{
    [ApiController]
    [Authorize]
    // [AllowAnonymous]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class EmployeesController : WebBaseController
    {
        private readonly IEmployeeRepository _employeeLayer;
        //var record = Task.Run(() => this.GetRecordById(saveRequest.RecordId)).GetAwaiter().GetResult();
        // Task.Run(async () => await this.GetRecordById(saveRequest.RecordId)).Result;

        public EmployeesController(ConfigHandler configHandler, IEmployeeRepository employeeRepository) : base(configHandler)
        {
            _employeeLayer = employeeRepository;
        }

        //   [Authorize(Roles = "Admin")]
        [HttpPost("Enumerable-List")]
        public async Task<ActionResult> GetEmployees([FromBody] ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _employeeLayer.GetEmployeesList(model, cancellationToken));
        }

        [HttpPost("IEnumerable-List")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<ActionResult> GetEmployeesPagination([FromBody] ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _employeeLayer.GetEmployeesPaginationAsync(model, cancellationToken));
        }

        [HttpPost("LongTermTask")]
        public async Task<IActionResult> LongTermTask([FromBody] ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);
            if (!validation.Status.IsSuccess)
                return Ok(validation);

            // ✅ Intentional delay (e.g. simulating long-running task)
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken); // You can set this to 50000ms or more

            // ⏳ Actual fetch after delay
            var result = await _employeeLayer.GetEmployeesPaginationAsync(model, cancellationToken);
            return Ok(result);
        }

        [HttpGet("IEnumerable-List2")]
        public async Task<ActionResult> GetEmployeesPagination2([FromQuery] ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _employeeLayer.GetEmployeesPaginationAsync(model, cancellationToken));
        }

        [HttpPost("IQueryable-List")]
        public async Task<ActionResult> GetEmployeesEnumerable([FromBody] ViewEmployeeModel model)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _employeeLayer.GetEmployeesListAsync(model));
        }

        [HttpPost("IAsyncEnumerable-List")]
        public async Task<ActionResult> GetEmployeesAsync([FromBody] ViewEmployeeModel model)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _employeeLayer.GetEmployeesListAsync2(model));
        }

        [HttpPost("CreateEmployee")]
        public async Task<ActionResult> CreateEmployee([FromForm] CreateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _employeeLayer.CreateEmployeeAsync(model, cancellationToken));
        }

        [HttpPut("UpdateEmployee")]
        public async Task<ActionResult> UpdateEmployee([FromForm] UpdateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess
                ? Ok(validation)
                : Ok(await _employeeLayer.UpdateEmployeeAsync(model, cancellationToken));
        }

        [HttpPost("GetEmployeeById")]
        public async Task<ActionResult> GetEmployeeByIdAsync([FromBody] EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);
            return !validation.Status.IsSuccess
                ? Ok(validation)
                : Ok(await _employeeLayer.GetEmployeeByIdAsync(model, cancellationToken));
        }

        [HttpDelete("Inactivate-Employee")]
        public async Task<ActionResult> DeleteEmployeeAsync([FromBody] EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);
            return !validation.Status.IsSuccess
                ? Ok(validation)
                : Ok(await _employeeLayer.DeleteEmployeeByIdAsync(model, cancellationToken));
        }

        [HttpPatch("PatchEmployee")]
        public async Task<ActionResult> PatchEmployeeAsync([FromBody] EmployeeByIdUpdateViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);
            return !validation.Status.IsSuccess
                ? Ok(validation)
                : Ok(await _employeeLayer.PatchEmployeeAsync(model, cancellationToken));
        }

        [HttpGet("Dictionery")]
        public async Task<ActionResult> GetAllEmployees() => Ok(await _employeeLayer.GetAllEmployeesAsync());


        [HttpGet("FrozenDictionery")]
        public async Task<ActionResult> GetAllEmployees1() => Ok(await _employeeLayer.GetAllEmployeesAsync1());

        [HttpGet("keyset-pagination")]
        public async Task<IActionResult> GetEmployeesKeyset([FromQuery] KeysetPaginationRequest model, CancellationToken cancellationToken = default)
        {
            var validation = this.ModelValidator(model);

            if (!validation.Status.IsSuccess)
                return Ok(validation);

            var result = await _employeeLayer.GetEmployeesKeysetAsync(model, cancellationToken);
            return Ok(result);
        }

    }
}
