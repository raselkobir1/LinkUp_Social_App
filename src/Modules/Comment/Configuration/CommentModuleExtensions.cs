using FluentValidation;
using LinkUp.Modules.Comment.Interfaces;
using LinkUp.Modules.Comment.Managers;
using LinkUp.Modules.Comment.Mappings;
using LinkUp.Modules.Comment.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.Comment.Configuration;

public static class CommentModuleExtensions
{
    public static IServiceCollection AddCommentModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddDbContext<CommentDbContext>(options =>
            options.UseNpgsql(connectionString, o =>
                o.MigrationsHistoryTable("__EFMigrationsHistory", "comment")));

        services.AddScoped<ICommentManager, CommentManager>();

        services.AddAutoMapper(typeof(CommentMappingProfile));

        services.AddValidatorsFromAssemblyContaining<CreateCommentValidator>();

        return services;
    }
}
