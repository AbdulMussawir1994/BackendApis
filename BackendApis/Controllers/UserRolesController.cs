using DAL.DatabaseLayer.ViewModels.RoleModels;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.BaseController;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApis.Controllers
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserRolesController : WebBaseController
    {
        private readonly IRoleUserService _roleUserService;
        private readonly ILogger<UserRolesController> _logger;

        public UserRolesController(ConfigHandler configHandler, IRoleUserService roleUserService, ILogger<UserRolesController> logger) : base(configHandler)
        {
            _roleUserService = roleUserService;
            _logger = logger;
        }

        [HttpGet("GetAllRoles")]
        public async Task<IActionResult> GetAllRoles() => Ok(await _roleUserService.GetAllRolesAsync());

        [AllowAnonymous]
        [HttpPost("CreateRole")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.CreateRoleAsync(model));
        }

        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers() => Ok(await _roleUserService.GetAllUsersAsync());

        [HttpPost("AddUserToRole")]
        //  [AllowAnonymous]
        // [Authorize]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddUserToRole([FromBody] UserRoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.AddUserToRoleAsync(model));
        }

        [HttpPost("GetUserRoles")]
        public async Task<IActionResult> GetUserRoles([FromBody] RoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.GetUserRolesAsync(model));
        }

        [HttpPost("RemoveUserFromRole")]
        public async Task<IActionResult> RemoveUserFromRole([FromBody] UserRoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.RemoveUserFromRoleAsync(model));
        }

        [HttpPost("GetAllClaims")]
        public async Task<IActionResult> GetAllClaims([FromBody] RoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.GetAllClaimsAsync(model));
        }

        [HttpPost("AddClaimToUser")]
        public async Task<IActionResult> AddClaimToUser([FromBody] ClaimViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.AddClaimToUserAsync(model));
        }

        [HttpPost("RemoveClaims")]
        public async Task<IActionResult> RemoveClaims([FromBody] RoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.RemoveClaimsAsync(model));
        }

        [HttpPost("RemoveClaim")]
        public async Task<IActionResult> RemoveClaim([FromBody] ClaimViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.RemoveClaimAsync(model));
        }
    }
}
