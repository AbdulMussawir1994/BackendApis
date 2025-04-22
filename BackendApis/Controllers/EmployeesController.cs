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
    //  [AllowAnonymous]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class EmployeesController : WebBaseController
    {
        private readonly IEmployeeRepository _employeeLayer;

        public EmployeesController(ConfigHandler configHandler, IEmployeeRepository employeeRepository) : base(configHandler)
        {
            _employeeLayer = employeeRepository;
        }

        [HttpPost("Enumerable-List")]
        public async Task<ActionResult> GetEmployees([FromBody] ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _employeeLayer.GetEmployeesList(model, cancellationToken));
        }

        [HttpGet("IQueryable-List")]
        public async Task<ActionResult> GetEmployeesEnumerable([FromBody] ViewEmployeeModel model)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _employeeLayer.GetEmployeesListAsync(model));
        }

        [HttpGet("IAsyncEnumerable-List")]
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

        [HttpGet("GetEmployeeById")]
        public async Task<ActionResult> GetEmployeeByIdAsync([FromBody] EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);
            return !validation.Status.IsSuccess
                ? Ok(validation)
                : Ok(await _employeeLayer.GetEmployeeByIdAsync(model, cancellationToken));
        }

        [HttpDelete("DeleteEmployee")]
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
    }
}
