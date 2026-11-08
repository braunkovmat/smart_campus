using CampusMarket.Client.Models;

namespace CampusMarket.Client.Services;

public interface ICampusApiClient
{
    Task<CatalogItemList> GetCatalogItemsAsync(string? category = null);
    Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request);
}
