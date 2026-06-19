using System.Diagnostics;

namespace Framework3Service.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _enabled;

    public RequestLoggingMiddleware(RequestDelegate next, bool enabled)
    {
        _next = next;
        _enabled = enabled;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled)
        {
            await _next(context);
            return;
        }

        var startedAt = Stopwatch.GetTimestamp();
        await _next(context);
        var durationMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        var path = context.Request.Path + context.Request.QueryString;
        Console.WriteLine(
            $"[learning] {context.Request.Method} {path} -> {context.Response.StatusCode} {durationMs:F1}ms");
    }
}
