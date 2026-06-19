namespace Framework4Service.Middleware;

public static class CorrelationContext
{
    public const string ItemKey = "CorrelationId";
}

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Items[CorrelationContext.ItemKey] = correlationId;
        await _next(context);
    }
}

public static class CorrelationIdExtensions
{
    public static string GetCorrelationId(this HttpContext context)
    {
        return context.Items[CorrelationContext.ItemKey] as string ?? Guid.NewGuid().ToString();
    }
}
