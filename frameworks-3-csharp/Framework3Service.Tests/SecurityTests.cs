using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Framework3Service;
using Framework3Service.Configuration;

namespace Framework3Service.Tests;

public class SecurityTests
{
    private static AppConfig MakeConfig(Action<AppConfig>? configure = null)
    {
        var config = new AppConfig
        {
            Mode = "learning",
            Port = 0,
            TrustedOrigins = ["http://trusted.local"],
            VerboseErrors = true,
            RateLimit = new RateLimitConfig
            {
                WindowMs = 60000,
                ReadMax = 2,
                CreateMax = 1
            }
        };

        configure?.Invoke(config);
        return config;
    }

    [Fact]
    public async Task BlocksRequestFromUntrustedOrigin()
    {
        var (app, client) = await HttpTestHelper.StartAsync(MakeConfig());
        await using (app)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("Origin", "http://evil.local");

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("origin_forbidden", body.GetProperty("error").GetString());
        }
    }

    [Fact]
    public async Task AllowsRequestFromTrustedOrigin_AndSetsCorsHeader()
    {
        var (app, client) = await HttpTestHelper.StartAsync(MakeConfig());
        await using (app)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("Origin", "http://trusted.local");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
            Assert.Equal("http://trusted.local", values!.First());
        }
    }

    [Fact]
    public async Task AppliesLowerPostLimitThanGetLimit()
    {
        var (app, client) = await HttpTestHelper.StartAsync(MakeConfig());
        await using (app)
        {
            var firstCreate = await client.PostAsync(
                "/items",
                new StringContent("""{"name":"one"}""", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Created, firstCreate.StatusCode);

            var secondCreate = await client.PostAsync(
                "/items",
                new StringContent("""{"name":"two"}""", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.TooManyRequests, secondCreate.StatusCode);

            var firstRead = await client.GetAsync("/items");
            var secondRead = await client.GetAsync("/items");
            var thirdRead = await client.GetAsync("/items");

            Assert.Equal(HttpStatusCode.OK, firstRead.StatusCode);
            Assert.Equal(HttpStatusCode.OK, secondRead.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, thirdRead.StatusCode);
        }
    }

    [Fact]
    public async Task SetsSecurityHeaders()
    {
        var (app, client) = await HttpTestHelper.StartAsync(MakeConfig());
        await using (app)
        {
            var response = await client.GetAsync("/health");

            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());
            Assert.Equal("no-store", response.Headers.GetValues("Cache-Control").First());
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
        }
    }

    [Fact]
    public async Task LearningMode_ReturnsDetailedLimitMessage()
    {
        var (app, client) = await HttpTestHelper.StartAsync(MakeConfig(c => c.Mode = "learning"));
        await using (app)
        {
            await client.PostAsync(
                "/items",
                new StringContent("""{"name":"one"}""", Encoding.UTF8, "application/json"));

            var response = await client.PostAsync(
                "/items",
                new StringContent("""{"name":"two"}""", Encoding.UTF8, "application/json"));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal(JsonValueKind.String, body.GetProperty("details").ValueKind);
            Assert.Contains("Limit", body.GetProperty("details").GetString());
        }
    }

    [Fact]
    public async Task ProductionMode_ReturnsMinimalLimitMessage()
    {
        var (app, client) = await HttpTestHelper.StartAsync(MakeConfig(c => c.Mode = "production"));

        await using (app)
        {
            await client.PostAsync(
                "/items",
                new StringContent("""{"name":"one"}""", Encoding.UTF8, "application/json"));

            var response = await client.PostAsync(
                "/items",
                new StringContent("""{"name":"two"}""", Encoding.UTF8, "application/json"));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal("rate_limit_exceeded", body.GetProperty("error").GetString());
            Assert.False(body.TryGetProperty("details", out _));
        }
    }

    [Fact]
    public async Task LearningMode_WritesRequestLogs()
    {
        var (app, client) = await HttpTestHelper.StartAsync(MakeConfig(c => c.Mode = "learning"));
        await using (app)
        {
            using var writer = new StringWriter();
            var original = Console.Out;
            Console.SetOut(writer);

            try
            {
                await client.GetAsync("/health");
            }
            finally
            {
                Console.SetOut(original);
            }

            Assert.Contains("[learning] GET /health -> 200", writer.ToString());
        }
    }

    [Fact]
    public async Task ProductionMode_DoesNotWriteRequestLogs()
    {
        var (app, client) = await HttpTestHelper.StartAsync(MakeConfig(c => c.Mode = "production"));

        await using (app)
        {
            using var writer = new StringWriter();
            var original = Console.Out;
            Console.SetOut(writer);

            try
            {
                await client.GetAsync("/health");
            }
            finally
            {
                Console.SetOut(original);
            }

            Assert.Equal(string.Empty, writer.ToString());
        }
    }
}
