using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace CampusOfflineTerminal
{
    // Контекст локальной базы данных
    public class LocalCampusDbContext : DbContext
    {
        public DbSet<CachedCard> CachedCards { get; set; } = null!;
        public DbSet<MealTransaction> MealTransactionsQueue { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Создаем файл БД в локальной папке АРМ
            optionsBuilder.UseSqlite(@"Data Source=campus_offline.db");
        }
    }

    // Кэш карт студентов с балансом питания (для проверки офлайн)
    public class CachedCard
    {
        [Key]
        public int Id { get; set; }
        public string CardCode { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
    }

    // Очередь транзакций питания (Outbox Pattern), вариант 3
    public class MealTransaction
    {
        [Key]
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Item { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsSynced { get; set; } = false; // Статус отправки на сервер
    }
}
