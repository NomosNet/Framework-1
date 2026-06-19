using Framework3Service;
using Framework3Service.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Framework3Service.Tests;

internal static class HttpTestHelper
{
    public static async Task<(WebApplication App, HttpClient Client)> StartAsync(AppConfig config)
    {
        var app = AppFactory.Create(config);
        await app.StartAsync();

        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses;
        var client = new HttpClient
        {
            BaseAddress = new Uri(addresses.First())
        };

        return (app, client);
    }
}
