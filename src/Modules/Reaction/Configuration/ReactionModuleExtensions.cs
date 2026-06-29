using FluentValidation;
using LinkUp.Modules.Reaction.Interfaces;
using LinkUp.Modules.Reaction.Managers;
using LinkUp.Modules.Reaction.Mappings;
using LinkUp.Modules.Reaction.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.Reaction.Configuration;

public static class ReactionModuleExtensions
{
    public static IServiceCollection AddReactionModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddDbContext<ReactionDbContext>(options =>
            options.UseNpgsql(connectionString, o =>
                o.MigrationsHistoryTable("__EFMigrationsHistory", "reaction")));

        services.AddScoped<IReactionManager, ReactionManager>();
        services.AddAutoMapper(typeof(ReactionMappingProfile));
        services.AddValidatorsFromAssemblyContaining<AddReactionValidator>();

        return services;
    }
}
