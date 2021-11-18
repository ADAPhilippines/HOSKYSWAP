using Microsoft.JSInterop;

namespace HOSKYSWAP.UI.WASM.Services.JSInterop;

public class HelperInteropService
{
    private readonly IJSRuntime? _jsRuntime;

    public HelperInteropService(IJSRuntime? jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async ValueTask<string> GenerateIdenticonAsync(string str)
    {
        if (_jsRuntime is null) return string.Empty;
        return await _jsRuntime.InvokeAsync<string>("GenerateIdenticon", str);
    }
    
    public async Task ScrollElementIntoView(string selector, string block="start")
    {
        if (_jsRuntime is null) return;
        await _jsRuntime.InvokeVoidAsync("ScrollElementIntoView", selector, block);
    }
}