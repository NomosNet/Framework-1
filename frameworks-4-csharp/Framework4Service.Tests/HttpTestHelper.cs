using Framework4Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Framework4Service.Tests;

internal static class HttpTestHelper
{
    public static async Task<(WebApplication App, HttpClient Client)> StartAsync(string url = "http://127.0.0.1:0")
    {
        var app = AppFactory.Create(url);
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
