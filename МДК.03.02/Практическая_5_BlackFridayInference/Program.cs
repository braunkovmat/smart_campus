using System.Collections.Concurrent;
using System.Diagnostics;
using BlackFridayInference;

Console.OutputEncoding = System.Text.Encoding.UTF8;
// Имитация сервера с 4 рабочими потоками: так проще показать перегрузку
ThreadPool.SetMinThreads(4, 4);
ThreadPool.SetMaxThreads(4, 4);

const int Requests = 200;   // «Чёрная пятница»: 200 одновременных запросов
Console.WriteLine($"Нагрузка: {Requests} одновременных запросов, инференс одного запроса ~100 мс, 4 потока");

// 1. Исходный вариант: блокирующий вызов модели без ограничений
Console.WriteLine("\n--- Вариант А: исходная система ---");
IRecommender before = new OnnxModelStub();
Report(await Load.RunAsync(before, Requests));

// 2. Исправленный вариант: декораторы мониторинга, лимита, таймаута и запасного ответа
Console.WriteLine("\n--- Вариант Б: после митигации ---");
ThreadPool.SetMaxThreads(64, 64);
ThreadPool.SetMinThreads(16, 16);
var monitor = new LatencyMonitor(
              new TimeoutWithFallback(
              new ConcurrencyLimiter(new AsyncModelStub(), maxParallel: 10), TimeSpan.FromSeconds(1)));
Report(await Load.RunAsync(monitor, Requests));
Console.WriteLine($"Медленных запросов (>1 с) по мониторингу: {monitor.Slow}");

static void Report(Result r)
{
    Console.WriteLine($"Успешно (модель):      {r.Ok}");
    Console.WriteLine($"Запасной ответ:        {r.Fallback}");
    Console.WriteLine($"Ответов дольше 5 с:    {r.Over5s}");
    Console.WriteLine($"Задержка p50 / p95 / max: {r.P50:F0} / {r.P95:F0} / {r.Max:F0} мс");
    Console.WriteLine($"Всего времени теста:   {r.TotalMs:F0} мс");
}
