using System.Text.Json;
using LinkUp.BuildingBlocks.Common.Exceptions;
using LinkUp.BuildingBlocks.Common.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LinkUp.BuildingBlocks.Infrastructure.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ApiResponse<object> response = exception switch
        {
            ValidationException ve => new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed",
                Errors = ve.ValidationErrors.ToList(),
                StatusCode = 400
            },
            NotFoundException nfe => ApiResponse<object>.NotFound(nfe.Message),
            UnauthorizedException ue => ApiResponse<object>.Unauthorized(ue.Message),
            ForbiddenException fe => ApiResponse<object>.Forbidden(fe.Message),
            ConflictException ce => ApiResponse<object>.Fail(ce.Message, 409),
            AppException ae => ApiResponse<object>.Fail(ae.Message, ae.StatusCode),
            _ => ApiResponse<object>.ServerError()
        };

        context.Response.StatusCode = response.StatusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }
}
