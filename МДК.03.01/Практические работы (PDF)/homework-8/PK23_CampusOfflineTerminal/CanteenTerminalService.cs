using System;
using System.Linq;

namespace CampusOfflineTerminal
{
    public class CanteenTerminalService
    {
        public void ProcessPurchase(string cardCode, string item, decimal price)
        {
            using var db = new LocalCampusDbContext();
            db.Database.EnsureCreated(); // Автосоздание файла БД при первом запуске

            // 1. Быстрая локальная проверка карты и баланса по кэшу SQLite
            var card = db.CachedCards.FirstOrDefault(c => c.CardCode == cardCode);

            if (card == null || !card.IsActive)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DENIED] Карта {cardCode} не найдена или заблокирована.");
                Console.ResetColor();
                return;
            }

            if (card.Balance < price)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DENIED] Недостаточно средств: баланс {card.Balance} руб., цена {price} руб.");
                Console.ResetColor();
                return;
            }

            // 2. Списание суммы с локального баланса
            card.Balance -= price;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[OK] Покупка '{item}' на {price} руб. Остаток баланса: {card.Balance} руб.");
            Console.ResetColor();

            // 3. Фиксация транзакции в локальной очереди независимо от статуса сети
            var transaction = new MealTransaction
            {
                StudentId = card.StudentId,
                Amount = price,
                Item = item,
                Timestamp = DateTime.Now,
                IsSynced = false
            };

            db.MealTransactionsQueue.Add(transaction);
            db.SaveChanges(); // баланс и транзакция сохраняются одной операцией
            Console.WriteLine("[BUFFER] Транзакция записана в локальную очередь SQLite.");
        }
    }
}
