using DAL.DatabaseLayer.ViewModels.RoleModels;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.BaseController;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApis.Controllers
{
    [ApiController]
    [AllowAnonymous]
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
        public async Task<ActionResult> GetAllRoles()
        {
            var result = await _roleUserService.GetAllRolesAsync();
            return Ok(result);

        }

        [HttpPost]
        [Route("CreateRole")]
        public async Task<ActionResult> CreateRole([FromBody] CreateRoleViewModel model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _roleUserService.CreateRoleAsync(model);
            return Ok(result);
        }

        [HttpGet]
        [Route("GetAllUsers")]
        public async Task<ActionResult> GetAllUsers()
        {
            var result = await _roleUserService.GetAllUsersAsync();
            return Ok(result);
        }

        [HttpPost]
        [Route("AddUserToRole")]
        public async Task<ActionResult> AddUserToRole([FromBody] UserRoleViewModel model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _roleUserService.AddUserToRoleAsync(model);
            return Ok(result);
        }

        [HttpGet]
        [Route("GetUserRoles")]
        public async Task<ActionResult> GetUserRoles([FromBody] RoleViewModel model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _roleUserService.GetUserRolesAsync(model);
            return Ok(result);
        }

        [HttpPost]
        [Route("RemoveUserFromRole")]
        public async Task<ActionResult> RemoveUserFromRole([FromBody] UserRoleViewModel model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _roleUserService.RemoveUserFromRoleAsync(model);
            return Ok(result);
        }

        [HttpGet]
        [Route("GetAllClaims")]
        public async Task<ActionResult> GetAllClaims([FromBody] RoleViewModel model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _roleUserService.GetAllClaimsAsync(model);
            return Ok(result);
        }

        [HttpPost]
        [Route("AddClaimToUser")]
        public async Task<ActionResult> AddClaimToUser([FromBody] ClaimViewModel model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _roleUserService.AddClaimToUserAsync(model);
            return Ok(result);
        }

        [HttpPost]
        [Route("RemoveClaims")]
        public async Task<ActionResult> RemoveClaims([FromBody] RoleViewModel model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _roleUserService.RemoveClaimsAsync(model);
            return Ok(result);
        }

        [HttpPost]
        [Route("RemoveClaim")]
        public async Task<ActionResult> RemoveClaim([FromBody] ClaimViewModel model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _roleUserService.RemoveClaimAsync(model);
            return Ok(result);
        }
    }
}
