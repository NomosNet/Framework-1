using System.Collections.Concurrent;
using System.Text.Json;
using Framework3Service.Configuration;

namespace Framework3Service.Middleware;

public sealed class CorsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _trustedOrigins;
    private readonly bool _verboseErrors;

    public CorsMiddleware(RequestDelegate next, AppConfig config)
    {
        _next = next;
        _trustedOrigins = config.TrustedOrigins.ToHashSet(StringComparer.Ordinal);
        _verboseErrors = config.Mode == "learning";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Origin", out var originValues))
        {
            await _next(context);
            return;
        }

        var origin = originValues.ToString();
        if (!_trustedOrigins.Contains(origin))
        {
            await WriteJsonAsync(context, StatusCodes.Status403Forbidden, BuildPayload(
                "origin_forbidden",
                _verboseErrors ? $"Origin {origin} is not trusted" : null));
            return;
        }

        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
        context.Response.Headers["Vary"] = "Origin";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await _next(context);
    }

    private static object BuildPayload(string error, string? details)
    {
        return details is null
            ? new { error }
            : new { error, details };
    }

    private static async Task WriteJsonAsync(HttpContext context, int statusCode, object payload)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
