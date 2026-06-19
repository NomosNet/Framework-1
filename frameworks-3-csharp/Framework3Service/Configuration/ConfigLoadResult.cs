namespace Framework3Service.Configuration;

public sealed class ConfigLoadResult
{
    public required AppConfig Config { get; init; }
    public required ConfigSources Sources { get; init; }
}

public sealed class ConfigSources
{
    public required FileConfigSnapshot File { get; init; }
    public required EnvConfigSnapshot Env { get; init; }
    public required CliConfigSnapshot Cli { get; init; }
}

public sealed class FileConfigSnapshot
{
    public string? Mode { get; set; }
    public int Port { get; set; }
    public List<string>? TrustedOrigins { get; set; }
}

public sealed class EnvConfigSnapshot
{
    public string? Mode { get; set; }
    public int? Port { get; set; }
    public List<string>? TrustedOrigins { get; set; }
    public RateLimitConfig RateLimit { get; set; } = new();
}

public sealed class CliConfigSnapshot
{
    public int? Port { get; set; }
    public List<string>? TrustedOrigins { get; set; }
    public RateLimitConfig RateLimit { get; set; } = new();
}
