namespace UserService.Middleware;

public class TimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TimingMiddleware> _logger;

    public TimingMiddleware(RequestDelegate next, ILogger<TimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = RequestContext.GetRequestId(context);
        var startTime = RequestContext.GetStartTime(context);
        var headerWritten = false;

        context.Response.OnStarting(() =>
        {
            if (!headerWritten)
            {
                WriteTiming(context, requestId, startTime);
                headerWritten = true;
            }

            return Task.CompletedTask;
        });

        await _next(context);

        if (!headerWritten)
        {
            WriteTiming(context, requestId, startTime);
        }
    }

    private void WriteTiming(HttpContext context, string requestId, DateTime startTime)
    {
        var duration = DateTime.UtcNow - startTime;
        context.Response.Headers["X-Response-Time"] = duration.ToString();

        _logger.LogInformation("[{RequestId}] Request timing: {Duration}", requestId, duration);
    }
}
