using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DAL.RepositoryLayer.Repositories
{
    // Application Layer Implementation
    public class RoleUserService : IRoleUserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RoleUserService> _logger;

        public RoleUserService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<RoleUserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<MobileResponse<IEnumerable<IdentityRole>>> GetAllRolesAsync(string logId)
        {
            var response = new MobileResponse<IEnumerable<IdentityRole>>(logId);
            var roles = _roleManager.Roles.ToList();
            return response.SetSuccess("SUCCESS-200", "Roles fetched", roles);
        }

        public async Task<MobileResponse<string>> CreateRoleAsync(string roleName, string logId)
        {
            var response = new MobileResponse<string>(logId);
            if (await _roleManager.RoleExistsAsync(roleName))
                return response.SetError("ERR-400", "Role already exists");

            var result = await _roleManager.CreateAsync(new IdentityRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            });

            return result.Succeeded
                ? response.SetSuccess("SUCCESS-201", $"Role '{roleName}' created")
                : response.SetError("ERR-500", $"Failed to create role '{roleName}'");
        }

        public async Task<MobileResponse<IEnumerable<IdentityUser>>> GetAllUsersAsync(string logId)
        {
            var response = new MobileResponse<IEnumerable<IdentityUser>>(logId);
            var users = await Task.FromResult(_userManager.Users.ToList());
            return response.SetSuccess("SUCCESS-200", "Users fetched", users);
        }

        public async Task<MobileResponse<string>> AddUserToRoleAsync(string email, string roleName, string logId)
        {
            var response = new MobileResponse<string>(logId);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return response.SetError("ERR-404", "User not found");

            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded
                ? response.SetSuccess("SUCCESS-200", $"User '{email}' added to role '{roleName}'")
                : response.SetError("ERR-500", $"Failed to add user to role");
        }

        public async Task<MobileResponse<IEnumerable<string>>> GetUserRolesAsync(string email, string logId)
        {
            var response = new MobileResponse<IEnumerable<string>>(logId);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return response.SetError("ERR-404", "User not found");
            var roles = await _userManager.GetRolesAsync(user);
            return response.SetSuccess("SUCCESS-200", "Roles fetched", roles);
        }

        public async Task<MobileResponse<string>> RemoveUserFromRoleAsync(string email, string roleName, string logId)
        {
            var response = new MobileResponse<string>(logId);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return response.SetError("ERR-404", "User not found");

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded
                ? response.SetSuccess("SUCCESS-200", $"User '{email}' removed from role '{roleName}'")
                : response.SetError("ERR-500", "Failed to remove user from role");
        }

        public async Task<MobileResponse<IEnumerable<Claim>>> GetAllClaimsAsync(string email, string logId)
        {
            var response = new MobileResponse<IEnumerable<Claim>>(logId);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return response.SetError("ERR-404", "User not found");
            var claims = await _userManager.GetClaimsAsync(user);
            return response.SetSuccess("SUCCESS-200", "Claims fetched", claims);
        }

        public async Task<MobileResponse<string>> AddClaimToUserAsync(string email, string claimType, string claimValue, string logId)
        {
            var response = new MobileResponse<string>(logId);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return response.SetError("ERR-404", "User not found");

            var result = await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
            return result.Succeeded
                ? response.SetSuccess("SUCCESS-200", $"Claim added to user '{email}'")
                : response.SetError("ERR-500", "Failed to add claim");
        }

        public async Task<MobileResponse<string>> RemoveClaimsAsync(string email, string logId)
        {
            var response = new MobileResponse<string>(logId);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return response.SetError("ERR-404", "User not found");

            var claims = await _userManager.GetClaimsAsync(user);
            var result = await _userManager.RemoveClaimsAsync(user, claims);

            return result.Succeeded
                ? response.SetSuccess("SUCCESS-200", "All claims removed")
                : response.SetError("ERR-500", "Failed to remove claims");
        }

        public async Task<MobileResponse<string>> RemoveClaimAsync(string email, string claimType, string claimValue, string logId)
        {
            var response = new MobileResponse<string>(logId);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return response.SetError("ERR-404", "User not found");

            var claims = await _userManager.GetClaimsAsync(user);
            var claimToRemove = claims.FirstOrDefault(c => c.Type == claimType && c.Value == claimValue);
            if (claimToRemove == null) return response.SetError("ERR-404", "Claim not found");

            var result = await _userManager.RemoveClaimAsync(user, claimToRemove);
            return result.Succeeded
                ? response.SetSuccess("SUCCESS-200", "Claim removed")
                : response.SetError("ERR-500", "Failed to remove claim");
        }
    }
}
