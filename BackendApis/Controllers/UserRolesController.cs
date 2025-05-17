using DAL.DatabaseLayer.ViewModels.RoleModels;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.BaseController;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApis.Controllers
{
    [ApiController]
    //[Authorize]
    // [AllowAnonymous]
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
        public async Task<ActionResult> GetAllRoles() => Ok(await _roleUserService.GetAllRolesAsync());

        // [AllowAnonymous]
        [Authorize]
        [HttpPost("CreateRole")]
        public async Task<ActionResult> CreateRole([FromBody] CreateRoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.CreateRoleAsync(model));
        }

        [HttpGet("GetAllUsers")]
        public async Task<ActionResult> GetAllUsers() => Ok(await _roleUserService.GetAllUsersAsync());

        [HttpPost("AddUserToRole")]
        //  [AllowAnonymous]
        [Authorize]
        // [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AddUserToRole([FromBody] UserRoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.AddUserToRoleAsync(model));
        }

        [HttpPost("GetUserRoles")]
        public async Task<ActionResult> GetUserRoles([FromBody] RoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.GetUserRolesAsync(model));
        }

        [HttpGet("GetUserRolesById")]
        public async Task<ActionResult> GetUserRolesById() => Ok(await _roleUserService.GetUserRolesByIdAsync());

        [HttpPost("RemoveUserFromRole")]
        public async Task<ActionResult> RemoveUserFromRole([FromBody] UserRoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.RemoveUserFromRoleAsync(model));
        }

        [HttpPost("GetAllClaims")]
        public async Task<ActionResult> GetAllClaims([FromBody] RoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.GetAllClaimsAsync(model));
        }

        [HttpPost("AddClaimToUser")]
        public async Task<ActionResult> AddClaimToUser([FromBody] ClaimViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.AddClaimToUserAsync(model));
        }

        [HttpPost("RemoveClaims")]
        public async Task<ActionResult> RemoveClaims([FromBody] RoleViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.RemoveClaimsAsync(model));
        }

        [HttpPost("RemoveClaim")]
        public async Task<ActionResult> RemoveClaim([FromBody] ClaimViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);

            return !validation.Status.IsSuccess ? Ok(validation) : Ok(await _roleUserService.RemoveClaimAsync(model));
        }
    }
}
