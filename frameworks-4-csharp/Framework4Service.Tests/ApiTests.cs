using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Framework4Service.Domain;

namespace Framework4Service.Tests;

public class ApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [Fact]
    public async Task HappyPath_CompletesBooking()
    {
        var (app, client) = await HttpTestHelper.StartAsync();
        await using (app)
        {
            await CreateProcess(client, "room-101");

            await SendEvent(client, "room-101", "evt-1", ProcessEvent.AcceptApplication);
            await SendEvent(client, "room-101", "evt-2", ProcessEvent.Reserve);
            await SendEvent(client, "room-101", "evt-3", ProcessEvent.GrantAccess);
            await SendEvent(client, "room-101", "evt-4", ProcessEvent.Complete);

            var state = await GetState(client, "room-101");
            Assert.Equal(ProcessState.Completed, state);
        }
    }

    [Fact]
    public async Task Compensation_OnGrantAccessFailure()
    {
        var (app, client) = await HttpTestHelper.StartAsync();
        await using (app)
        {
            await CreateProcess(client, "room-202");
            await SendEvent(client, "room-202", "e1", ProcessEvent.AcceptApplication);
            await SendEvent(client, "room-202", "e2", ProcessEvent.Reserve);

            var response = await SendEvent(
                client,
                "room-202",
                "e3",
                ProcessEvent.GrantAccess,
                simulateFailure: true);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(body.GetProperty("compensated").GetBoolean());
            Assert.Equal(ProcessState.CompensationDone, body.GetProperty("current_state").GetString());
        }
    }

    [Fact]
    public async Task Idempotency_IgnoresRepeatedDelivery()
    {
        var (app, client) = await HttpTestHelper.StartAsync();
        await using (app)
        {
            await CreateProcess(client, "room-idem");
            await SendEvent(client, "room-idem", "evt-1", ProcessEvent.AcceptApplication);

            var response = await SendEvent(client, "room-idem", "evt-1", ProcessEvent.AcceptApplication);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

            Assert.True(body.GetProperty("idempotent_replay").GetBoolean());
            Assert.Equal(ProcessState.ApplicationAccepted, body.GetProperty("current_state").GetString());
        }
    }

    [Fact]
    public async Task InvalidTransition_Returns422()
    {
        var (app, client) = await HttpTestHelper.StartAsync();
        await using (app)
        {
            await CreateProcess(client, "room-bad");
            var response = await SendEvent(client, "room-bad", "evt-1", ProcessEvent.Reserve);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }
    }

    [Fact]
    public async Task DuplicateProcess_Returns409()
    {
        var (app, client) = await HttpTestHelper.StartAsync();
        await using (app)
        {
            await CreateProcess(client, "room-dup");
            var response = await CreateProcessRaw(client, "room-dup");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }

    [Fact]
    public async Task Readiness_Returns503_WhenErrorRateHigh()
    {
        var (app, client) = await HttpTestHelper.StartAsync();
        await using (app)
        {
            for (var i = 0; i < 5; i++)
            {
                var key = $"room-degrade-{i}";
                await CreateProcess(client, key);
                await SendEvent(client, key, "fail", ProcessEvent.Reserve, simulateFailure: true);
            }

            var response = await client.GetAsync("/health/ready");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("degraded", body.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task Liveness_AlwaysReturnsOk()
    {
        var (app, client) = await HttpTestHelper.StartAsync();
        await using (app)
        {
            var response = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private static async Task<HttpResponseMessage> CreateProcessRaw(HttpClient client, string processKey)
    {
        return await client.PostAsync(
            "/api/process",
            new StringContent(
                $$"""{"process_key":"{{processKey}}"}""",
                Encoding.UTF8,
                "application/json"));
    }

    private static async Task CreateProcess(HttpClient client, string processKey)
    {
        var response = await CreateProcessRaw(client, processKey);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendEvent(
        HttpClient client,
        string processKey,
        string idempotencyKey,
        string eventName,
        bool simulateFailure = false)
    {
        var payload = simulateFailure
            ? $$"""{"idempotency_key":"{{idempotencyKey}}","event":"{{eventName}}","simulate_failure":true}"""
            : $$"""{"idempotency_key":"{{idempotencyKey}}","event":"{{eventName}}"}""";

        return await client.PostAsync(
            $"/api/process/{processKey}/event",
            new StringContent(payload, Encoding.UTF8, "application/json"));
    }

    private static async Task<string> GetState(HttpClient client, string processKey)
    {
        var response = await client.GetAsync($"/api/process/{processKey}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return body.GetProperty("state").GetString()!;
    }
}
