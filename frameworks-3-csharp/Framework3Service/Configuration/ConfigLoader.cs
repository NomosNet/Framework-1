using System.Collections;
using System.Text.Json;
using Framework3Service.Utils;

namespace Framework3Service.Configuration;

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ConfigLoadResult Load(ConfigLoadOptions? options = null)
    {
        var cwd = options?.WorkingDirectory ?? Directory.GetCurrentDirectory();
        var argv = options?.Args ?? Environment.GetCommandLineArgs().Skip(1).ToArray();
        var env = options?.Environment ?? BuildEnvironmentSnapshot();

        var configPath = Path.Combine(cwd, "config", "default.json");
        var fromFile = ReadJsonFile(configPath);
        var cli = CliArgsParser.Parse(argv);

        var fromEnv = new EnvConfigSnapshot
        {
            Mode = GetEnvValue(env, "APP_MODE"),
            Port = ParseNumber(GetEnvValue(env, "PORT")),
            TrustedOrigins = ParseOrigins(GetEnvValue(env, "TRUSTED_ORIGINS")),
            RateLimit = new RateLimitConfig
            {
                WindowMs = ParseNumber(GetEnvValue(env, "RATE_LIMIT_WINDOW_MS")) ?? 0,
                ReadMax = ParseNumber(GetEnvValue(env, "RATE_LIMIT_READ_MAX")) ?? 0,
                CreateMax = ParseNumber(GetEnvValue(env, "RATE_LIMIT_CREATE_MAX")) ?? 0
            }
        };

        var fromCli = new CliConfigSnapshot
        {
            Port = ParseNumber(cli.GetValueOrDefault("port")),
            TrustedOrigins = ParseOrigins(cli.GetValueOrDefault("trustedOrigins")),
            RateLimit = new RateLimitConfig
            {
                WindowMs = ParseNumber(cli.GetValueOrDefault("rateLimitWindowMs")) ?? 0,
                ReadMax = ParseNumber(cli.GetValueOrDefault("rateLimitReadMax")) ?? 0,
                CreateMax = ParseNumber(cli.GetValueOrDefault("rateLimitCreateMax")) ?? 0
            }
        };

        var selectedMode = PickMode(fromFile.Mode, fromEnv.Mode);
        var modeProfiles = fromFile.ModeProfiles ?? new Dictionary<string, ModeProfile>(StringComparer.OrdinalIgnoreCase);
        modeProfiles.TryGetValue(selectedMode, out var selectedProfile);
        selectedProfile ??= new ModeProfile();

        var profileRate = EnsureRateLimit(selectedProfile.RateLimit);
        var fileRate = EnsureRateLimit(fromFile.RateLimit);

        var merged = new AppConfig
        {
            Mode = selectedMode,
            Port = fromFile.Port,
            TrustedOrigins = fromFile.TrustedOrigins ?? [],
            VerboseErrors = selectedProfile.VerboseErrors ?? selectedMode == "learning",
            RateLimit = new RateLimitConfig
            {
                WindowMs = fileRate.WindowMs ?? profileRate.WindowMs ?? 0,
                ReadMax = fileRate.ReadMax ?? profileRate.ReadMax ?? 0,
                CreateMax = fileRate.CreateMax ?? profileRate.CreateMax ?? 0
            }
        };

        if (fromEnv.Port.HasValue)
        {
            merged.Port = fromEnv.Port.Value;
        }

        if (fromEnv.TrustedOrigins is not null)
        {
            merged.TrustedOrigins = fromEnv.TrustedOrigins;
        }

        ApplyRateLimitOverride(merged.RateLimit, fromEnv.RateLimit);

        if (fromCli.Port.HasValue)
        {
            merged.Port = fromCli.Port.Value;
        }

        if (fromCli.TrustedOrigins is not null)
        {
            merged.TrustedOrigins = fromCli.TrustedOrigins;
        }

        ApplyRateLimitOverride(merged.RateLimit, fromCli.RateLimit);

        return new ConfigLoadResult
        {
            Config = merged,
            Sources = new ConfigSources
            {
                File = new FileConfigSnapshot
                {
                    Mode = fromFile.Mode,
                    Port = fromFile.Port,
                    TrustedOrigins = fromFile.TrustedOrigins
                },
                Env = fromEnv,
                Cli = fromCli
            }
        };
    }

    private static FileConfigRoot ReadJsonFile(string filePath)
    {
        var raw = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<FileConfigRoot>(raw, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to parse config file '{filePath}'.");
    }

    private static Dictionary<string, string?> BuildEnvironmentSnapshot()
    {
        var snapshot = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            snapshot[entry.Key.ToString()!] = entry.Value?.ToString();
        }

        return snapshot;
    }

    private static string? GetEnvValue(IReadOnlyDictionary<string, string?> env, string key)
    {
        return env.TryGetValue(key, out var value) ? value : null;
    }

    private static int? ParseNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value, out var num) ? num : null;
    }

    private static List<string>? ParseOrigins(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(origin => origin.Length > 0)
            .ToList();
    }

    private static PartialRateLimit EnsureRateLimit(RateLimitConfig? input)
    {
        if (input is null)
        {
            return new PartialRateLimit();
        }

        return new PartialRateLimit
        {
            WindowMs = input.WindowMs > 0 ? input.WindowMs : null,
            ReadMax = input.ReadMax > 0 ? input.ReadMax : null,
            CreateMax = input.CreateMax > 0 ? input.CreateMax : null
        };
    }

    private static string PickMode(string? baseMode, string? envMode)
    {
        return envMode ?? baseMode ?? "learning";
    }

    private static void ApplyRateLimitOverride(RateLimitConfig target, RateLimitConfig source)
    {
        if (source.WindowMs > 0)
        {
            target.WindowMs = source.WindowMs;
        }

        if (source.ReadMax > 0)
        {
            target.ReadMax = source.ReadMax;
        }

        if (source.CreateMax > 0)
        {
            target.CreateMax = source.CreateMax;
        }
    }

    private sealed class FileConfigRoot
    {
        public string? Mode { get; set; }
        public int Port { get; set; }
        public List<string>? TrustedOrigins { get; set; }
        public Dictionary<string, ModeProfile>? ModeProfiles { get; set; }
        public RateLimitConfig? RateLimit { get; set; }
    }

    private sealed class ModeProfile
    {
        public bool? VerboseErrors { get; set; }
        public RateLimitConfig? RateLimit { get; set; }
    }

    private sealed class PartialRateLimit
    {
        public int? WindowMs { get; set; }
        public int? ReadMax { get; set; }
        public int? CreateMax { get; set; }
    }
}
