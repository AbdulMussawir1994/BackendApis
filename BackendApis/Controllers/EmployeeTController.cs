using DAL.DatabaseLayer.ViewModels.EmployeeModels;
using DAL.RepositoryLayer.IRepositories;
using Microsoft.AspNetCore.Mvc;

namespace BackendApis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)] // 🔒 This hides the entire controller
    public class EmployeeTController : ControllerBase
    {
        private readonly IEmployeeRepository _employeeLayer;

        public EmployeeTController(IEmployeeRepository employeeRepository)
        {
            _employeeLayer = employeeRepository;
        }

        [HttpPost("Enumerable-List")]
        public async Task<ActionResult> GetEmployees([FromBody] ViewEmployeeModel model, CancellationToken cancellationToken)
        {

            return Ok(await _employeeLayer.GetEmployeesList(model, cancellationToken));
        }

        [HttpPost("IEnumerable-List")]
        public async Task<ActionResult> GetEmployeesPagination([FromBody] ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            return Ok(await _employeeLayer.GetEmployeesPaginationAsync(model, cancellationToken));
        }

        [HttpGet("IEnumerable-List2")]
        public async Task<ActionResult> GetEmployeesPagination2([FromQuery] ViewEmployeeModel model, CancellationToken cancellationToken)
        {
            return Ok(await _employeeLayer.GetEmployeesPaginationAsync(model, cancellationToken));
        }

        [HttpPost("IQueryable-List")]
        public async Task<ActionResult> GetEmployeesEnumerable([FromBody] ViewEmployeeModel model)
        {
            return Ok(await _employeeLayer.GetEmployeesListAsync(model));
        }

        [HttpPost("IAsyncEnumerable-List")]
        public async Task<ActionResult> GetEmployeesAsync([FromBody] ViewEmployeeModel model)
        {

            return Ok(await _employeeLayer.GetEmployeesListAsync2(model));
        }

        [HttpPost("CreateEmployee")]
        public async Task<ActionResult> CreateEmployee([FromForm] CreateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            return Ok(await _employeeLayer.CreateEmployeeAsync(model, cancellationToken));
        }

        [HttpPut("UpdateEmployee")]
        public async Task<ActionResult> UpdateEmployee([FromForm] UpdateEmployeeViewModel model, CancellationToken cancellationToken)
        {
            return Ok(await _employeeLayer.UpdateEmployeeAsync(model, cancellationToken));
        }

        [HttpPost("GetEmployeeById")]
        public async Task<ActionResult> GetEmployeeByIdAsync([FromBody] EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            return Ok(await _employeeLayer.GetEmployeeByIdAsync(model, cancellationToken));
        }

        [HttpDelete("Inactivate-Employee")]
        public async Task<ActionResult> DeleteEmployeeAsync([FromBody] EmployeeIdViewModel model, CancellationToken cancellationToken)
        {
            return Ok(await _employeeLayer.DeleteEmployeeByIdAsync(model, cancellationToken));
        }

        [HttpPatch("PatchEmployee")]
        public async Task<ActionResult> PatchEmployeeAsync([FromBody] EmployeeByIdUpdateViewModel model, CancellationToken cancellationToken)
        {
            return Ok(await _employeeLayer.PatchEmployeeAsync(model, cancellationToken));
        }

        [HttpGet("GetAllEmployees")]
        public async Task<ActionResult> GetAllEmployees() => Ok(await _employeeLayer.GetAllEmployeesAsync());

        [HttpGet("keyset-pagination")]
        public async Task<IActionResult> GetEmployeesKeyset([FromQuery] KeysetPaginationRequest model, CancellationToken cancellationToken = default)
        {

            return Ok(await _employeeLayer.GetEmployeesKeysetAsync(model, cancellationToken));
        }
    }
}
