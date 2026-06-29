using LinkUp.Modules.Notification.Interfaces;
using LinkUp.Modules.Notification.Managers;
using LinkUp.Modules.Notification.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.Notification.Configuration;

public static class NotificationModuleExtensions
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "notification")));

        services.AddScoped<INotificationManager, NotificationManager>();
        services.AddAutoMapper(typeof(NotificationMappingProfile));

        return services;
    }
}
