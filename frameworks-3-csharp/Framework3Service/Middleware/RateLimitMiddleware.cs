using System.Collections.Concurrent;
using System.Text.Json;
using Framework3Service.Configuration;

namespace Framework3Service.Middleware;

public sealed class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitConfig _config;
    private readonly bool _verboseErrors;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _store = new();

    public RateLimitMiddleware(RequestDelegate next, AppConfig config)
    {
        _next = next;
        _config = config.RateLimit;
        _verboseErrors = config.Mode == "learning";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var bucketKey = $"{ip}:{context.Request.Method}:{context.Request.Path}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var limit = GetRouteLimit(context);

        var bucket = _store.AddOrUpdate(
            bucketKey,
            _ => new RateLimitBucket(now, 1),
            (_, existing) =>
            {
                if (now - existing.StartedAt >= _config.WindowMs)
                {
                    return new RateLimitBucket(now, 1);
                }

                return existing with { Count = existing.Count + 1 };
            });

        if (bucket.Count > limit)
        {
            await WriteJsonAsync(context, StatusCodes.Status429TooManyRequests, BuildPayload(limit));
            return;
        }

        await _next(context);
    }

    private int GetRouteLimit(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals("/items", StringComparison.OrdinalIgnoreCase))
        {
            return _config.CreateMax;
        }

        return _config.ReadMax;
    }

    private object BuildPayload(int limit)
    {
        if (_verboseErrors)
        {
            return new
            {
                error = "rate_limit_exceeded",
                details = $"Limit {limit} requests per {_config.WindowMs}ms"
            };
        }

        return new { error = "rate_limit_exceeded" };
    }

    private static async Task WriteJsonAsync(HttpContext context, int statusCode, object payload)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    private sealed record RateLimitBucket(long StartedAt, int Count);
}
