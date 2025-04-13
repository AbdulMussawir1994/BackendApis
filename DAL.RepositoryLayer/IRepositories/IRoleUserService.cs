using DAL.ServiceLayer.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DAL.RepositoryLayer.IRepositories;

public interface IRoleUserService
{
    Task<MobileResponse<IEnumerable<IdentityRole>>> GetAllRolesAsync();
    Task<MobileResponse<string>> CreateRoleAsync(string roleName);
    Task<MobileResponse<IEnumerable<IdentityUser>>> GetAllUsersAsync();
    Task<MobileResponse<string>> AddUserToRoleAsync(string email, string roleName);
    Task<MobileResponse<IEnumerable<string>>> GetUserRolesAsync(string email);
    Task<MobileResponse<string>> RemoveUserFromRoleAsync(string email, string roleName);
    Task<MobileResponse<IEnumerable<Claim>>> GetAllClaimsAsync(string email);
    Task<MobileResponse<string>> AddClaimToUserAsync(string email, string claimType, string claimValue);
    Task<MobileResponse<string>> RemoveClaimsAsync(string email);
    Task<MobileResponse<string>> RemoveClaimAsync(string email, string claimType, string claimValue);
}
