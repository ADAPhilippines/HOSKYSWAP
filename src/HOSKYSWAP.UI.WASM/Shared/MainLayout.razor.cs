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
    private bool IsCurrentPriceSet { get; set; } = false;

    private decimal CurrentPrice
    {
        get
        {
            if (AppStateService?.LastExcecutedOrder is null) return 0;
            return Math.Round((AppStateService.LastExcecutedOrder.Rate * AppStateService.AdaToUsdRate), 10);
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
            if (CardanoWalletInteropService != null)
                await CardanoWalletInteropService.SetBackendUrl(AppStateService?.BackendUrl ?? string.Empty);
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
                    if (CardanoWalletInteropService is null || 
                        !(await CardanoWalletInteropService.HasNamiAsync()) || 
                        !(await CardanoWalletInteropService.IsWalletConnectedAsync()) || 
                        AppStateService is null ||
                        BackendService is null) return;

                    var walletAddress = await CardanoWalletInteropService.GetWalletAddressAsync();

                    if (walletAddress is null) return;
                    AppStateService.CurrentWalletAddress = walletAddress;

                    var buyOrders = await BackendService.GetOpenBuyOrdersByAddressAsync(walletAddress);
                    AppStateService.CurrentOrder = buyOrders?.FirstOrDefault();
                }),
                Task.Run(async () =>
                {
                    if (BackendService is null || AppStateService is null) return;
                    var rate = await BackendService.GetADAPriceAsync();

                    if (rate is null) return;
                    AppStateService.AdaToUsdRate = rate.Cardano.USD;
                    
                    if (BackendService is null || AppStateService is null) return;
                    AppStateService.MarketCap = await BackendService.GetMarketCapAsync(AppStateService.AdaToUsdRate);
                }),
                Task.Run(async () =>
                {
                    if (BackendService is null || AppStateService is null) return;
                    var rate = await BackendService.GetADAPriceAsync();

                    if (rate is null) return;
                    AppStateService.AdaToUsdRate = rate.Cardano.USD;
                    
                    if (AppStateService is null || BackendService is null) return;
                    AppStateService.DailyVolume = await BackendService.GetDailyVolumeAsync(AppStateService.AdaToUsdRate);
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
                    if (CardanoWalletInteropService is null || 
                        !(await CardanoWalletInteropService.HasNamiAsync()) || 
                        !(await CardanoWalletInteropService.IsWalletConnectedAsync()) || 
                        AppStateService is null ||
                        BackendService is null) return;

                    var walletAddress = await CardanoWalletInteropService.GetWalletAddressAsync();

                    if (walletAddress is null) return;
                    
                    AppStateService.UserOrderHistory = await BackendService.GetOrderHistoryAsync(walletAddress);

                    AppStateService.LovelaceBalance = ulong.Parse(await CardanoWalletInteropService.GetBalanceAsync());
                    AppStateService.HoskyBalance = ulong.Parse(await CardanoWalletInteropService.GetBalanceAsync(AppStateService.HoskyUnit));
                }),
                Task.Run(async () =>
                {
                    if (AppStateService is not null && BackendService is not null)
                        AppStateService.OpenOrderRatio = await BackendService.GetOpenOrderRatioAsync();
                }),
                Task.Run(async () =>
                {
                    if (BackendService is null || AppStateService is null) return;
                    var rate = await BackendService.GetADAPriceAsync();

                    if (rate is null) return;
                    AppStateService.AdaToUsdRate = rate.Cardano.USD;
                    
                    if (AppStateService is null || BackendService is null) return;
                    AppStateService.LastExcecutedOrder = await BackendService.GetLastExecutedOrderAsync();

                    if (IsCurrentPriceSet) return;

                    AppStateService.InitialPrice = AppStateService?.LastExcecutedOrder?.Rate ?? 0.000001m;
                    IsCurrentPriceSet = true;
                }),
                Task.Run(async () =>
                {
                    if (BackendService is null || AppStateService is null) return;
                    AppStateService.GlobalOrderHistory  = await BackendService.GetGlobalOrderHistoryAsync();
                }),
                Task.Run(async () =>
                {
                    if (BackendService is null || AppStateService is null) return;
                    AppStateService.TotalStaked = await BackendService.GetTotalStakedAsync();
                    AppStateService.UserStaked =
                        await BackendService.GetUserStakedAsync(AppStateService.CurrentWalletAddress);
                })
            };

            await Task.WhenAll(tasks);
            await Task.Delay(10000);
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
        if (CardanoWalletInteropService is not null && 
            await CardanoWalletInteropService.HasNamiAsync() && 
            await CardanoWalletInteropService.IsWalletConnectedAsync() &&
            AppStateService is not null)
        {
            AppStateService.IsWalletConnected = true;
            WalletAddress = await CardanoWalletInteropService.GetWalletAddressAsync() ?? String.Empty;
            UserIdenticon = await GetIdenticonAsync();
            
            AppStateService.LovelaceBalance = ulong.Parse(await CardanoWalletInteropService.GetBalanceAsync());
            AppStateService.HoskyBalance = ulong.Parse(await CardanoWalletInteropService.GetBalanceAsync(AppStateService.HoskyUnit));
            await InvokeAsync(StateHasChanged);
        }
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
        if (AppStateService != null) AppStateService.PropertyChanged -= OnAppStateChanged;
    }
}