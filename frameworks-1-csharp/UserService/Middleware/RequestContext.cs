namespace UserService.Middleware;

public static class RequestContext
{
    public const string RequestIdKey = "request_id";
    public const string StartTimeKey = "start_time";

    public static string GetRequestId(HttpContext context)
    {
        return context.Items[RequestIdKey] as string ?? "unknown";
    }

    public static DateTime GetStartTime(HttpContext context)
    {
        return context.Items[StartTimeKey] as DateTime? ?? DateTime.UtcNow;
    }
}
