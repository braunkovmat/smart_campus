using System.Diagnostics;

namespace BlackFridayInference;

public record Result(int Ok, int Fallback, int Over5s, double P50, double P95, double Max, double TotalMs);

public static class Load
{
    public static async Task<Result> RunAsync(IRecommender rec, int n)
    {
        var times = new double[n]; var sources = new string[n];
        var total = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, n).Select(async i =>
        {
            var sw = Stopwatch.StartNew();
            var r = await rec.RecommendAsync(i, CancellationToken.None);
            times[i] = sw.Elapsed.TotalMilliseconds; sources[i] = r.Source;
        });
        await Task.WhenAll(tasks);
        total.Stop();
        var s = times.OrderBy(x => x).ToArray();
        return new Result(sources.Count(x => x == "model"), sources.Count(x => x == "fallback"),
            times.Count(t => t > 5000), s[n / 2], s[(int)(n * 0.95)], s[^1], total.Elapsed.TotalMilliseconds);
    }
}
