using System.Diagnostics;

namespace MediGuide.API.Middleware;

public sealed class RequestCorrelationMiddleware
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCorrelationMiddleware> _logger;

    public RequestCorrelationMiddleware(
        RequestDelegate next,
        ILogger<RequestCorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationHeader] = correlationId;

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Request started {Method} {Path} {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Request completed {Method} {Path} {StatusCode} {ElapsedMs}ms {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        var supplied = context.Request.Headers[CorrelationHeader].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(supplied) && supplied.Length <= 64
            ? supplied
            : Guid.NewGuid().ToString("N");
    }
}
