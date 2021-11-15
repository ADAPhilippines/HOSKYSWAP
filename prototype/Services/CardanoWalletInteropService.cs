using Microsoft.JSInterop;
using prototype.Models;

namespace prototype.Services;

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
        await EnsureErrorHandlerIsSet();
        return await _jsRuntime.InvokeAsync<bool>("CardanoWalletInterop.IsWalletConnectedAsync");
    }

    public async ValueTask<bool> ConnectWalletAsync()
    {
        if (_jsRuntime is null) return false;
        await EnsureErrorHandlerIsSet();
        return await _jsRuntime.InvokeAsync<bool>("CardanoWalletInterop.ConnectWalletAsync");
    }

    public async ValueTask<string> GetBalanceAsync(string unit = "lovelace")
    {
        if (_jsRuntime is null) return "0";
        await EnsureErrorHandlerIsSet();
        return await _jsRuntime.InvokeAsync<string>("CardanoWalletInterop.GetBalanceAsync", unit);
    }

    public async ValueTask SetErrorHandlerAsync()
    {
        if (_jsRuntime is null) return;
        var objRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("CardanoWalletInterop.SetErrorHandlerCallback", objRef, "OnError");
        IsErrorHandlerSet = true;
    }
    
    public async ValueTask<string?> SendAssetsAsync(TxOutput output)
    {
        if (_jsRuntime is null) return null;
        await EnsureErrorHandlerIsSet();
        return await _jsRuntime.InvokeAsync<string?>("CardanoWalletInterop.SendAssetsAsync", output);
    }

    private async ValueTask EnsureErrorHandlerIsSet()
    {
        if (!IsErrorHandlerSet)
            await SetErrorHandlerAsync();
    }

    public event EventHandler<CardanoWalletInteropError>? Error;

    [JSInvokable]
    public void OnError(CardanoWalletInteropError error)
    {
        Error?.Invoke(this, error);
    }
}