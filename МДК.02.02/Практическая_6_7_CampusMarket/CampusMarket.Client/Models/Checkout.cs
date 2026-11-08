using System.Text.Json.Serialization;

namespace CampusMarket.Client.Models;

public class OrderLine
{
    [JsonPropertyName("itemId")] public long ItemId { get; set; }
    [JsonPropertyName("quantity")] public int Quantity { get; set; }
}

// Тело POST /orders/checkout (схема CheckoutRequest)
public class CheckoutRequest
{
    [JsonPropertyName("customerId")] public string CustomerId { get; set; } = "";
    [JsonPropertyName("lines")] public List<OrderLine> Lines { get; set; } = new();
    [JsonPropertyName("paymentMethod")] public string PaymentMethod { get; set; } = "card";
}

// Ответ POST /orders/checkout (схема CheckoutResponse)
public class CheckoutResponse
{
    [JsonPropertyName("orderId")] public string OrderId { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("total")] public double Total { get; set; }
    [JsonPropertyName("createdAt")] public DateTime? CreatedAt { get; set; }
}

// Схема Error
public class ApiError
{
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("message")] public string Message { get; set; } = "";
}
