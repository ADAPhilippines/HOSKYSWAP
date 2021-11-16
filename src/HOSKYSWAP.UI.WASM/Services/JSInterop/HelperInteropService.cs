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
}