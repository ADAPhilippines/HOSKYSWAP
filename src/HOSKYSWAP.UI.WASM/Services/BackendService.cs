using System.Net.Http.Json;
using HOSKYSWAP.Common;
using HOSKYSWAP.UI.WASM.Models;

namespace HOSKYSWAP.UI.WASM.Services;

public class BackendService
{
    private HttpClient HttpClient { get; set; }

    public BackendService()
    {
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5120")
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

    public async Task<List<Order>?> GetOrderHistoryAsync(string address)
    {
        var orderResponse = await HttpClient.GetAsync("/order/last/" + address);
        orderResponse.EnsureSuccessStatusCode();
        var orderHistory = await orderResponse.Content.ReadFromJsonAsync<List<Order>>();
        return orderHistory;
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

    public async Task<OpenOrderRatio?> GetOpenOrderRatioAsync()
    {
        var orderResponse = await HttpClient.GetAsync("/order/open/ratio");
        orderResponse.EnsureSuccessStatusCode();
        var orderRatio = await orderResponse.Content.ReadFromJsonAsync<OpenOrderRatio>();
        return orderRatio;
    }

    public async Task<decimal> GetMarketCapAsync()
    {
        var lastOrder = await GetLastExecutedOrderAsync();
        var price = await GetADAPriceAsync();
        var totalHOSKY = 1_000_000_000_000_000;

        if (lastOrder != null && price != null) return totalHOSKY * lastOrder.Rate * price.Cardano.USD;
        return 0;
    }
    
    public async Task<decimal> GetDailyVolumeAsync()
    {
        var dailyVolumeUsd = 0m;
        var price = await GetADAPriceAsync();
        var dailyVolumeResponse = await HttpClient.GetAsync("/market/daily/volume");
        dailyVolumeResponse.EnsureSuccessStatusCode();
        
        var dailyVolume = await dailyVolumeResponse.Content.ReadFromJsonAsync<Volume>();

        if (dailyVolume is not null && price is not null) dailyVolumeUsd = dailyVolume.Amount * price.Cardano.USD;
            
        return dailyVolumeUsd;
    }
    
}