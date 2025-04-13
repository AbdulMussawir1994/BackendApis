using DAL.ServiceLayer.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DAL.RepositoryLayer.IRepositories;

public interface IRoleUserService
{
    Task<MobileResponse<IEnumerable<IdentityRole>>> GetAllRolesAsync(string logId);
    Task<MobileResponse<string>> CreateRoleAsync(string roleName, string logId);
    Task<MobileResponse<IEnumerable<IdentityUser>>> GetAllUsersAsync(string logId);
    Task<MobileResponse<string>> AddUserToRoleAsync(string email, string roleName, string logId);
    Task<MobileResponse<IEnumerable<string>>> GetUserRolesAsync(string email, string logId);
    Task<MobileResponse<string>> RemoveUserFromRoleAsync(string email, string roleName, string logId);
    Task<MobileResponse<IEnumerable<Claim>>> GetAllClaimsAsync(string email, string logId);
    Task<MobileResponse<string>> AddClaimToUserAsync(string email, string claimType, string claimValue, string logId);
    Task<MobileResponse<string>> RemoveClaimsAsync(string email, string logId);
    Task<MobileResponse<string>> RemoveClaimAsync(string email, string claimType, string claimValue, string logId);
}
