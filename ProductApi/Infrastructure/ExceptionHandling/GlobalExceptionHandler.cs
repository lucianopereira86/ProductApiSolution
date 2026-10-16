using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict", "The resource was modified by another user."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad request", exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Resource cannot be accessed", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred."),
        };

        if (status >= 500)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning(exception, "Request failed with {statusCode}", status);

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            }, cancellationToken
        );

        return true;
    }
}