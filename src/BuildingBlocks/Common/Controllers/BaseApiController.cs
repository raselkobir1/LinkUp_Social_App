using System.Security.Claims;
using LinkUp.BuildingBlocks.Common.Responses;
using Microsoft.AspNetCore.Mvc;

namespace LinkUp.BuildingBlocks.Common.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User not authenticated."));

    protected string? CurrentUserEmail =>
        User.FindFirstValue(ClaimTypes.Email);

    protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    protected IActionResult ApiOk<T>(T data, string message = "Success") =>
        Ok(ApiResponse<T>.Ok(data, message));

    protected IActionResult ApiCreated<T>(T data, string message = "Created successfully") =>
        StatusCode(201, ApiResponse<T>.Created(data, message));

    protected IActionResult ApiFail(string error, int statusCode = 400) =>
        StatusCode(statusCode, ApiResponse<object>.Fail(error, statusCode));

    protected IActionResult ApiNotFound(string message = "Resource not found") =>
        NotFound(ApiResponse<object>.NotFound(message));

    protected IActionResult ApiForbidden(string message = "Access denied") =>
        StatusCode(403, ApiResponse<object>.Forbidden(message));

    protected IActionResult ApiOkPaged<T>(T data, string message = "Success") =>
        Ok(ApiResponse<T>.Ok(data, message));
}
