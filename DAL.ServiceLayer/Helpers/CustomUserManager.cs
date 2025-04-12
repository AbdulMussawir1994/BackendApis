using DAL.DatabaseLayer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DAL.ServiceLayer.Helpers;

public class CustomUserManager : UserManager<AppUser>
{
    public CustomUserManager(
        IUserStore<AppUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<AppUser> passwordHasher,
        IEnumerable<IUserValidator<AppUser>> userValidators,
        IEnumerable<IPasswordValidator<AppUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<AppUser>> logger)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
    }

    // Example wrapper for a protected method
    public async Task<IdentityResult> UpdatePassword(AppUser user, string newPassword, bool validatePassword)
    {
        return await base.UpdatePasswordHash(user, newPassword, validatePassword);
    }
}