namespace UserService.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = RequestContext.GetRequestId(context);
        var startTime = RequestContext.GetStartTime(context);

        _logger.LogInformation(
            "[{RequestId}] Started {Method} {Path} from {RemoteAddr}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        await _next(context);

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "[{RequestId}] Completed {Method} {Path} - Status: {StatusCode}, Duration: {Duration}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            duration);
    }
}
