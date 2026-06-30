using System.Text;
using Asp.Versioning;
using LinkUp.BuildingBlocks.Infrastructure.Middleware;
using LinkUp.Modules.Admin.Configuration;
using LinkUp.Modules.Chat.Configuration;
using LinkUp.Modules.Chat.Hubs;
using LinkUp.Modules.Comment.Configuration;
using LinkUp.Modules.Friend.Configuration;
using LinkUp.Modules.Identity.Configuration;
using LinkUp.Modules.Media.Configuration;
using LinkUp.Modules.Notification.Configuration;
using LinkUp.Modules.Notification.Hubs;
using LinkUp.Modules.Post.Configuration;
using LinkUp.Modules.Reaction.Configuration;
using LinkUp.Modules.Search.Configuration;
using LinkUp.Modules.UserProfile.Configuration;
using LinkUp.Modules.VideoCall.Configuration;
using LinkUp.Modules.VideoCall.Hubs;
using LinkUp.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Local secrets (Cloudinary keys, etc.) — gitignored, optional. Loaded last so it
// overrides the placeholder values committed in appsettings*.json.
configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// ─── Serilog ───────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console()
       .WriteTo.File("logs/linkup-.log", rollingInterval: RollingInterval.Day));

// ─── Controllers + FluentValidation ───────────────────────────────────────
services.AddControllers()
    .AddJsonOptions(o =>
        // Serialize/accept enums as their string names so they match the Angular
        // string-union enum types (PostType, ReactionType, MessageType, etc.).
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
services.AddEndpointsApiExplorer();

// ─── API Versioning ────────────────────────────────────────────────────────
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ─── Swagger + JWT Bearer security ────────────────────────────────────────
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LinkUp API", Version = "v1", Description = "Connect. Share. Chat." });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─── JWT Authentication ────────────────────────────────────────────────────
var jwtSecret = configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };

    // Allow SignalR to receive token from query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var accessToken = ctx.Request.Query["access_token"];
            var path = ctx.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/chat") ||
                 path.StartsWithSegments("/hubs/notification") ||
                 path.StartsWithSegments("/hubs/videocall")))
            {
                ctx.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

services.AddAuthorization(options =>
{
    options.AddPolicy(AppConstants.Policy.AdminOnly,
        policy => policy.RequireRole(AppConstants.Roles.Admin));
    options.AddPolicy(AppConstants.Policy.AuthenticatedUser,
        policy => policy.RequireAuthenticatedUser());
});

// ─── SignalR ───────────────────────────────────────────────────────────────
services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// ─── CORS (allow Angular dev + credentials for SignalR) ────────────────────
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

services.AddCors(options =>
{
    options.AddPolicy("LinkUpCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ─── Module registrations ─────────────────────────────────────────────────
services.AddIdentityModule(configuration);
services.AddUserProfileModule(configuration);
services.AddFriendModule(configuration);
services.AddMediaModule(configuration);
services.AddPostModule(configuration);
services.AddCommentModule(configuration);
services.AddReactionModule(configuration);
services.AddChatModule(configuration);
services.AddNotificationModule(configuration);
services.AddVideoCallModule(configuration);
services.AddSearchModule(configuration);
services.AddAdminModule(configuration);

// ─── Re-assert JWT as the default scheme ───────────────────────────────────
// AddIdentity() (inside AddIdentityModule) resets the default authentication
// scheme to the Identity application cookie. Since it runs after the JWT setup
// above, we must re-assert JWT here so [Authorize] uses Bearer tokens, not cookies.
services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});

// ─── Build ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Apply EF Core migrations + seed on startup ────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Migrations");
    var contexts = new Microsoft.EntityFrameworkCore.DbContext[]
    {
        sp.GetRequiredService<LinkUp.Modules.Identity.Configuration.IdentityDbContext>(),
        sp.GetRequiredService<LinkUp.Modules.UserProfile.Configuration.ProfileDbContext>(),
        sp.GetRequiredService<LinkUp.Modules.Friend.Configuration.FriendDbContext>(),
        sp.GetRequiredService<LinkUp.Modules.Media.Configuration.MediaDbContext>(),
        sp.GetRequiredService<LinkUp.Modules.Post.Configuration.PostDbContext>(),
        sp.GetRequiredService<LinkUp.Modules.Comment.Configuration.CommentDbContext>(),
        sp.GetRequiredService<LinkUp.Modules.Reaction.Configuration.ReactionDbContext>(),
        sp.GetRequiredService<LinkUp.Modules.Chat.Configuration.ChatDbContext>(),
        sp.GetRequiredService<LinkUp.Modules.Notification.Configuration.NotificationDbContext>(),
        sp.GetRequiredService<LinkUp.Modules.VideoCall.Configuration.VideoCallDbContext>(),
    };
    foreach (var ctx in contexts)
    {
        logger.LogInformation("Applying migrations for {Context}", ctx.GetType().Name);
        await ctx.Database.MigrateAsync();
    }

    await LinkUp.Modules.Identity.Configuration.IdentitySeeder.SeedAsync(sp);
}

// ─── Middleware pipeline ───────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LinkUp API v1"));
    // Land on Swagger when hitting the site root (otherwise "/" returns 404).
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseCors("LinkUpCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ─── SignalR Hub endpoints ─────────────────────────────────────────────────
app.MapHub<ChatHub>(AppConstants.SignalR.ChatHub);
app.MapHub<NotificationHub>(AppConstants.SignalR.NotificationHub);
app.MapHub<VideoCallHub>(AppConstants.SignalR.VideoCallHub);

app.Run();
