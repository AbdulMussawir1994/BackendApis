using DAL.DatabaseLayer.ViewModels.AuthModels;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.BaseController;
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

        public UserController(IAuthRepository authRepository)
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

    }
}
