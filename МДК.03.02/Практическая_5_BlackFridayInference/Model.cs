using System.Diagnostics;

namespace BlackFridayInference;

public record Rec(string Source, int[] Items);

public interface IRecommender
{
    Task<Rec> RecommendAsync(int userId, CancellationToken ct);
}

// Заглушка ONNX-модели: блокирует поток примерно на 100 мс, как тяжёлый синхронный Run()
public class OnnxModelStub : IRecommender
{
    public Task<Rec> RecommendAsync(int userId, CancellationToken ct) =>
        Task.Run(() =>
        {
            Thread.Sleep(100);                       // синхронный инференс
            return new Rec("model", new[] { userId % 10, 11, 12 });
        });
}

// Исправленная модель: инференс не блокирует поток (вынесен в отдельный сервис/пул), ~100 мс
public class AsyncModelStub : IRecommender
{
    public async Task<Rec> RecommendAsync(int userId, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        return new Rec("model", new[] { userId % 10, 11, 12 });
    }
}

// Декоратор 1: ограничивает число одновременных вызовов модели
public class ConcurrencyLimiter : IRecommender
{
    private readonly IRecommender _inner;
    private readonly SemaphoreSlim _gate;
    public ConcurrencyLimiter(IRecommender inner, int maxParallel)
    { _inner = inner; _gate = new SemaphoreSlim(maxParallel); }

    public async Task<Rec> RecommendAsync(int userId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);                   // ждём свободное место или отмену
        try { return await _inner.RecommendAsync(userId, ct); }
        finally { _gate.Release(); }
    }
}

// Декоратор 2: таймаут и запасной ответ вместо долгого ожидания
public class TimeoutWithFallback : IRecommender
{
    private readonly IRecommender _inner;
    private readonly TimeSpan _timeout;
    public TimeoutWithFallback(IRecommender inner, TimeSpan timeout) { _inner = inner; _timeout = timeout; }

    public async Task<Rec> RecommendAsync(int userId, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeout);
        try { return await _inner.RecommendAsync(userId, cts.Token); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new Rec("fallback", new[] { 1, 2, 3 });   // популярные товары
        }
    }
}

// Декоратор 3: измеряет задержку каждого запроса и считает «медленные»
public class LatencyMonitor : IRecommender
{
    private readonly IRecommender _inner;
    private int _slow;
    public int Slow => _slow;
    public LatencyMonitor(IRecommender inner) => _inner = inner;
    public async Task<Rec> RecommendAsync(int userId, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var r = await _inner.RecommendAsync(userId, ct);
        if (sw.ElapsedMilliseconds > 1000) Interlocked.Increment(ref _slow);
        return r;
    }
}
