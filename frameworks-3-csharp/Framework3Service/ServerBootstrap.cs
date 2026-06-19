using Framework3Service.Configuration;

namespace Framework3Service;

public static class ServerBootstrap
{
    public static async Task<int> RunAsync(string[] args)
    {
        var result = ConfigLoader.Load(new ConfigLoadOptions { Args = args });
        var validation = ConfigValidator.Validate(result.Config);

        if (!validation.Ok)
        {
            PrintStartupErrors(result.Config.Mode, validation.Errors);
            return 1;
        }

        var app = AppFactory.Create(result.Config);
        if (result.Config.Mode == "learning")
        {
            Console.WriteLine($"Service started on port {result.Config.Port} in {result.Config.Mode} mode");
        }
        else
        {
            Console.WriteLine("Service started");
        }

        await app.RunAsync();
        return 0;
    }

    public static void PrintStartupErrors(string mode, IReadOnlyList<string> errors)
    {
        if (string.Equals(mode, "learning", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Configuration errors:");
            foreach (var error in errors)
            {
                Console.Error.WriteLine($"- {error}");
            }

            return;
        }

        Console.Error.WriteLine("Invalid configuration");
    }
}
