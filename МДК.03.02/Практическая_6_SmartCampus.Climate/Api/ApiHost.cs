using SmartCampus.Climate.Api.Data;

namespace SmartCampus.Climate.Api;

public static class ApiHost
{
    // Создаёт приложение; Url = null для обычного запуска, либо адрес для тестов
    public static WebApplication Create(string? url = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(ApiHost).Assembly);   // контроллеры из этой сборки
        builder.Services.AddSingleton<ClimateStore>();   // своё хранилище у каждого приложения
        var app = builder.Build();
        app.MapControllers();
        if (url != null) app.Urls.Add(url);
        return app;
    }
}
