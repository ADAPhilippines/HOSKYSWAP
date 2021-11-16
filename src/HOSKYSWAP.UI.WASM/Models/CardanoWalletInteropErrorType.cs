namespace HOSKYSWAP.UI.WASM.Models;

public enum CardanoWalletInteropErrorType
{
    WalletNotConnectedError,
    ConnectWalletError,
    CreateTxError,
    SignTxError,
    SubmitTxError
}