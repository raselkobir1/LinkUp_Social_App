using LinkUp.Modules.Identity.Configuration;
using LinkUp.Modules.Identity.Interfaces;
using LinkUp.Modules.Identity.Managers;
using LinkUp.Modules.Identity.Mappings;
using LinkUp.Modules.Identity.Services;
using LinkUp.Modules.Identity.Validators;
using LinkUp.Modules.Identity.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.Identity.Configuration;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthManager, AuthManager>();
        services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
        services.AddAutoMapper(typeof(IdentityMappingProfile));

        return services;
    }
}
