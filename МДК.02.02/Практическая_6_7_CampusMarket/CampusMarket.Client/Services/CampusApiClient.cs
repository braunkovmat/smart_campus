using System.Net.Http.Json;
using System.Text.Json;
using CampusMarket.Client.Models;

namespace CampusMarket.Client.Services;

// Сетевой слой: единственное место, которое знает про HTTP и JSON
public class CampusApiClient : ICampusApiClient
{
    private readonly HttpClient _http;

    public CampusApiClient(HttpClient http) => _http = http;

    // GET /catalog/items
    public async Task<CatalogItemList> GetCatalogItemsAsync(string? category = null)
    {
        var url = category == null ? "catalog/items" : $"catalog/items?category={Uri.EscapeDataString(category)}";
        var response = await _http.GetAsync(url);
        await EnsureOkAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CatalogItemList>(json) ?? new CatalogItemList();
    }

    // POST /orders/checkout
    public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request)
    {
        var response = await _http.PostAsJsonAsync("orders/checkout", request);
        await EnsureOkAsync(response);
        return (await response.Content.ReadFromJsonAsync<CheckoutResponse>())!;
    }

    // Превращаем ответ с ошибкой в понятное исключение
    private static async Task EnsureOkAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        ApiError? err = null;
        try { err = JsonSerializer.Deserialize<ApiError>(body); } catch (JsonException) { }
        throw new CampusApiException((int)response.StatusCode, err?.Code ?? "UNKNOWN", err?.Message ?? body);
    }
}

public class CampusApiException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public CampusApiException(int status, string code, string message) : base(message)
    { StatusCode = status; Code = code; }
}
