namespace Framework4Service.Domain;

public sealed class TransitionResult
{
    public required string PreviousState { get; init; }
    public required string NewState { get; init; }
    public bool IdempotentReplay { get; init; }
    public bool Compensated { get; init; }
}

internal sealed class IdempotentRecord
{
    public required string PreviousState { get; init; }
    public required string ResultState { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

public sealed class Process
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Transitions =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            [ProcessState.New] = new Dictionary<string, string>
            {
                [ProcessEvent.AcceptApplication] = ProcessState.ApplicationAccepted
            },
            [ProcessState.ApplicationAccepted] = new Dictionary<string, string>
            {
                [ProcessEvent.Reserve] = ProcessState.ResourceReserved
            },
            [ProcessState.ResourceReserved] = new Dictionary<string, string>
            {
                [ProcessEvent.GrantAccess] = ProcessState.AccessGranted
            },
            [ProcessState.AccessGranted] = new Dictionary<string, string>
            {
                [ProcessEvent.Complete] = ProcessState.Completed
            }
        };

    private readonly object _lock = new();
    private readonly Dictionary<string, IdempotentRecord> _processedEvents = new();

    public string Key { get; }
    public string State { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Process(string key)
    {
        Key = key;
        State = ProcessState.New;
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public string CurrentState()
    {
        lock (_lock)
        {
            return State;
        }
    }

    public TransitionResult Apply(string idempotencyKey, string eventName, bool simulateFailure)
    {
        lock (_lock)
        {
            if (_processedEvents.TryGetValue(idempotencyKey, out var record))
            {
                return new TransitionResult
                {
                    PreviousState = record.PreviousState,
                    NewState = record.ResultState,
                    IdempotentReplay = true
                };
            }

            var previousState = State;

            if (!Transitions.TryGetValue(State, out var stateTransitions))
            {
                throw new InvalidOperationException(
                    $"{ProcessErrors.InvalidTransition}: процесс находится в терминальном состоянии {State}");
            }

            if (!stateTransitions.TryGetValue(eventName, out var nextState))
            {
                throw new InvalidOperationException(
                    $"{ProcessErrors.InvalidTransition}: событие {eventName} недопустимо в состоянии {State}");
            }

            var compensated = false;
            var now = DateTimeOffset.UtcNow;

            if (simulateFailure)
            {
                if (State == ProcessState.ResourceReserved && eventName == ProcessEvent.GrantAccess)
                {
                    State = ProcessState.CompensationDone;
                    compensated = true;
                }
                else
                {
                    State = ProcessState.Error;
                }
            }
            else
            {
                State = nextState;
            }

            UpdatedAt = now;
            _processedEvents[idempotencyKey] = new IdempotentRecord
            {
                PreviousState = previousState,
                ResultState = State,
                Timestamp = now
            };

            return new TransitionResult
            {
                PreviousState = previousState,
                NewState = State,
                Compensated = compensated
            };
        }
    }
}
