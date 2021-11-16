using Microsoft.JSInterop;
using HOSKYSWAP.UI.WASM.Models;

namespace HOSKYSWAP.UI.WASM.Services.JSInterop;

public class CardanoWalletInteropService
{
    private readonly IJSRuntime? _jsRuntime;
    private bool IsErrorHandlerSet { get; set; }

    public CardanoWalletInteropService(IJSRuntime? jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask<bool> IsWalletConnectedAsync()
    {
        if (_jsRuntime is null) return false;
        await EnsureParametersAreSet();
        return await _jsRuntime.InvokeAsync<bool>("CardanoWalletInterop.IsWalletConnectedAsync");
    }

    public async ValueTask<bool> ConnectWalletAsync()
    {
        if (_jsRuntime is null) return false;
        await EnsureParametersAreSet();
        return await _jsRuntime.InvokeAsync<bool>("CardanoWalletInterop.ConnectWalletAsync");
    }

    public async ValueTask<string> GetBalanceAsync(string unit = "lovelace")
    {
        if (_jsRuntime is null) return "0";
        await EnsureParametersAreSet();
        return await _jsRuntime.InvokeAsync<string>("CardanoWalletInterop.GetBalanceAsync", unit);
    }
    
    public async ValueTask<string?> SendAssetsAsync(TxOutput output, string metadata = "")
    {
        if (_jsRuntime is null) return null;
        await EnsureParametersAreSet();
        return await _jsRuntime.InvokeAsync<string?>("CardanoWalletInterop.SendAssetsAsync", output, metadata);
    }
    
    public async ValueTask<string?> GetWalletAddressAsync()
    {
        if (_jsRuntime is null) return null;
        await EnsureParametersAreSet();
        return await _jsRuntime.InvokeAsync<string?>("CardanoWalletInterop.GetWalletAddressAsync");
    }
    
    public async ValueTask<Transaction?> GetTransactionAsync(string hash)
    {
        if (_jsRuntime is null) return null;
        await EnsureParametersAreSet();
        return await _jsRuntime
            .InvokeAsync<Transaction?>("CardanoWalletInterop.GetTransactionAsync", hash);
    }

    private async ValueTask EnsureParametersAreSet()
    {
        if (!IsErrorHandlerSet)
            await SetErrorHandlerAsync();
    }
    
    public async ValueTask SetErrorHandlerAsync()
    {
        if (_jsRuntime is null) return;
        var objRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("CardanoWalletInterop.SetErrorHandlerCallback", objRef, "OnError");
        IsErrorHandlerSet = true;
    }

    public event EventHandler<CardanoWalletInteropError>? Error;

    [JSInvokable]
    public void OnError(CardanoWalletInteropError error)
    {
        Error?.Invoke(this, error);
    }
}