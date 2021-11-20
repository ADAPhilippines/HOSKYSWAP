using System.Net.Http.Json;
using HOSKYSWAP.Common;
using HOSKYSWAP.UI.WASM.Models;

namespace HOSKYSWAP.UI.WASM.Services;

public class BackendService
{
    private HttpClient HttpClient { get; set; }

    public BackendService(AppStateService appStateService)
    {
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri( appStateService.BackendUrl)
        };
    }

    public async Task<CardanoPrice?> GetADAPriceAsync()
    {
        var priceResponse =
            await HttpClient.GetAsync("https://api.coingecko.com/api/v3/simple/price?ids=cardano&vs_currencies=usd");
        priceResponse.EnsureSuccessStatusCode();
        var price = await priceResponse.Content.ReadFromJsonAsync<CardanoPrice>();
        return price;
    }
    
    public async Task<Order?> GetLastExecutedOrderAsync()
    {
        var orderResponse = await HttpClient.GetAsync("/order/last/execute");
        orderResponse.EnsureSuccessStatusCode();
        var lastOrder = await orderResponse.Content.ReadFromJsonAsync<Order>();
        return lastOrder;
    }

    public async Task<List<Order>> GetOrderHistoryAsync(string address)
    {
        var orderResponse = await HttpClient.GetAsync("/order/" + address);
        orderResponse.EnsureSuccessStatusCode();
        var orderHistory = await orderResponse.Content.ReadFromJsonAsync<List<Order>>();
        if (orderHistory is not null) return orderHistory;
        return new List<Order>();
    }

    public async Task<List<Order>?> GetOpenSellOrdersAsync()
    {
        var orderResponse = await HttpClient.GetAsync("/order/open/sell");
        orderResponse.EnsureSuccessStatusCode();
        var orderHistory = await orderResponse.Content.ReadFromJsonAsync<List<Order>>();
        return orderHistory;
    }

    public async Task<List<Order>?> GetOpenBuyOrdersAsync()
    {
        var orderResponse = await HttpClient.GetAsync("/order/open/buy");
        orderResponse.EnsureSuccessStatusCode();
        var orderHistory = await orderResponse.Content.ReadFromJsonAsync<List<Order>>();
        return orderHistory;
    }

    public async Task<List<Order>?> GetOpenBuyOrdersByAddressAsync(string? address)
    {
        var orderResponse = await HttpClient.GetAsync($"/order/{address}/open");
        orderResponse.EnsureSuccessStatusCode();
        var openOrders = await orderResponse.Content.ReadFromJsonAsync<List<Order>>();
        return openOrders;
    }

    public async Task<OpenOrderRatio?> GetOpenOrderRatioAsync()
    {
        var orderResponse = await HttpClient.GetAsync("/order/open/ratio");
        orderResponse.EnsureSuccessStatusCode();
        var orderRatio = await orderResponse.Content.ReadFromJsonAsync<OpenOrderRatio>();
        return orderRatio;
    }

    public async Task<decimal> GetMarketCapAsync(decimal adaUsdRate)
    {
        var lastOrder = await GetLastExecutedOrderAsync();
        
        var totalHOSKY = 1_000_000_000_000_000;

        if (lastOrder != null) return totalHOSKY * lastOrder.Rate * adaUsdRate;
        return 0;
    }
    
    public async Task<decimal> GetDailyVolumeAsync(decimal adaUsdRate)
    {
        var dailyVolumeUsd = 0m;
        var dailyVolumeResponse = await HttpClient.GetAsync("/market/daily/volume");
        dailyVolumeResponse.EnsureSuccessStatusCode();
        
        var dailyVolume = await dailyVolumeResponse.Content.ReadFromJsonAsync<Volume>();

        if (dailyVolume is not null) dailyVolumeUsd = dailyVolume.Amount * adaUsdRate;
            
        return dailyVolumeUsd;
    }
    
    public async Task<decimal> GetTotalFeesRugpulledAsync()
    {
        var rugpulledFeesResponse = await HttpClient.GetAsync("/order/total/rugpulled");
        rugpulledFeesResponse.EnsureSuccessStatusCode();
        
        return await rugpulledFeesResponse.Content.ReadFromJsonAsync<decimal>();
    }

    public async Task<List<Order>?> GetGlobalOrderHistoryAsync()
    {
        var orderResponse = await HttpClient.GetAsync($"/order/history");
        orderResponse.EnsureSuccessStatusCode();
        return await orderResponse.Content.ReadFromJsonAsync<List<Order>>();
    }
}