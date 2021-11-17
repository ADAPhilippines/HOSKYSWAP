using HOSKYSWAP.UI.WASM.Services;
using HOSKYSWAP.UI.WASM.Services.JSInterop;
using Microsoft.AspNetCore.Components;

namespace HOSKYSWAP.UI.WASM.Shared;

public partial class MainLayout
{
    [Inject] protected CardanoWalletInteropService? CardanoWalletInteropService { get; set; }
    [Inject] protected HelperInteropService? HelperInteropService { get; set; }
    [Inject] protected AppStateService? AppStateService { get; set; }
    private string WalletAddress { get; set; } = string.Empty;
    private string UserIdenticon { get; set; } = string.Empty;
    private bool IsNamiWarningDialogVisible { get; set; } = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetAvatarAsync();
            _ = StartDataPolling();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task StartDataPolling()
    {
        while (true)
        {
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
            if (CardanoWalletInteropService is null || await CardanoWalletInteropService.IsWalletConnectedAsync() || AppStateService is null) return;
            AppStateService.IsWalletConnected = await CardanoWalletInteropService.ConnectWalletAsync();
            await SetAvatarAsync();
        }
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetAvatarAsync()
    {
        if (CardanoWalletInteropService is not null && await CardanoWalletInteropService.IsWalletConnectedAsync() && AppStateService is not null)
        {
            AppStateService.IsWalletConnected = true;
            WalletAddress = await CardanoWalletInteropService.GetWalletAddressAsync() ?? String.Empty;
            UserIdenticon = await GetIdenticonAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    private string FormatAddress()
    {
        return WalletAddress.Length > 10 ? 
            $"{WalletAddress.Substring(0,4)}...{WalletAddress[^8..]}"
            : WalletAddress;
    }

    private async Task<string> GetIdenticonAsync()
    {
        if (HelperInteropService is not null)
            return await HelperInteropService.GenerateIdenticonAsync(WalletAddress);
        else
            return string.Empty;
    }
}