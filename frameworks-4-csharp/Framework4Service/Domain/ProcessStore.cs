namespace Framework4Service.Domain;

public sealed class ProcessStore
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Process> _processes = new();

    public Process Create(string key)
    {
        lock (_lock)
        {
            if (_processes.ContainsKey(key))
            {
                throw new InvalidOperationException($"{ProcessErrors.AlreadyExists}: {key}");
            }

            var process = new Process(key);
            _processes[key] = process;
            return process;
        }
    }

    public Process Get(string key)
    {
        lock (_lock)
        {
            if (!_processes.TryGetValue(key, out var process))
            {
                throw new KeyNotFoundException($"{ProcessErrors.ProcessNotFound}: {key}");
            }

            return process;
        }
    }

    public int Count()
    {
        lock (_lock)
        {
            return _processes.Count;
        }
    }
}
