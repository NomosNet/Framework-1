using System.Text;
using System.Text.Json;
using Framework3Service;
using Framework3Service.Configuration;

namespace Framework3Service.Tests;

public class ConfigTests
{
    private static string ServiceRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Framework3Service"));

    [Fact]
    public void ConfigSourcePriority_IsFileLessThanEnvLessThanCli_ForOperationalParams()
    {
        var result = ConfigLoader.Load(new ConfigLoadOptions
        {
            WorkingDirectory = ServiceRoot,
            Args = ["--port=4300"],
            Environment = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["APP_MODE"] = "production",
                ["PORT"] = "4200",
                ["TRUSTED_ORIGINS"] = "http://env.local:3000",
                ["RATE_LIMIT_READ_MAX"] = "77"
            }
        });

        Assert.Equal("production", result.Config.Mode);
        Assert.Equal(4300, result.Config.Port);
        Assert.Equal(["http://env.local:3000"], result.Config.TrustedOrigins);
        Assert.Equal(77, result.Config.RateLimit.ReadMax);
    }

    [Fact]
    public void Mode_IsControlledByConfiguration_CliModeIsIgnored()
    {
        var result = ConfigLoader.Load(new ConfigLoadOptions
        {
            WorkingDirectory = ServiceRoot,
            Args = ["--mode=learning"],
            Environment = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["APP_MODE"] = "production"
            }
        });

        Assert.Equal("production", result.Config.Mode);
    }

    [Fact]
    public void InvalidConfig_IsDetectedBeforeServerStart()
    {
        var validation = ConfigValidator.Validate(new AppConfig
        {
            Mode = "production",
            Port = 70000,
            TrustedOrigins = ["not-an-origin"],
            RateLimit = new RateLimitConfig
            {
                WindowMs = -1,
                ReadMax = 0,
                CreateMax = 1
            }
        });

        Assert.False(validation.Ok);
        Assert.True(validation.Errors.Count >= 3);
    }

    [Fact]
    public void LearningMode_PrintsDetailedStartupErrors()
    {
        using var writer = new StringWriter();
        var original = Console.Error;
        Console.SetError(writer);

        try
        {
            ServerBootstrap.PrintStartupErrors("learning", ["port invalid", "origin invalid"]);
        }
        finally
        {
            Console.SetError(original);
        }

        var lines = writer.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Configuration errors:", lines[0]);
        Assert.Equal("- port invalid", lines[1]);
        Assert.Equal("- origin invalid", lines[2]);
    }

    [Fact]
    public void ProductionMode_PrintsMinimalStartupError()
    {
        using var writer = new StringWriter();
        var original = Console.Error;
        Console.SetError(writer);

        try
        {
            ServerBootstrap.PrintStartupErrors("production", ["port invalid"]);
        }
        finally
        {
            Console.SetError(original);
        }

        var text = writer.ToString().Trim();
        Assert.Equal("Invalid configuration", text);
    }
}
