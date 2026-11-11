using Polly;
using Polly.Retry;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CampusOfflineTerminal
{
    public class BackgroundSyncService
    {
        private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

        // Адрес синхронизации варианта 3: /api/v1/sync/canteen
        // (httpbin.org/anything возвращает 200 OK на любой путь, поэтому подходит для проверки)
        private const string SyncUrl = "https://httpbin.org/anything/api/v1/sync/canteen";

        // Настройка политики повторов Polly: 3 попытки с интервалом 2с, 4с, 8с
        private readonly AsyncRetryPolicy _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[WARN] Ошибка отправки пакета. Попытка {retryCount} через {timeSpan.Seconds}с. Ошибка: {exception.Message}");
                    Console.ResetColor();
                });

        public async Task SyncPendingEventsAsync()
        {
            using var db = new LocalCampusDbContext();

            // Выбираем все транзакции, которые еще не были синхронизированы
            var pending = db.MealTransactionsQueue.Where(e => !e.IsSynced).ToList();

            if (pending.Count == 0)
            {
                Console.WriteLine("[SYNC] Локальная очередь пуста. Синхронизация не требуется.");
                return;
            }

            Console.WriteLine($"[SYNC] Обнаружено неподтвержденных транзакций: {pending.Count}. Начинаем отправку...");

            foreach (var tx in pending)
            {
                bool success = await SendWithRetryAsync(tx);
                if (success)
                {
                    tx.IsSynced = true;
                    db.SaveChanges();
                    Console.WriteLine($"[SYNC OK] Транзакция ID {tx.Id} успешно передана на сервер.");
                }
                else
                {
                    Console.WriteLine($"[SYNC FAIL] Не удалось передать транзакцию ID {tx.Id}. Оставляем в локальном буфере.");
                    break; // Прерываем цикл при длительном обрыве сети
                }
            }
        }

        private async Task<bool> SendWithRetryAsync(MealTransaction tx)
        {
            string payload = JsonSerializer.Serialize(tx);

            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var response = await HttpClient.PostAsync(
                        SyncUrl,
                        new StringContent(payload, Encoding.UTF8, "application/json"));

                    return response.IsSuccessStatusCode;
                });
            }
            catch
            {
                return false; // Сеть полностью недоступна
            }
        }
    }
}
