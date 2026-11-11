using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace SmokeTests;

// ПК №21, вариант 3: дымовые тесты АРМ библиотеки после установки.
// Проверяем: переменные окружения, целостность файла (SHA-256), доступность NuGet.
// Результат сохраняется в протокол приёмки protocol.md (Markdown).
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}")
            .WriteTo.File("logs/smoke-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Секреты берём из переменных окружения, а не из кода
        string? dbPassword = Environment.GetEnvironmentVariable("LIB_DB_PASSWORD");
        string nugetUrl = Environment.GetEnvironmentVariable("NUGET_INDEX_URL") ?? "https://api.nuget.org/v3/index.json";

        string file = "arm.config";
        File.WriteAllText(file, "library=main;mode=prod");
        string expected = Hash(file);
        if (args.Contains("--tamper")) File.AppendAllText(file, "#changed"); // имитация порчи файла

        var results = new List<(string Name, bool Ok, string Info)>();

        // Тест 1. Переменная окружения с паролем БД
        bool t1 = !string.IsNullOrEmpty(dbPassword);
        results.Add(("Переменная LIB_DB_PASSWORD", t1, t1 ? "задана" : "не задана"));

        // Тест 2. Целостность файла конфигурации
        bool t2 = Hash(file) == expected;
        results.Add(("Целостность arm.config (SHA-256)", t2, t2 ? "совпадает" : "файл изменён"));

        // Тест 3. Доступность NuGet
        bool t3;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var resp = await client.GetAsync(nugetUrl);
            t3 = resp.IsSuccessStatusCode;
            results.Add(("Доступность NuGet", t3, $"HTTP {(int)resp.StatusCode}"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            t3 = false;
            results.Add(("Доступность NuGet", false, "нет ответа"));
        }

        // Пароль в лог не пишем - только факт проверки
        var md = new StringBuilder("# Протокол приёмки АРМ библиотеки\n\n| Проверка | Результат | Детали |\n|---|---|---|\n");
        foreach (var r in results)
        {
            if (r.Ok) Log.Information("{Name}: PASS ({Info})", r.Name, r.Info);
            else Log.Error("{Name}: FAIL ({Info})", r.Name, r.Info);
            md.AppendLine($"| {r.Name} | {(r.Ok ? "PASS" : "FAIL")} | {r.Info} |");
        }
        bool all = results.All(r => r.Ok);
        md.AppendLine($"\nИтог: {(all ? "приёмка пройдена" : "приёмка НЕ пройдена")}");
        File.WriteAllText("protocol.md", md.ToString(), Encoding.UTF8);

        Log.Information("Итог: {Result}. Протокол: protocol.md", all ? "PASS" : "FAIL");
        Log.CloseAndFlush();
        return all ? 0 : 1;
    }

    private static string Hash(string path)
    {
        using var fs = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(fs));
    }
}
