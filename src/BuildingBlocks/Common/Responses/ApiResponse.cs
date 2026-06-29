namespace LinkUp.BuildingBlocks.Common.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = [];
    public int StatusCode { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Data = data, Message = message, StatusCode = 200 };

    public static ApiResponse<T> Created(T data, string message = "Created successfully") =>
        new() { Success = true, Data = data, Message = message, StatusCode = 201 };

    public static ApiResponse<T> Fail(string error, int statusCode = 400) =>
        new() { Success = false, Errors = [error], Message = error, StatusCode = statusCode };

    public static ApiResponse<T> Fail(List<string> errors, int statusCode = 400) =>
        new() { Success = false, Errors = errors, Message = "Validation failed", StatusCode = statusCode };

    public static ApiResponse<T> NotFound(string message = "Resource not found") =>
        new() { Success = false, Message = message, Errors = [message], StatusCode = 404 };

    public static ApiResponse<T> Unauthorized(string message = "Unauthorized") =>
        new() { Success = false, Message = message, Errors = [message], StatusCode = 401 };

    public static ApiResponse<T> Forbidden(string message = "Access denied") =>
        new() { Success = false, Message = message, Errors = [message], StatusCode = 403 };

    public static ApiResponse<T> ServerError(string message = "An unexpected error occurred") =>
        new() { Success = false, Message = message, Errors = [message], StatusCode = 500 };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "Success") =>
        new() { Success = true, Message = message, StatusCode = 200 };

    public static new ApiResponse Fail(string error, int statusCode = 400) =>
        new() { Success = false, Errors = [error], Message = error, StatusCode = statusCode };

    public static new ApiResponse NotFound(string message = "Resource not found") =>
        new() { Success = false, Message = message, Errors = [message], StatusCode = 404 };
}
