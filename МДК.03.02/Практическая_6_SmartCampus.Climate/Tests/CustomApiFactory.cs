using Microsoft.AspNetCore.Builder;
using SmartCampus.Climate.Api;

namespace SmartCampus.Climate.Tests;

// Аналог CustomWebApplicationFactory: поднимает отдельный экземпляр API на свободном порту
public class CustomApiFactory : IAsyncDisposable
{
    private readonly WebApplication _app;
    public HttpClient Client { get; }

    public CustomApiFactory()
    {
        _app = ApiHost.Create("http://127.0.0.1:0");
        _app.StartAsync().GetAwaiter().GetResult();
        var address = _app.Urls.First();
        Client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
