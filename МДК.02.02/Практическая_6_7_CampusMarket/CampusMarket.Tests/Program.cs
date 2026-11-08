using System.Net;
using System.Text;
using CampusMarket.Client.Services;

// Заглушка HTTP: возвращает заранее заданный JSON без сети
class FakeHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _code; private readonly string _json;
    public FakeHandler(HttpStatusCode code, string json) { _code = code; _json = json; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) =>
        Task.FromResult(new HttpResponseMessage(_code) { Content = new StringContent(_json, Encoding.UTF8, "application/json") });
}

static class Tests
{
    static int failed;
    static void Check(string name, bool ok)
    {
        Console.WriteLine($"{(ok ? "PASSED" : "FAILED")}  {name}");
        if (!ok) failed++;
    }
    static CampusApiClient Make(HttpStatusCode c, string json) =>
        new(new HttpClient(new FakeHandler(c, json)) { BaseAddress = new Uri("http://test/api/v1/") });

    public static async Task<int> Main()
    {
        // 1. Десериализация списка товаров
        var json = "{\"items\":[{\"id\":101,\"name\":\"Худи\",\"category\":\"clothes\",\"price\":2490.5,\"stock\":35}],\"total\":1}";
        var list = await Make(HttpStatusCode.OK, json).GetCatalogItemsAsync();
        Check("GetCatalogItems_ParsesItems", list.Total == 1 && list.Items.Count == 1);
        Check("GetCatalogItems_ParsesFields", list.Items[0].Id == 101 && list.Items[0].Name == "Худи" && list.Items[0].Price == 2490.5 && list.Items[0].Stock == 35);

        // 2. Десериализация ответа оформления заказа
        var order = await Make(HttpStatusCode.Created, "{\"orderId\":\"ORD-1\",\"status\":\"created\",\"total\":4981.0}")
            .CheckoutAsync(new() { CustomerId = "s1" });
        Check("Checkout_ParsesResponse", order.OrderId == "ORD-1" && order.Status == "created" && order.Total == 4981.0);

        // 3. Ошибка 409 превращается в исключение с кодом
        try
        {
            await Make(HttpStatusCode.Conflict, "{\"code\":\"OUT_OF_STOCK\",\"message\":\"Нет на складе\"}").CheckoutAsync(new());
            Check("Checkout_Conflict_Throws", false);
        }
        catch (CampusApiException ex)
        {
            Check("Checkout_Conflict_Throws", ex.StatusCode == 409 && ex.Code == "OUT_OF_STOCK");
        }
        Console.WriteLine(failed == 0 ? "\nВсе тесты пройдены: 4 из 4" : $"\nПровалено тестов: {failed}");
        return failed;
    }
}
