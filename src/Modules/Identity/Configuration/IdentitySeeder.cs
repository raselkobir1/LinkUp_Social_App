using LinkUp.Modules.Identity.Entities;
using LinkUp.SharedKernel.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkUp.Modules.Identity.Configuration;

/// <summary>Seeds default roles and an admin account on application startup.</summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        foreach (var role in new[] { AppConstants.Roles.Admin, AppConstants.Roles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
                logger.LogInformation("Created role {Role}", role);
            }
        }

        const string adminEmail = "admin@linkup.com";
        const string adminPassword = "Admin@123";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FirstName = "System",
                LastName = "Admin",
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AppConstants.Roles.Admin);
                logger.LogInformation("Seeded default admin {Email}", adminEmail);
            }
            else
            {
                logger.LogWarning("Failed to seed admin: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
