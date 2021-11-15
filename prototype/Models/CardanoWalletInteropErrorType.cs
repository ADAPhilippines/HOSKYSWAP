namespace prototype.Models;

public enum CardanoWalletInteropErrorType
{
    WalletNotConnectedError,
    ConnectWalletError,
    CreateTxError,
    SignTxError,
    SubmitTxError
}