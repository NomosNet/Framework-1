using System.Diagnostics;
using System.Text.Json;
using Framework4Service.Domain;
using Framework4Service.Middleware;
using Framework4Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace Framework4Service;

public static class AppFactory
{
    public static WebApplication Create(string url = "http://127.0.0.1:8080")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = false;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        });
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        });

        builder.WebHost.UseUrls(url);

        var store = new ProcessStore();
        var metrics = new MetricsService();

        var app = builder.Build();
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.MapPost("/api/process", (HttpContext context, [FromBody] CreateProcessRequest? request) =>
        {
            var correlationId = context.GetCorrelationId();
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Framework4Service.Process");

            if (request?.ProcessKey is not string processKey || processKey.Length == 0)
            {
                return Results.Json(
                    new ErrorResponse { CorrelationId = correlationId, Error = "process_key is required" },
                    AppJson.Options,
                    statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var process = store.Create(processKey);
                logger.LogInformation(
                    "process created correlation_id={CorrelationId} process_key={ProcessKey} state={State}",
                    correlationId,
                    process.Key,
                    ProcessState.New);

                return Results.Json(
                    new CreateProcessResponse
                    {
                        CorrelationId = correlationId,
                        ProcessKey = process.Key,
                        State = ProcessState.New
                    },
                    AppJson.Options,
                    statusCode: StatusCodes.Status201Created);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains(ProcessErrors.AlreadyExists))
            {
                return Results.Json(
                    new ErrorResponse { CorrelationId = correlationId, Error = ex.Message },
                    AppJson.Options,
                    statusCode: StatusCodes.Status409Conflict);
            }
        });

        app.MapPost("/api/process/{key}/event", async (HttpContext context, string key) =>
        {
            var correlationId = context.GetCorrelationId();
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Framework4Service.Process");

            EventRequest? request;
            try
            {
                request = await JsonSerializer.DeserializeAsync<EventRequest>(
                    context.Request.Body,
                    AppJson.Options);
            }
            catch (JsonException)
            {
                return Results.Json(
                    new ErrorResponse { CorrelationId = correlationId, Error = "invalid request body" },
                    AppJson.Options,
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (request?.IdempotencyKey is not string idempotencyKey || idempotencyKey.Length == 0
                || request.Event is not string eventName || eventName.Length == 0)
            {
                return Results.Json(
                    new ErrorResponse
                    {
                        CorrelationId = correlationId,
                        Error = "idempotency_key and event are required"
                    },
                    AppJson.Options,
                    statusCode: StatusCodes.Status400BadRequest);
            }

            Domain.Process process;
            try
            {
                process = store.Get(key);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.Json(
                    new ErrorResponse { CorrelationId = correlationId, Error = ex.Message },
                    AppJson.Options,
                    statusCode: StatusCodes.Status404NotFound);
            }

            var startedAt = Stopwatch.GetTimestamp();
            TransitionResult result;
            try
            {
                result = process.Apply(idempotencyKey, eventName, request.SimulateFailure);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains(ProcessErrors.InvalidTransition))
            {
                metrics.IncrementFailedTransitions();
                logger.LogError(
                    "transition rejected correlation_id={CorrelationId} process_key={ProcessKey} event={Event} error={Error}",
                    correlationId,
                    key,
                    eventName,
                    ex.Message);

                return Results.Json(
                    new ErrorResponse { CorrelationId = correlationId, Error = ex.Message },
                    AppJson.Options,
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            var elapsed = Stopwatch.GetElapsedTime(startedAt);

            if (result.IdempotentReplay)
            {
                metrics.IncrementRepeatedDeliveries();
                logger.LogInformation(
                    "idempotent replay, event ignored correlation_id={CorrelationId} process_key={ProcessKey} event={Event} idempotency_key={IdempotencyKey} previous_state={PreviousState} current_state={CurrentState}",
                    correlationId,
                    key,
                    eventName,
                    idempotencyKey,
                    result.PreviousState,
                    result.NewState);
            }
            else if (result.Compensated)
            {
                metrics.IncrementFailedTransitions();
                metrics.IncrementCompensations();
                metrics.RecordLatency(eventName, elapsed);
                logger.LogWarning(
                    "step failed, compensation executed correlation_id={CorrelationId} process_key={ProcessKey} event={Event} idempotency_key={IdempotencyKey} previous_state={PreviousState} new_state={NewState} elapsed={ElapsedMs}ms",
                    correlationId,
                    key,
                    eventName,
                    idempotencyKey,
                    result.PreviousState,
                    result.NewState,
                    elapsed.TotalMilliseconds);
            }
            else
            {
                if (result.NewState == ProcessState.Error)
                {
                    metrics.IncrementFailedTransitions();
                }
                else
                {
                    metrics.IncrementSuccessfulTransitions();
                }

                metrics.RecordLatency(eventName, elapsed);
                logger.LogInformation(
                    "state transition correlation_id={CorrelationId} process_key={ProcessKey} event={Event} idempotency_key={IdempotencyKey} previous_state={PreviousState} new_state={NewState} elapsed={ElapsedMs}ms",
                    correlationId,
                    key,
                    eventName,
                    idempotencyKey,
                    result.PreviousState,
                    result.NewState,
                    elapsed.TotalMilliseconds);
            }

            return Results.Json(
                new EventResponse
                {
                    CorrelationId = correlationId,
                    ProcessKey = key,
                    PreviousState = result.PreviousState,
                    CurrentState = result.NewState,
                    Event = eventName,
                    IdempotentReplay = result.IdempotentReplay,
                    Compensated = result.Compensated
                },
                AppJson.Options);
        });

        app.MapGet("/api/process/{key}", (HttpContext context, string key) =>
        {
            var correlationId = context.GetCorrelationId();

            try
            {
                var process = store.Get(key);
                return Results.Json(
                    new ProcessStateResponse
                    {
                        CorrelationId = correlationId,
                        ProcessKey = key,
                        State = process.CurrentState()
                    },
                    AppJson.Options);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.Json(
                    new ErrorResponse { CorrelationId = correlationId, Error = ex.Message },
                    AppJson.Options,
                    statusCode: StatusCodes.Status404NotFound);
            }
        });

        app.MapGet("/health/live", () =>
            Results.Json(new { status = "ok" }, AppJson.Options));

        app.MapGet("/health/ready", () =>
        {
            if (!metrics.IsHealthy())
            {
                return Results.Json(
                    new ReadinessResponse
                    {
                        Status = "degraded",
                        Reason = "error rate exceeds threshold"
                    },
                    AppJson.Options,
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Json(
                new ReadinessResponse
                {
                    Status = "ok",
                    Processes = store.Count()
                },
                AppJson.Options);
        });

        app.MapGet("/metrics", () => Results.Json(metrics.Snapshot(), AppJson.Options));

        return app;
    }

    private sealed class CreateProcessRequest
    {
        public string? ProcessKey { get; set; }
    }

    private sealed class CreateProcessResponse
    {
        public required string CorrelationId { get; init; }
        public required string ProcessKey { get; init; }
        public required string State { get; init; }
    }

    private sealed class EventRequest
    {
        public string? IdempotencyKey { get; set; }
        public string? Event { get; set; }
        public bool SimulateFailure { get; set; }
    }

    private sealed class EventResponse
    {
        public required string CorrelationId { get; init; }
        public required string ProcessKey { get; init; }
        public required string PreviousState { get; init; }
        public required string CurrentState { get; init; }
        public required string Event { get; init; }
        public bool IdempotentReplay { get; init; }
        public bool Compensated { get; init; }
    }

    private sealed class ProcessStateResponse
    {
        public required string CorrelationId { get; init; }
        public required string ProcessKey { get; init; }
        public required string State { get; init; }
    }

    private sealed class ErrorResponse
    {
        public required string CorrelationId { get; init; }
        public required string Error { get; init; }
    }

    private sealed class ReadinessResponse
    {
        public required string Status { get; init; }
        public int Processes { get; init; }
        public string? Reason { get; init; }
    }
}
