using FluentValidation;
using LinkUp.Modules.Post.Interfaces;
using LinkUp.Modules.Post.Managers;
using LinkUp.Modules.Post.Mappings;
using LinkUp.Modules.Post.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.Post.Configuration;

public static class PostModuleExtensions
{
    public static IServiceCollection AddPostModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddDbContext<PostDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "post")));

        services.AddScoped<IPostManager, PostManager>();

        services.AddAutoMapper(typeof(PostMappingProfile));

        services.AddValidatorsFromAssemblyContaining<CreatePostValidator>();

        return services;
    }
}
