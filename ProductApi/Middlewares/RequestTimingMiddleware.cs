using System.Diagnostics;

public sealed class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestTimingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopWatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopWatch.Stop();

            _logger.LogInformation(
                "{Method} {Path} returned {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopWatch.ElapsedMilliseconds);
        }
    }
}