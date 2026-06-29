using CloudinaryDotNet;
using FluentValidation;
using LinkUp.Modules.Media.Interfaces;
using LinkUp.Modules.Media.Mappings;
using LinkUp.Modules.Media.Services;
using LinkUp.Modules.Media.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.Media.Configuration;

public static class MediaModuleExtensions
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddDbContext<MediaDbContext>(options =>
            options.UseNpgsql(connectionString, o =>
                o.MigrationsHistoryTable("__EFMigrationsHistory", "media")));

        // Register Cloudinary as singleton
        var cloudName = configuration["Cloudinary:CloudName"]!;
        var apiKey = configuration["Cloudinary:ApiKey"]!;
        var apiSecret = configuration["Cloudinary:ApiSecret"]!;

        var account = new Account(cloudName, apiKey, apiSecret);
        var cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        services.AddSingleton(cloudinary);

        services.AddScoped<IMediaService, CloudinaryMediaService>();

        services.AddAutoMapper(typeof(MediaMappingProfile));

        services.AddValidatorsFromAssemblyContaining<ImageUploadValidator>();

        return services;
    }
}
