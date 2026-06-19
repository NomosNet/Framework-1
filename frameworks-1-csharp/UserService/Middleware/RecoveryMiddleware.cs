namespace UserService.Middleware;

public class RecoveryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RecoveryMiddleware> _logger;

    public RecoveryMiddleware(RequestDelegate next, ILogger<RecoveryMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var requestId = RequestContext.GetRequestId(context);

            _logger.LogError(ex, "[{RequestId}] PANIC: {Message}", requestId, ex.Message);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Error = "Internal server error occurred",
                Code = "INTERNAL_ERROR",
                RequestId = requestId
            });
        }
    }
}
