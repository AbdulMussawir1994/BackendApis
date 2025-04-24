using DAL.DatabaseLayer.DTOs.AuthDto;
using DAL.DatabaseLayer.ViewModels.AuthModels;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.BaseController;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendApis.Controllers
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController : WebBaseController
    {
        private readonly IAuthRepository _authRepository;

        public UserController(ConfigHandler configHandler, IAuthRepository authRepository) : base(configHandler)
        {
            _authRepository = authRepository;
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                Role = User.FindFirst(ClaimTypes.Role)?.Value,
                IsAuthenticated = User.Identity?.IsAuthenticated
            });
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpGet("admin-only")]
        public IActionResult GetAdminData()
        {
            return Ok("Only admins can access this.");
        }

        [Authorize(Policy = "PermissionPolicy")]
        [HttpGet("with-permission")]
        public IActionResult GetPermissionBasedData()
        {
            return Ok("Users with valid permission claim.");
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("LoginUser")]
        public async Task<ActionResult> Login([FromBody] LoginViewModel model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _authRepository.LoginUser(model, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("RegisterUser")]
        public async Task<ActionResult> NewUser([FromBody] RegisterViewModel model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _authRepository.RegisterUser(model, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("SavePassword")]
        public async Task<ActionResult> SavePassword([FromBody] PasswordViewModel model)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _authRepository.SavePasswordAsync(model);
            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        //  [Authorize(Roles = "Admin,Manager")]
        [Route("GetRefreshToken")]
        public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequest model, CancellationToken cancellationToken)
        {
            var modelValid = this.ModelValidator(model);

            if (!modelValid.Status.IsSuccess)
            {
                return Ok(modelValid);
            }

            var result = await _authRepository.RefreshTokenAsync(model, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("Inactivate-user")]
        [Authorize]
        public async Task<ActionResult<MobileResponse<bool>>> InActivateUserAsync([FromBody] UserIdViewModel model, CancellationToken cancellationToken)
        {
            var validation = this.ModelValidator(model);
            if (!validation.Status.IsSuccess)
                return Ok(validation);

            var result = await _authRepository.InActivateUserAsync(model, cancellationToken);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("GetUsers")]
        public MobileResponse<IEnumerable<GetUsersDto>> GetUsers()
        {
            return _authRepository.GetUsersAsync();
        }
    }
}
