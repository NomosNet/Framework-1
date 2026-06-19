namespace UserService.Middleware;

public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        context.Items[RequestContext.RequestIdKey] = requestId;
        context.Items[RequestContext.StartTimeKey] = startTime;
        context.Response.Headers["X-Request-ID"] = requestId;

        await _next(context);
    }
}
