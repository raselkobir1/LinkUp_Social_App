using LinkUp.Modules.Admin.Interfaces;
using LinkUp.Modules.Admin.Managers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.Admin.Configuration;

public static class AdminModuleExtensions
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAdminManager, AdminManager>();
        return services;
    }
}
