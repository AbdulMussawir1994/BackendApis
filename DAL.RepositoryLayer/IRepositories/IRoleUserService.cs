using DAL.DatabaseLayer.ViewModels.RoleModels;
using DAL.ServiceLayer.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DAL.RepositoryLayer.IRepositories;

public interface IRoleUserService
{
    Task<MobileResponse<IEnumerable<IdentityRole>>> GetAllRolesAsync();
    Task<MobileResponse<string>> CreateRoleAsync(CreateRoleViewModel model);
    Task<MobileResponse<IEnumerable<IdentityUser>>> GetAllUsersAsync();
    Task<MobileResponse<string>> AddUserToRoleAsync(UserRoleViewModel model);
    Task<MobileResponse<IEnumerable<string>>> GetUserRolesAsync(RoleViewModel model);
    Task<MobileResponse<IEnumerable<string>>> GetUserRolesByIdAsync();
    Task<MobileResponse<string>> RemoveUserFromRoleAsync(UserRoleViewModel model);
    Task<MobileResponse<IEnumerable<Claim>>> GetAllClaimsAsync(RoleViewModel model);
    Task<MobileResponse<string>> AddClaimToUserAsync(ClaimViewModel model);
    Task<MobileResponse<string>> RemoveClaimsAsync(RoleViewModel model);
    Task<MobileResponse<string>> RemoveClaimAsync(ClaimViewModel model);
}
