using System.Text;
using System.Text.Json;

namespace NuGetCheck;

// ПК №19, вариант 3: аудит зависимостей проекта через NuGet API.
// Программа берёт локальные версии пакетов, спрашивает у NuGet актуальные версии,
// сравнивает их и формирует отчёт для администратора (файл nuget_audit_report.md).
public class Program
{
    // Локальные версии пакетов, которые используются в проекте АРМ
    private static readonly Dictionary<string, string> LocalPackages = new()
    {
        { "Serilog", "3.1.1" },
        { "Newtonsoft.Json", "12.0.3" }
    };

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Адрес главного индекса NuGet (можно передать другим аргументом)
        string indexUrl = args.Length > 0 ? args[0] : "https://api.nuget.org/v3/index.json";

        Console.WriteLine("=== Аудит зависимостей через NuGet API ===");
        Console.WriteLine($"Источник: {indexUrl}");

        var report = new StringBuilder();
        report.AppendLine("# Отчёт для администратора: актуальность NuGet-пакетов");
        report.AppendLine($"Дата проверки: {DateTime.Now:dd.MM.yyyy HH:mm}");
        report.AppendLine();
        report.AppendLine("| Пакет | Локальная версия | Актуальная версия | Статус |");
        report.AppendLine("|---|---|---|---|");

        int outdated = 0;

        // Таймаут 10 секунд, чтобы программа не зависла при плохой сети
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            // Шаг 1. Из индекса NuGet узнаём адрес, где лежат списки версий пакетов
            string baseUrl = await GetBaseAddressAsync(client, indexUrl);

            // Шаг 2. Для каждого локального пакета получаем актуальную версию и сравниваем
            foreach (var package in LocalPackages)
            {
                string? latest = await GetLatestVersionAsync(client, baseUrl, package.Key);

                string status;
                if (latest == null)
                {
                    status = "Пакет не найден";
                }
                else if (Version.Parse(package.Value) >= Version.Parse(latest))
                {
                    status = "Актуальна";
                }
                else
                {
                    status = "Устарела, нужно обновить";
                    outdated++;
                }

                Console.WriteLine($"{package.Key}: локально {package.Value}, актуальная {latest ?? "-"} -> {status}");
                report.AppendLine($"| {package.Key} | {package.Value} | {latest ?? "-"} | {status} |");
            }
        }
        catch (HttpRequestException ex)
        {
            // Нет сети или сервер недоступен
            Console.WriteLine($"Ошибка сети: {ex.Message}");
            return 2;
        }
        catch (TaskCanceledException)
        {
            // Сработал таймаут
            Console.WriteLine("Ошибка: превышено время ожидания ответа NuGet (10 секунд).");
            return 2;
        }

        report.AppendLine();
        report.AppendLine(outdated == 0
            ? "Вывод: все зависимости актуальны."
            : $"Вывод: устаревших пакетов - {outdated}. Рекомендуется обновить их перед развёртыванием.");

        // Сохраняем отчёт в Markdown (Docs-as-Code)
        File.WriteAllText("nuget_audit_report.md", report.ToString(), Encoding.UTF8);
        Console.WriteLine();
        Console.WriteLine($"Устаревших пакетов: {outdated}. Отчёт сохранён: nuget_audit_report.md");

        return outdated == 0 ? 0 : 1;
    }

    // Ищет в индексе NuGet ресурс PackageBaseAddress и возвращает его адрес
    private static async Task<string> GetBaseAddressAsync(HttpClient client, string indexUrl)
    {
        string json = await client.GetStringAsync(indexUrl);
        using var doc = JsonDocument.Parse(json);

        foreach (var resource in doc.RootElement.GetProperty("resources").EnumerateArray())
        {
            string type = resource.GetProperty("@type").GetString() ?? "";
            if (type.StartsWith("PackageBaseAddress"))
            {
                return resource.GetProperty("@id").GetString()!;
            }
        }
        throw new HttpRequestException("В индексе NuGet не найден ресурс PackageBaseAddress");
    }

    // Возвращает последнюю стабильную версию пакета (версии с дефисом - beta/rc - пропускаем)
    private static async Task<string?> GetLatestVersionAsync(HttpClient client, string baseUrl, string packageName)
    {
        var response = await client.GetAsync($"{baseUrl}{packageName.ToLowerInvariant()}/index.json");
        if (!response.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Version? max = null;
        foreach (var item in doc.RootElement.GetProperty("versions").EnumerateArray())
        {
            string text = item.GetString() ?? "";
            if (text.Contains('-')) continue;

            if (Version.TryParse(text, out var version) && (max == null || version > max))
            {
                max = version;
            }
        }
        return max?.ToString();
    }
}
