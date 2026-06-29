using LinkUp.Modules.VideoCall.Interfaces;
using LinkUp.Modules.VideoCall.Managers;
using LinkUp.Modules.VideoCall.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.VideoCall.Configuration;

public static class VideoCallModuleExtensions
{
    public static IServiceCollection AddVideoCallModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddDbContext<VideoCallDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "videocall")));

        services.AddScoped<IVideoCallManager, VideoCallManager>();
        services.AddAutoMapper(typeof(VideoCallMappingProfile));

        return services;
    }
}
