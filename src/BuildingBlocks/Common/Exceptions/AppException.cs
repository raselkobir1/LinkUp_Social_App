namespace LinkUp.BuildingBlocks.Common.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string resource, object id)
        : base($"{resource} with id '{id}' was not found.", 404) { }

    public NotFoundException(string message)
        : base(message, 404) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized access.")
        : base(message, 401) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message, 403) { }
}

public class ValidationException : AppException
{
    public IEnumerable<string> ValidationErrors { get; }

    public ValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.", 400)
    {
        ValidationErrors = errors;
    }

    public ValidationException(string error)
        : base(error, 400)
    {
        ValidationErrors = [error];
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409) { }
}
