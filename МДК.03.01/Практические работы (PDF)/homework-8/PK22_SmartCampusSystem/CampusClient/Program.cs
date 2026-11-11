using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Serilog;

namespace CampusClient
{
    // Описание одного файла патча в manifest.json
    public class PatchFile
    {
        public string Name { get; set; } = "";
        public string Sha256 { get; set; } = "";
    }

    // Управляющий файл manifest.json
    public class Manifest
    {
        public string Version { get; set; } = "";
        public List<PatchFile> Files { get; set; } = new();
    }

    class Program
    {
        private static readonly string AppDir = AppContext.BaseDirectory;
        private static readonly string StagingDir = Path.Combine(Path.GetTempPath(), "CampusStaging");
        // Папка, которая изображает сервер обновлений
        private static readonly string ServerDir = Path.Combine(AppDir, "server");

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}")
                .WriteTo.File(Path.Combine(AppDir, "Logs", "updater.log"), shared: true)
                .CreateLogger();

            // Текущая версия хранится в файле version.txt (по умолчанию 2.0.0)
            string versionFile = Path.Combine(AppDir, "version.txt");
            string currentVersion = File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : "2.0.0";

            Console.WriteLine($"=== АРМ Умный кампус запущен (Версия: {currentVersion}) ===");

            await CheckForUpdatesAsync(currentVersion);

            Console.WriteLine("Работа клиентского модуля продолжается в штатном режиме...");
            if (!Console.IsInputRedirected) Console.ReadLine();
            Log.CloseAndFlush();
        }

        private static async Task CheckForUpdatesAsync(string currentVersion)
        {
            try
            {
                Log.Information("Проверка обновлений на сервере...");

                // 1. Читаем manifest.json
                string manifestPath = Path.Combine(ServerDir, "manifest.json");
                string json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<Manifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

                // 2. Сравниваем версии по SemVer
                if (Version.Parse(manifest.Version) <= Version.Parse(currentVersion))
                {
                    Log.Information("Установлена актуальная версия {Version}. Обновление не требуется.", currentVersion);
                    return;
                }
                Log.Information("Обнаружена новая версия {New} (текущая {Old}). Загрузка файлов патча...", manifest.Version, currentVersion);

                // 3. Скачиваем только файлы патча во временную папку и проверяем SHA-256
                if (Directory.Exists(StagingDir)) Directory.Delete(StagingDir, true);
                Directory.CreateDirectory(StagingDir);

                foreach (var file in manifest.Files)
                {
                    string target = Path.Combine(StagingDir, file.Name);
                    File.Copy(Path.Combine(ServerDir, file.Name), target, true);

                    string actual = ComputeSha256(target);
                    if (!actual.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        // Хэш не совпал - обновление прерываем
                        Log.Error("Хэш SHA-256 файла {File} не совпадает. Ожидался {Expected}, получен {Actual}. Обновление прервано.",
                            file.Name, file.Sha256[..12] + "...", actual[..12] + "...");
                        Directory.Delete(StagingDir, true);
                        return;
                    }
                    Log.Debug("Файл {File}: SHA-256 совпал", file.Name);
                }

                // 4. Передаём управление помощнику
                StartUpdaterHelper(StagingDir, AppDir);
            }
            catch (Exception ex)
            {
                Log.Error("Ошибка проверки обновлений: {Message}", ex.Message);
            }
        }

        // Считает SHA-256 файла
        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }

        private static void StartUpdaterHelper(string stagingDir, string targetDir)
        {
            string helperExe = Path.Combine(targetDir, "UpdaterHelper.exe");
            string helperDll = Path.Combine(targetDir, "UpdaterHelper.dll");
            int currentPid = Environment.ProcessId;
            string helperArgs = $"\"{stagingDir}\" \"{targetDir}\" {currentPid}";

            ProcessStartInfo info;
            if (File.Exists(helperExe))
                info = new ProcessStartInfo { FileName = helperExe, Arguments = helperArgs, UseShellExecute = false };
            else if (File.Exists(helperDll))
                info = new ProcessStartInfo { FileName = "dotnet", Arguments = $"\"{helperDll}\" {helperArgs}", UseShellExecute = false };
            else
            {
                Log.Warning("Утилита UpdaterHelper не найдена в каталоге.");
                return;
            }

            Log.Information("Передача управления фоновому процессу обновления...");
            Log.CloseAndFlush();
            Process.Start(info);

            // Штатное выключение, чтобы освободить файлы для перезаписи
            Environment.Exit(0);
        }
    }
}
