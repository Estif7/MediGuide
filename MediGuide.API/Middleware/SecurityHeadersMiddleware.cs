namespace MediGuide.API.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
            headers["Content-Security-Policy"] =
                "default-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
