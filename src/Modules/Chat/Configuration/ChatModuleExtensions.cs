using FluentValidation;
using LinkUp.Modules.Chat.Interfaces;
using LinkUp.Modules.Chat.Managers;
using LinkUp.Modules.Chat.Mappings;
using LinkUp.Modules.Chat.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUp.Modules.Chat.Configuration;

public static class ChatModuleExtensions
{
    public static IServiceCollection AddChatModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddDbContext<ChatDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "chat")));

        services.AddScoped<IChatManager, ChatManager>();
        services.AddScoped<IGroupChatManager, GroupChatManager>();
        services.AddValidatorsFromAssemblyContaining<SendMessageValidator>();
        services.AddAutoMapper(typeof(ChatMappingProfile));

        return services;
    }
}
