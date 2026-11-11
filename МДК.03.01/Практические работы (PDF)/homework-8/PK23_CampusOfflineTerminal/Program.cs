using System;
using System.Linq;
using System.Threading.Tasks;

namespace CampusOfflineTerminal
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== АРМ СТОЛОВОЙ С РЕЖИМОМ OFFLINE-FIRST (.NET 8) ===");

            // Инициализация тестовых данных в SQLite, если таблица пуста
            SeedDatabase();

            var terminal = new CanteenTerminalService();
            var syncService = new BackgroundSyncService();

            // 1. Имитируем покупки студента во время работы терминала
            Console.WriteLine("\n--- Симуляция покупок по карте ---");
            terminal.ProcessPurchase("RFID-998877", "Комплексный обед", 250m);
            terminal.ProcessPurchase("RFID-998877", "Компот", 40m);
            terminal.ProcessPurchase("RFID-000000", "Булочка", 30m); // неизвестная карта

            // 2. Имитируем попытку фоновой синхронизации накопленных данных с сервером
            Console.WriteLine("\n--- Запуск фоновой синхронизации с сервером ---");
            await syncService.SyncPendingEventsAsync();

            Console.WriteLine("\nРабота терминала завершена. Нажмите Enter для выхода.");
            Console.ReadLine();
        }

        private static void SeedDatabase()
        {
            using var db = new LocalCampusDbContext();
            db.Database.EnsureCreated();

            if (!db.CachedCards.Any())
            {
                db.CachedCards.Add(new CachedCard { CardCode = "RFID-998877", StudentId = "ST-103", Balance = 500m, IsActive = true });
                db.SaveChanges();
            }
        }
    }
}
