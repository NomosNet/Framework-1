namespace Framework4Service;

public static class ServerBootstrap
{
    public static async Task<int> RunAsync(string[] args)
    {
        var app = AppFactory.Create();
        await app.RunAsync();
        return 0;
    }
}
