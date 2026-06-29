using FluentValidation;
using LinkUp.Modules.UserProfile.Interfaces;
using LinkUp.Modules.UserProfile.Managers;
using LinkUp.Modules.UserProfile.Mappings;
using LinkUp.Modules.UserProfile.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.UserProfile.Configuration;

public static class UserProfileModuleExtensions
{
    public static IServiceCollection AddUserProfileModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddDbContext<ProfileDbContext>(options =>
            options.UseNpgsql(connectionString, o =>
                o.MigrationsHistoryTable("__EFMigrationsHistory", "profile")));

        services.AddScoped<IProfileManager, ProfileManager>();

        services.AddAutoMapper(typeof(ProfileMappingProfile));

        services.AddValidatorsFromAssemblyContaining<UpdateProfileValidator>();

        return services;
    }
}
