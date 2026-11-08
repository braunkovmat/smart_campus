using CampusMarket.Client.Models;
using CampusMarket.Client.Services;

namespace CampusMarket.Client.UI;

// Слой интерфейса: только вывод и ввод, без HTTP и JSON
public class ConsoleMenu
{
    private readonly ICampusApiClient _api;

    public ConsoleMenu(ICampusApiClient api) => _api = api;

    public async Task RunAsync()
    {
        Console.WriteLine("=== Маркетплейс кампуса ===");
        var list = await _api.GetCatalogItemsAsync();
        Console.WriteLine($"Товаров в каталоге: {list.Total}");
        foreach (var i in list.Items)
            Console.WriteLine($"  #{i.Id,-4} {i.Name,-28} {i.Price,9:F2} руб.  остаток: {i.Stock}");

        var req = new CheckoutRequest { CustomerId = "student-14239", PaymentMethod = "campus_card" };
        req.Lines.Add(new OrderLine { ItemId = list.Items[0].Id, Quantity = 2 });
        Console.WriteLine($"\nОформляем заказ: товар #{req.Lines[0].ItemId} x {req.Lines[0].Quantity}");
        try
        {
            var order = await _api.CheckoutAsync(req);
            Console.WriteLine($"Заказ {order.OrderId}: {order.Status}, сумма {order.Total:F2} руб.");
        }
        catch (CampusApiException ex)
        {
            Console.WriteLine($"Ошибка {ex.StatusCode} [{ex.Code}]: {ex.Message}");
        }

        req.Lines[0].Quantity = 500;
        Console.WriteLine($"\nОформляем заказ: товар #{req.Lines[0].ItemId} x {req.Lines[0].Quantity}");
        try
        {
            var order = await _api.CheckoutAsync(req);
            Console.WriteLine($"Заказ {order.OrderId}: {order.Status}");
        }
        catch (CampusApiException ex)
        {
            Console.WriteLine($"Ошибка {ex.StatusCode} [{ex.Code}]: {ex.Message}");
        }
    }
}
