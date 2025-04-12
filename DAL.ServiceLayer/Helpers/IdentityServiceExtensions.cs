using DAL.DatabaseLayer.DataContext;
using DAL.DatabaseLayer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAL.ServiceLayer.Helpers;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddEnterpriseIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            // Default SignIn settings.
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;

            options.User.AllowedUserNameCharacters = string.Empty;

            // Default Password settings.
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 0;

            // Default User settings.
            options.User.RequireUniqueEmail = false;

            options.Lockout = new LockoutOptions()
            {
                AllowedForNewUsers = configuration.GetValue<bool>("LockoutSettings:AllowedForNewUsers"),
                MaxFailedAccessAttempts = configuration.GetValue<int>("LockoutSettings:MaxFailedAccessAttempts"),
                DefaultLockoutTimeSpan = TimeSpan.FromMinutes(configuration.GetValue<int>("LockoutSettings:DefaultLockoutInMinutes"))
            };

        })
        .AddEntityFrameworkStores<WebContextDb>()
        .AddDefaultTokenProviders();

        return services;
    }
}