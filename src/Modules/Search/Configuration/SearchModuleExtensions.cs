using LinkUp.Modules.Search.Interfaces;
using LinkUp.Modules.Search.Managers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.Search.Configuration;

public static class SearchModuleExtensions
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISearchManager, SearchManager>();
        return services;
    }
}
