using Microsoft.AspNetCore.Mvc;
public sealed class ClientHeaderMiddleware(RequestDelegate next)
{
    private const string EXPECTED_HEADER = "X-Client-Id";
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path != "/test/error")
        {
            if (!context.Request.Headers.TryGetValue(
                    EXPECTED_HEADER, out var value)
                || string.IsNullOrWhiteSpace(value.ToString()))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = 400,
                        Title = "Bad request",
                        Detail = "Missing required header.",
                        Instance = context.Request.Path
                    },
                    context.RequestAborted);

                return;
            }
        }

        await _next(context);
    }
}