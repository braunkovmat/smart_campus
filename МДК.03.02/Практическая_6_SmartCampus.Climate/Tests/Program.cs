using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using SmartCampus.Climate.Api.Models;
using SmartCampus.Climate.Tests;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var results = new List<(string Name, bool Ok, long Ms, string Msg)>();

async Task Run(string name, Func<Task> test)
{
    var sw = Stopwatch.StartNew();
    try { await test(); results.Add((name, true, sw.ElapsedMilliseconds, "")); }
    catch (Exception ex) { results.Add((name, false, sw.ElapsedMilliseconds, ex.Message)); }
}
void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }

// Тест 1 (позитивный): корректная запись сохраняется и возвращается (200 OK)
await Run("Post_ValidRecord_Returns200_AndGetReturnsIt", async () =>
{
    await using var f = new CustomApiFactory();
    var rec = new ClimateSensorRecord { AuditoriumCode = "A-101", Temperature = 21.5f, Humidity = 45f };

    var post = await f.Client.PostAsJsonAsync("/api/v1/climate/sensor", rec);
    Assert(post.StatusCode == HttpStatusCode.OK, $"POST вернул {(int)post.StatusCode}");

    var list = await f.Client.GetFromJsonAsync<List<ClimateSensorRecord>>("/api/v1/climate/sensor");
    Assert(list!.Count == 1, "в списке должна быть 1 запись");
    Assert(list[0].AuditoriumCode == "A-101" && list[0].Temperature == 21.5f, "данные не совпали");
});

// Тест 2 (негативный): AuditoriumCode = null -> 400 Bad Request
await Run("Post_NullAuditoriumCode_Returns400", async () =>
{
    await using var f = new CustomApiFactory();
    var bad = new ClimateSensorRecord { AuditoriumCode = null, Temperature = 20f, Humidity = 40f };

    var post = await f.Client.PostAsJsonAsync("/api/v1/climate/sensor", bad);
    Assert(post.StatusCode == HttpStatusCode.BadRequest, $"ожидали 400, получили {(int)post.StatusCode}");

    var list = await f.Client.GetFromJsonAsync<List<ClimateSensorRecord>>("/api/v1/climate/sensor");
    Assert(list!.Count == 0, "некорректная запись не должна сохраняться");
});

// Тест 3 (изоляция): данные одного теста не видны в другом
await Run("Isolation_EachTestStartsWithEmptyDatabase", async () =>
{
    await using (var first = new CustomApiFactory())
    {
        await first.Client.PostAsJsonAsync("/api/v1/climate/sensor",
            new ClimateSensorRecord { AuditoriumCode = "B-202", Temperature = 19f, Humidity = 50f });
    }
    await using var second = new CustomApiFactory();
    var list = await second.Client.GetFromJsonAsync<List<ClimateSensorRecord>>("/api/v1/climate/sensor");
    Assert(list!.Count == 0, $"в новой базе найдено записей: {list.Count}");
});

foreach (var r in results)
    Console.WriteLine($"{(r.Ok ? "PASSED" : "FAILED")}  {r.Name} [{r.Ms} мс]{(r.Ok ? "" : "  -> " + r.Msg)}");
int passed = results.Count(r => r.Ok), failed = results.Count - passed;
Console.WriteLine($"\nИтого: всего {results.Count}, успешно {passed}, провалено {failed}");
return failed;
