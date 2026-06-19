using System.Text.Json;
using Framework3Service.Configuration;
using Framework3Service.Middleware;

namespace Framework3Service;

public static class AppFactory
{
    public static WebApplication Create(AppConfig config)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(config.Port > 0
            ? $"http://127.0.0.1:{config.Port}"
            : "http://127.0.0.1:0");

        var app = builder.Build();
        var items = new List<ItemDto>();

        app.UseMiddleware<RequestLoggingMiddleware>(config.Mode == "learning");
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<CorsMiddleware>(config);
        app.UseMiddleware<RateLimitMiddleware>(config);

        app.MapGet("/health", () => Results.Json(new { ok = true, mode = config.Mode }));

        app.MapGet("/items", () => Results.Json(new { items }));

        app.MapPost("/items", async (HttpRequest request) =>
        {
            var body = await JsonSerializer.DeserializeAsync<CreateItemRequest>(
                request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (body?.Name is not string name || name.Trim().Length == 0)
            {
                return Results.Json(new { error = "name_required" }, statusCode: StatusCodes.Status400BadRequest);
            }

            var item = new ItemDto
            {
                Id = items.Count + 1,
                Name = name.Trim()
            };
            items.Add(item);

            return Results.Json(item, statusCode: StatusCodes.Status201Created);
        });

        return app;
    }

    private sealed class CreateItemRequest
    {
        public string? Name { get; set; }
    }

    private sealed class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
