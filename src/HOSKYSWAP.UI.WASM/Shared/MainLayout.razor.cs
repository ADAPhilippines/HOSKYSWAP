using System.ComponentModel;
using HOSKYSWAP.UI.WASM.Services;
using HOSKYSWAP.UI.WASM.Services.JSInterop;
using Microsoft.AspNetCore.Components;

namespace HOSKYSWAP.UI.WASM.Shared;

public partial class MainLayout : IDisposable
{
    [Inject] protected CardanoWalletInteropService? CardanoWalletInteropService { get; set; }
    [Inject] protected HelperInteropService? HelperInteropService { get; set; }
    [Inject] protected AppStateService? AppStateService { get; set; }
    [Inject] protected BackendService? BackendService { get; set; }
    private string WalletAddress { get; set; } = string.Empty;
    private string UserIdenticon { get; set; } = string.Empty;
    private bool IsNamiWarningDialogVisible { get; set; } = false;

    private ulong CurrentPrice
    {
        get
        {
            if (AppStateService?.LastExcecutedOrder is null) return 0;
            return (ulong) (1 / AppStateService.LastExcecutedOrder.Rate);
        }
    }

    protected override void OnInitialized()
    {
        if (AppStateService != null) AppStateService.PropertyChanged += OnAppStateChanged;
        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetAvatarAsync();
            _ = StartDataPolling();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e) => StateHasChanged();

    private async Task StartDataPolling()
    {
        while (true)
        {
            var tasks = new List<Task>
            {
                Task.Run(async () =>
                {
                    if (CardanoWalletInteropService is null || AppStateService is null ||
                        BackendService is null) return;

                    var walletAddress = await CardanoWalletInteropService.GetWalletAddressAsync();

                    if (walletAddress is null) return;

                    var buyOrders = await BackendService.GetOpenBuyOrdersByAddressAsync(walletAddress);
                    AppStateService.CurrentOrder = buyOrders?.FirstOrDefault();
                }),
                Task.Run(async () =>
                {
                    if (BackendService is null || AppStateService is null) return;
                    var rate = await BackendService.GetADAPriceAsync();

                    if (rate is null) return;
                    AppStateService.MarketCap = await BackendService.GetMarketCapAsync(rate.Cardano.USD);
                }),
                Task.Run(async () =>
                {
                    if (AppStateService is not null && BackendService is not null)
                        AppStateService.TotalFeesRugpulled = await BackendService.GetTotalFeesRugpulledAsync();
                }),
                Task.Run(async () =>
                {
                    if (AppStateService is not null && BackendService is not null)
                        AppStateService.OpenBuyOrders = await BackendService.GetOpenBuyOrdersAsync();
                }),
                Task.Run(async () =>
                {
                    if (AppStateService is not null && BackendService is not null)
                        AppStateService.OpenSellOrders = await BackendService.GetOpenSellOrdersAsync();
                }),
                Task.Run(async () =>
                {
                    if (CardanoWalletInteropService is null || AppStateService is null ||
                        BackendService is null) return;

                    var walletAddress = await CardanoWalletInteropService.GetWalletAddressAsync();

                    if (walletAddress is null) return;

                    if (AppStateService is not null && BackendService is not null)
                        AppStateService.OrderHistory = await BackendService.GetOrderHistoryAsync(walletAddress);
                }),
                Task.Run(async () =>
                {
                    if (AppStateService is not null && BackendService is not null)
                        AppStateService.OpenOrderRatio = await BackendService.GetOpenOrderRatioAsync();
                }),
                Task.Run(async () =>
                {
                    if (AppStateService is null || BackendService is null) return;
                    AppStateService.LastExcecutedOrder = await BackendService.GetLastExecutedOrderAsync();
                })
            };

            await Task.WhenAll(tasks);
            await Task.Delay(5000);
        }
    }

    private async void OnConnectBtnClicked()
    {
        if (CardanoWalletInteropService != null && !await CardanoWalletInteropService.HasNamiAsync())
        {
            IsNamiWarningDialogVisible = true;
        }
        else
        {
            if (CardanoWalletInteropService is null || await CardanoWalletInteropService.IsWalletConnectedAsync() ||
                AppStateService is null) return;
            AppStateService.IsWalletConnected = await CardanoWalletInteropService.ConnectWalletAsync();
            await SetAvatarAsync();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SetAvatarAsync()
    {
        if (CardanoWalletInteropService is not null && await CardanoWalletInteropService.IsWalletConnectedAsync() &&
            AppStateService is not null)
        {
            AppStateService.IsWalletConnected = true;
            WalletAddress = await CardanoWalletInteropService.GetWalletAddressAsync() ?? String.Empty;
            UserIdenticon = await GetIdenticonAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    private string FormatAddress()
    {
        return WalletAddress.Length > 10
            ? $"{WalletAddress.Substring(0, 4)}...{WalletAddress[^8..]}"
            : WalletAddress;
    }

    private async Task<string> GetIdenticonAsync()
    {
        if (HelperInteropService is not null)
            return await HelperInteropService.GenerateIdenticonAsync(WalletAddress);
        else
            return string.Empty;
    }

    public void Dispose()
    {
        if (AppStateService != null) AppStateService.PropertyChanged += OnAppStateChanged;
    }
}