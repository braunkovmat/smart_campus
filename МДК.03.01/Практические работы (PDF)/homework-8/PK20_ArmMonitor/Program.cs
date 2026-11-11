using System.Diagnostics;
using System.Text.Json;
using Serilog;

namespace ArmMonitor;

// ПК №20, вариант 3: мониторинг АРМ с журналированием через Serilog.
// Программа проверяет актуальность пакета в NuGet, замеряет задержку ответа
// и пишет всё в консоль и в файл logs/arm-.log (новый файл на каждый день).
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Настройка логгера: вывод в консоль и в файл
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}")
            .WriteTo.File("logs/arm-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        string indexUrl = args.Length > 0 ? args[0] : "https://api.nuget.org/v3/index.json";
        string package = "Serilog";
        string localVersion = "4.2.0";
        int exitCode = 0;

        Log.Information("Мониторинг АРМ запущен. Источник: {Url}", indexUrl);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var timer = Stopwatch.StartNew();
        try
        {
            string json = await client.GetStringAsync(indexUrl);
            timer.Stop();

            // Задержка ответа: больше 1000 мс - предупреждение
            if (timer.ElapsedMilliseconds > 1000)
                Log.Warning("Медленный ответ NuGet: {Ms} мс", timer.ElapsedMilliseconds);
            else
                Log.Information("Ответ NuGet получен за {Ms} мс", timer.ElapsedMilliseconds);

            string baseUrl = FindBaseAddress(json);
            string? latest = await GetLatestAsync(client, baseUrl, package);

            if (latest == null)
            {
                Log.Error("Пакет {Package} не найден в NuGet", package);
                exitCode = 2;
            }
            else if (Version.Parse(localVersion) < Version.Parse(latest))
            {
                Log.Warning("Пакет {Package} устарел: {Local} -> {Latest}", package, localVersion, latest);
                exitCode = 1;
            }
            else
            {
                Log.Information("Пакет {Package} актуален ({Local})", package, localVersion);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Log.Error(ex, "NuGet недоступен");
            exitCode = 2;
        }

        Log.Information("Мониторинг завершён, код {Code}", exitCode);
        Log.CloseAndFlush();
        return exitCode;
    }

    // Достаёт из индекса адрес PackageBaseAddress
    private static string FindBaseAddress(string json)
    {
        using var doc = JsonDocument.Parse(json);
        foreach (var r in doc.RootElement.GetProperty("resources").EnumerateArray())
            if ((r.GetProperty("@type").GetString() ?? "").StartsWith("PackageBaseAddress"))
                return r.GetProperty("@id").GetString()!;
        throw new HttpRequestException("PackageBaseAddress не найден");
    }

    // Последняя стабильная версия пакета
    private static async Task<string?> GetLatestAsync(HttpClient client, string baseUrl, string name)
    {
        var resp = await client.GetAsync($"{baseUrl}{name.ToLowerInvariant()}/index.json");
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Version? max = null;
        foreach (var v in doc.RootElement.GetProperty("versions").EnumerateArray())
        {
            string t = v.GetString() ?? "";
            if (!t.Contains('-') && Version.TryParse(t, out var ver) && (max == null || ver > max)) max = ver;
        }
        return max?.ToString();
    }
}
