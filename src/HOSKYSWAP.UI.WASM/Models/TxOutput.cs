namespace HOSKYSWAP.UI.WASM.Models;

public class TxOutput
{
    public string Address { get; init; } = string.Empty;
    public IEnumerable<Asset>? Amount { get; init; }
}