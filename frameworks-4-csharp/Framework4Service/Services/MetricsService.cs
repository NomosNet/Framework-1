namespace Framework4Service.Services;

public sealed class MetricsService
{
    private long _successfulTransitions;
    private long _failedTransitions;
    private long _repeatedDeliveries;
    private long _compensations;

    private readonly object _latencyLock = new();
    private readonly Dictionary<string, List<TimeSpan>> _stepLatencies = new();

    public void RecordLatency(string step, TimeSpan duration)
    {
        lock (_latencyLock)
        {
            if (!_stepLatencies.TryGetValue(step, out var durations))
            {
                durations = [];
                _stepLatencies[step] = durations;
            }

            durations.Add(duration);
        }
    }

    public void IncrementSuccessfulTransitions() =>
        Interlocked.Increment(ref _successfulTransitions);

    public void IncrementFailedTransitions() =>
        Interlocked.Increment(ref _failedTransitions);

    public void IncrementRepeatedDeliveries() =>
        Interlocked.Increment(ref _repeatedDeliveries);

    public void IncrementCompensations() =>
        Interlocked.Increment(ref _compensations);

    public bool IsHealthy()
    {
        var successful = Interlocked.Read(ref _successfulTransitions);
        var failed = Interlocked.Read(ref _failedTransitions);
        var total = successful + failed;

        if (total < 5)
        {
            return true;
        }

        return failed * 2 < total;
    }

    public MetricsSnapshot Snapshot()
    {
        lock (_latencyLock)
        {
            var latencies = new Dictionary<string, double>(_stepLatencies.Count);
            foreach (var (step, durations) in _stepLatencies)
            {
                if (durations.Count == 0)
                {
                    continue;
                }

                var totalMs = durations.Sum(d => d.TotalMilliseconds);
                latencies[step] = totalMs / durations.Count;
            }

            return new MetricsSnapshot
            {
                SuccessfulTransitions = Interlocked.Read(ref _successfulTransitions),
                FailedTransitions = Interlocked.Read(ref _failedTransitions),
                RepeatedDeliveries = Interlocked.Read(ref _repeatedDeliveries),
                Compensations = Interlocked.Read(ref _compensations),
                StepLatenciesAvgMs = latencies
            };
        }
    }
}

public sealed class MetricsSnapshot
{
    public long SuccessfulTransitions { get; init; }
    public long FailedTransitions { get; init; }
    public long RepeatedDeliveries { get; init; }
    public long Compensations { get; init; }
    public Dictionary<string, double> StepLatenciesAvgMs { get; init; } = new();
}
