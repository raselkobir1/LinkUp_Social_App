using FluentValidation;
using LinkUp.Modules.Friend.Interfaces;
using LinkUp.Modules.Friend.Managers;
using LinkUp.Modules.Friend.Mappings;
using LinkUp.Modules.Friend.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.Friend.Configuration;

public static class FriendModuleExtensions
{
    public static IServiceCollection AddFriendModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddDbContext<FriendDbContext>(options =>
            options.UseNpgsql(connectionString, o =>
                o.MigrationsHistoryTable("__EFMigrationsHistory", "friend")));

        services.AddScoped<IFriendManager, FriendManager>();

        services.AddAutoMapper(typeof(FriendMappingProfile));

        services.AddValidatorsFromAssemblyContaining<SendFriendRequestValidator>();

        return services;
    }
}
