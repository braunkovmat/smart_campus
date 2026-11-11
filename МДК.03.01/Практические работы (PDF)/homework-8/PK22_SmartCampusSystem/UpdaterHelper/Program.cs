using System.Diagnostics;
using Serilog;

namespace UpdaterHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            // Принимаем аргументы: [0] временная папка, [1] рабочая папка, [2] PID клиента
            if (args.Length < 3)
            {
                Console.WriteLine("Ошибка: передано недостаточно аргументов для обновления.");
                return;
            }

            string stagingDir = args[0];
            string targetDir = args[1];

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}")
                .WriteTo.File(Path.Combine(targetDir, "Logs", "updater.log"), shared: true)
                .CreateLogger();

            if (int.TryParse(args[2], out int parentPid))
            {
                try
                {
                    var parent = Process.GetProcessById(parentPid);
                    Log.Information("Ожидание закрытия АРМ (PID: {Pid})...", parentPid);
                    parent.WaitForExit(10000);
                }
                catch { /* процесс уже завершён */ }
            }

            Log.Information("Применение патча и подмена файлов...");

            var backups = new List<string>();
            try
            {
                foreach (string source in Directory.GetFiles(stagingDir))
                {
                    string target = Path.Combine(targetDir, Path.GetFileName(source));

                    // Rollback: перед перезаписью делаем резервную копию (.bak)
                    if (File.Exists(target))
                    {
                        File.Copy(target, target + ".bak", true);
                        backups.Add(target);
                        Log.Debug("Резервная копия: {File}.bak", Path.GetFileName(target));
                    }

                    File.Copy(source, target, true);
                    Log.Debug("Файл {File} заменён", Path.GetFileName(target));

                    // Имитация сбоя при распаковке (для проверки отката): срабатывает один раз,
                    // если в папке программы лежит файл simulate_fail.flag
                    string flag = Path.Combine(targetDir, "simulate_fail.flag");
                    if (File.Exists(flag))
                    {
                        File.Delete(flag);
                        throw new IOException("Сбой записи на диск во время распаковки патча");
                    }
                }
                Log.Information("[OK] Файлы успешно обновлены!");
            }
            catch (Exception ex)
            {
                Log.Error("[CRITICAL] Ошибка применения патча: {Message}", ex.Message);

                // Откат: возвращаем старые файлы из .bak
                foreach (string target in backups)
                {
                    File.Copy(target + ".bak", target, true);
                    Log.Warning("Откат: восстановлен {File} из резервной копии", Path.GetFileName(target));
                }
            }

            RestartClient(targetDir);
            Log.CloseAndFlush();
        }

        // Перезапуск основного клиентского модуля
        private static void RestartClient(string targetDir)
        {
            string exe = Path.Combine(targetDir, "CampusClient.exe");
            string dll = Path.Combine(targetDir, "CampusClient.dll");
            ProcessStartInfo? info = null;
            if (OperatingSystem.IsWindows() && File.Exists(exe))
                info = new ProcessStartInfo { FileName = exe, UseShellExecute = true };
            else if (File.Exists(dll))
                info = new ProcessStartInfo { FileName = "dotnet", Arguments = $"\"{dll}\"", UseShellExecute = false };

            if (info != null)
            {
                Process.Start(info);
                Log.Information("[OK] АРМ перезапущен.");
            }
        }
    }
}
