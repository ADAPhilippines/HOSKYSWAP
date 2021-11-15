namespace prototype.Models;

public class CardanoWalletInteropError
{
    public CardanoWalletInteropErrorType Type { get; set; }
    public string Message { get; set; } = string.Empty;
}