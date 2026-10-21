using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(e => e.ErrorMessage).ToArray()
                );

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Instance = httpContext.Request.Path
            };

            _logger.LogWarning(
                "Request validation failed with {ErrorCount} errors",
                validationException.Errors.Count());

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                cancellationToken);
        }
        else {

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
        }
        return true;
    }
}