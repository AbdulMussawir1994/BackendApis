using DAL.DatabaseLayer.Models;
using DAL.DatabaseLayer.ViewModels.RoleModels;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DAL.RepositoryLayer.Repositories
{
    public class RoleUserService : IRoleUserService
    {
        private readonly ConfigHandler _configHandler;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RoleUserService> _logger;

        public RoleUserService(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager,
                                                     ILogger<RoleUserService> logger, ConfigHandler configHandler)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _configHandler = configHandler;
        }

        public async Task<MobileResponse<IEnumerable<IdentityRole>>> GetAllRolesAsync()
        {
            var response = new MobileResponse<IEnumerable<IdentityRole>>(_configHandler, serviceName: "Roles");

            var roles = _roleManager.Roles.ToList();

            return response.SetSuccess("SUCCESS-200", "Roles fetched", roles);

            //    lockRecord = Task.Run(async () => await _IMakerCheckerService.LockCriteria(setUpForm, action, campaignId)).Result;
        }

        public async Task<MobileResponse<string>> CreateRoleAsync(CreateRoleViewModel model)
        {
            var response = new MobileResponse<string>(_configHandler, serviceName: "Roles");

            if (await _roleManager.RoleExistsAsync(model.RoleName))
                return response.SetError("ERR-400", "Role already exists");

            var result = await _roleManager.CreateAsync(new IdentityRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = model.RoleName,
                NormalizedName = model.RoleName.ToUpperInvariant(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            });

            return result.Succeeded
                ? response.SetSuccess("SUCCESS-201", $"Role '{model.RoleName}' created")
                : response.SetError("ERR-500", $"Failed to create role '{model.RoleName}'");
        }

        public async Task<MobileResponse<IEnumerable<IdentityUser>>> GetAllUsersAsync()
        {
            var response = new MobileResponse<IEnumerable<IdentityUser>>(_configHandler, serviceName: "Roles");

            var users = await Task.FromResult(_userManager.Users.ToList());

            return response.SetSuccess("SUCCESS-200", "Users fetched", users);
        }

        public async Task<MobileResponse<string>> AddUserToRoleAsync(UserRoleViewModel model)
        {
            var response = new MobileResponse<string>(_configHandler, serviceName: "Roles");

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
                return response.SetError("ERR-404", "User not found");

            var result = await _userManager.AddToRoleAsync(user, model.RoleName);

            return result.Succeeded
                ? response.SetSuccess("SUCCESS-200", $"User '{model.Email}' added to role '{model.RoleName}'")
                : response.SetError("ERR-500", $"Failed to add user to role");
        }

        public async Task<MobileResponse<IEnumerable<string>>> GetUserRolesAsync(RoleViewModel model)
        {
            var response = new MobileResponse<IEnumerable<string>>(_configHandler, serviceName: "Roles");

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
                return response.SetError("ERR-404", "User not found");

            var roles = await _userManager.GetRolesAsync(user);

            return response.SetSuccess("SUCCESS-200", "Roles fetched", roles);
        }

        public async Task<MobileResponse<string>> RemoveUserFromRoleAsync(UserRoleViewModel model)
        {
            var response = new MobileResponse<string>(_configHandler, serviceName: "Roles");

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
                return response.SetError("ERR-404", "User not found");

            var result = await _userManager.RemoveFromRoleAsync(user, model.RoleName);

            return result.Succeeded
                ? response.SetSuccess("SUCCESS-200", $"User '{model.Email}' removed from role '{model.RoleName}'")
                : response.SetError("ERR-500", "Failed to remove user from role");
        }

        public async Task<MobileResponse<IEnumerable<Claim>>> GetAllClaimsAsync(RoleViewModel model)
        {
            var response = new MobileResponse<IEnumerable<Claim>>(_configHandler, serviceName: "Roles");

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
                return response.SetError("ERR-404", "User not found");

            var claims = await _userManager.GetClaimsAsync(user);

            return response.SetSuccess("SUCCESS-200", "Claims fetched", claims);
        }

        public async Task<MobileResponse<string>> AddClaimToUserAsync(ClaimViewModel model)
        {
            var response = new MobileResponse<string>(_configHandler, serviceName: "Roles");

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
                return response.SetError("ERR-404", "User not found");

            var result = await _userManager.AddClaimAsync(user, new Claim(model.ClaimType, model.ClaimValue));

            return result.Succeeded
                ? response.SetSuccess("SUCCESS-200", $"Claim added to user '{model.Email}'")
                : response.SetError("ERR-500", "Failed to add claim");
        }

        public async Task<MobileResponse<string>> RemoveClaimsAsync(RoleViewModel mode)
        {
            var response = new MobileResponse<string>(_configHandler, serviceName: "Roles");

            var user = await _userManager.FindByEmailAsync(mode.Email);

            if (user is null)
                return response.SetError("ERR-404", "User not found");

            var claims = await _userManager.GetClaimsAsync(user);
            var result = await _userManager.RemoveClaimsAsync(user, claims);

            return result.Succeeded
                ? response.SetSuccess("SUCCESS-200", "All claims removed")
                : response.SetError("ERR-500", "Failed to remove claims");
        }

        public async Task<MobileResponse<string>> RemoveClaimAsync(ClaimViewModel model)
        {
            var response = new MobileResponse<string>(_configHandler, serviceName: "Roles");

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
                return response.SetError("ERR-404", "User not found");

            var claims = await _userManager.GetClaimsAsync(user);

            var claimToRemove = claims.FirstOrDefault(c => c.Type == model.ClaimType && c.Value == model.ClaimValue);

            if (claimToRemove is null)
                return response.SetError("ERR-404", "Claim not found");

            var result = await _userManager.RemoveClaimAsync(user, claimToRemove);

            return result.Succeeded
                ? response.SetSuccess("SUCCESS-200", "Claim removed")
                : response.SetError("ERR-500", "Failed to remove claim");
        }
    }
}
