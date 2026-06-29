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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// ─── Serilog ───────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console()
       .WriteTo.File("logs/linkup-.log", rollingInterval: RollingInterval.Day));

// ─── Controllers + FluentValidation ───────────────────────────────────────
services.AddControllers();
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

// ─── Build ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Middleware pipeline ───────────────────────────────────────────────────
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LinkUp API v1"));
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
