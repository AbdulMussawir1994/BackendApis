using DAL.RepositoryLayer.IRepositories;
using Microsoft.AspNetCore.Mvc;

namespace BackendApis.Controllers
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserRolesController : ControllerBase
    {
        private readonly IRoleUserService _roleUserService;
        private readonly ILogger<UserRolesController> _logger;

        public UserRolesController(IRoleUserService roleUserService, ILogger<UserRolesController> logger)
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

        [HttpPost("CreateRole")]
        public async Task<ActionResult> CreateRole([FromQuery] string roleName, CancellationToken cancellationToken)
        {
            var result = await _roleUserService.CreateRoleAsync(roleName);
            return Ok(result);
        }

        [HttpGet("GetAllUsers")]
        public async Task<ActionResult> GetAllUsers()
        {
            var result = await _roleUserService.GetAllUsersAsync();
            return Ok(result);
        }

        [HttpPost("AddUserToRole")]
        public async Task<ActionResult> AddUserToRole([FromQuery] string email, [FromQuery] string roleName, CancellationToken cancellationToken)
        {
            var result = await _roleUserService.AddUserToRoleAsync(email, roleName);
            return Ok(result);
        }

        [HttpGet("GetUserRoles")]
        public async Task<ActionResult> GetUserRoles([FromQuery] string email, CancellationToken cancellationToken)
        {
            var result = await _roleUserService.GetUserRolesAsync(email);
            return Ok(result);
        }

        [HttpPost("RemoveUserFromRole")]
        public async Task<ActionResult> RemoveUserFromRole([FromQuery] string email, [FromQuery] string roleName, CancellationToken cancellationToken)
        {
            var result = await _roleUserService.RemoveUserFromRoleAsync(email, roleName);
            return Ok(result);
        }

        [HttpGet("GetAllClaims")]
        public async Task<ActionResult> GetAllClaims([FromQuery] string email, CancellationToken cancellationToken)
        {
            var result = await _roleUserService.GetAllClaimsAsync(email);
            return Ok(result);
        }

        [HttpPost("AddClaimToUser")]
        public async Task<ActionResult> AddClaimToUser([FromQuery] string email, [FromQuery] string claimType, [FromQuery] string claimValue, CancellationToken cancellationToken)
        {
            var result = await _roleUserService.AddClaimToUserAsync(email, claimType, claimValue);
            return Ok(result);
        }

        [HttpPost("RemoveClaims")]
        public async Task<ActionResult> RemoveClaims([FromQuery] string email, CancellationToken cancellationToken)
        {
            var result = await _roleUserService.RemoveClaimsAsync(email);
            return Ok(result);
        }

        [HttpPost("RemoveClaim")]
        public async Task<ActionResult> RemoveClaim([FromQuery] string email, [FromQuery] string claimType, [FromQuery] string claimValue, CancellationToken cancellationToken)
        {
            var result = await _roleUserService.RemoveClaimAsync(email, claimType, claimValue);
            return Ok(result);
        }
    }
}
