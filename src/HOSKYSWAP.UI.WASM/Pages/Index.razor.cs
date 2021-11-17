using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using HOSKYSWAP.UI.WASM.Services.JSInterop;
using Blazored.LocalStorage;
using HOSKYSWAP.Common;
using HOSKYSWAP.UI.WASM.Models;
using HOSKYSWAP.UI.WASM.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace HOSKYSWAP.UI.WASM.Pages;

public partial class IndexBase : ComponentBase
{
    [Inject] protected CardanoWalletInteropService? CardanoWalletInteropService { get; set; }
    [Inject] protected ILocalStorageService? LocalStorage { get; set; }
    [Inject] protected AppStateService? AppStateService { get; set; }
    protected string ToToken { get; set; } = "HOSKY";
    protected string FromToken { get; set; } = "ADA";
    protected decimal FromAmount { get; set; } = 5m;
    protected decimal ToAmount { get; set; } = 5000000;
    protected decimal PriceAmount { get; set; } = 0.000001m;
    protected double BuyRatioWidth { get; set; } = 70;
    protected double SellRatioWidth { get; set; } = 30;
    protected bool DisplayToError { get; set; }
    protected bool DisplayFromError { get; set; }
    protected string MinimumADAErrorMessage = "Minimum $ADA to swap is 5 $ADA.";
    protected string WholeNumberADAErrorMessage = "Total $ADA must be a whole number.";
    protected string WholeNumberHOSKYErrorMessage = "Total $HOSKY must be a whole number.";
    protected string ToErrorMessage = string.Empty;
    protected string FromErrorMessage = string.Empty;
    protected bool HasUnfilledOrder { get; set; }
    protected DialogOptions DialogOptions = new() {FullWidth = true, DisableBackdropClick = true};
    protected bool IsDialogVisible { get; set; }
    protected List<Order> OrderHistory { get; set; } = new List<Order>();
    protected List<Order> OpenSellOrders { get; set; } = new List<Order>();
    protected List<Order> OpenBuyOrders { get; set; } = new List<Order>();
    private string DidReadDialogStorageKey = "DidReadDialog";
    private string SwapAddress { get; set; } = "addr_test1vqc9ekv93a55g6m59ucceh8v83he3hyve6eawm79dczezsqn8cms9";
    private string HoskyUnit { get; set; } = "88672eaaf6f5c5fb59ffa5b978016207dbbf769014c6870d31adc4de484f534b59";
    protected BackendService BackendService { get; set; } = new BackendService();
    protected bool IsDisclaimerDialogVisible { get; set; }
    protected bool IsGeneralDialogVisible { get; set; }
    protected bool IsGeneralActionVisible { get; set; }
    protected string GeneralDialogMessage { get; set; } = string.Empty;
    private HttpClient HttpClient { get; set; } = new HttpClient();

    protected override void OnInitialized()
    {
        if (AppStateService != null) AppStateService.PropertyChanged += OnAppStateChanged;
        base.OnInitialized();
    }

    protected void HideGeneralDialog()
    {
        IsGeneralActionVisible = false;
        IsGeneralDialogVisible = false;
    }

    private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e) => StateHasChanged();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (LocalStorage is not null)
            {
                var didRead = await LocalStorage.GetItemAsync<bool?>(DidReadDialogStorageKey);
                if (didRead is false or null) IsDisclaimerDialogVisible = true;
            }

            if (CardanoWalletInteropService is not null)
            {
                CardanoWalletInteropService.Error += OnWalletError;
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private void OnWalletError(object? sender, CardanoWalletInteropError e)
    {
        switch (e.Type)
        {
            case CardanoWalletInteropErrorType.SignTxError:
                IsGeneralDialogVisible = true;
                IsGeneralActionVisible = true;
                GeneralDialogMessage = "Unable to sign transaction...";
                StateHasChanged();
                break;
            case CardanoWalletInteropErrorType.CreateTxError:
                IsGeneralDialogVisible = true;
                IsGeneralActionVisible = true;
                GeneralDialogMessage = "Not enough funds, please check your wallet...";
                StateHasChanged();
                break;
            default:
                break;
        }
    }

    protected async void OnFromAmountChange(decimal fromAmount)
    {
        FromAmount = fromAmount;
        if (fromAmount > 0 && PriceAmount > 0)
        {
            if (FromToken == "ADA")
            {
                ToAmount = (ulong)(FromAmount / PriceAmount);
            }
            else
            {
                ToAmount = FromAmount * PriceAmount;
                var floor = Math.Floor(ToAmount);
                var ceil = Math.Ceiling(ToAmount);

                if (Math.Abs(ToAmount - floor) <= 0.000003m) ToAmount = floor;
                if (Math.Abs(ToAmount - ceil) <= 0.000003m) ToAmount = ceil;
            }
        }
        else
        {
            ToAmount = 0;
        }

        ValidateForm();
        await InvokeAsync(StateHasChanged);
    }

    protected async void OnToAmountChange(decimal toAmount)
    {
        ToAmount = toAmount;
        if (ToAmount > 0 && PriceAmount > 0)
        {
            if (FromToken == "ADA")
                FromAmount = ToAmount * PriceAmount;
            else
                FromAmount = (ulong)(ToAmount / PriceAmount);
        }
        else
        {
            FromAmount = 0;
        }

        ValidateForm();
        await InvokeAsync(StateHasChanged);
    }

    protected async void OnPriceAmountChange(decimal priceAmount)
    {
        PriceAmount = priceAmount;
        if (PriceAmount > 0 && FromAmount > 0)
        {
            OnFromAmountChange(FromAmount);
        }
        else
        {
            ToAmount = 0;
        }

        await InvokeAsync(StateHasChanged);
    }


    protected async void OnToTokenChanged(string toToken)
    {
        ToToken = toToken;
        FromToken = toToken == "ADA" ? "HOSKY" : "ADA";
        OnToAmountChange(ToAmount);
        await InvokeAsync(StateHasChanged);
    }

    protected async void OnFromTokenChanged(string fromToken)
    {
        FromToken = fromToken;
        ToToken = fromToken == "ADA" ? "HOSKY" : "ADA";
        OnFromAmountChange(FromAmount);
        await InvokeAsync(StateHasChanged);
    }

    protected async void OnSwapClicked(MouseEventArgs args)
    {
        (FromToken, ToToken) = (ToToken, FromToken);
        (FromAmount, ToAmount) = (ToAmount, FromAmount);
        OnFromAmountChange(FromAmount);
        ValidateForm();
        await InvokeAsync(StateHasChanged);
    }

    private void ValidateForm()
    {
        if (FromAmount < 5 && FromToken is "ADA")
        {
            DisplayFromError = true;
            FromErrorMessage = MinimumADAErrorMessage;
        }
        else if (FromAmount % 1 > 0)
        {
            DisplayFromError = true;
            FromErrorMessage = FromToken switch
            {
                "ADA" => WholeNumberADAErrorMessage,
                "HOSKY" => WholeNumberHOSKYErrorMessage,
                _ => FromErrorMessage
            };
        }
        else
        {
            DisplayFromError = false;
            FromErrorMessage = string.Empty;
        }

        if (ToAmount < 5 && ToToken == "ADA")
        {
            DisplayToError = true;
            ToErrorMessage = MinimumADAErrorMessage;
        }
        else if (ToAmount % 1 > 0)
        {
            DisplayToError = true;
            ToErrorMessage = ToToken switch
            {
                "ADA" => WholeNumberADAErrorMessage,
                "HOSKY" => WholeNumberHOSKYErrorMessage,
                _ => ToErrorMessage
            };
        }
        else
        {
            DisplayToError = false;
        }
    }

    protected async void OnSubmitSwapClicked(MouseEventArgs args)
    {
        IsGeneralDialogVisible = true;
        GeneralDialogMessage = "Submitting your order...";
        await InvokeAsync(StateHasChanged);
        
        var txId = FromToken switch
        {
            "ADA" => await BuyHoskyAsync(),
            "HOSKY" => await SellHoskyAsync(),
            _ => string.Empty
        };

        if (txId is not null && CardanoWalletInteropService is not null)
        {
            GeneralDialogMessage = $"Waiting for confirmation, TxID: {txId}";
            await InvokeAsync(StateHasChanged);
            var tx = await CardanoWalletInteropService.GetTransactionAsync(txId);
            if (tx is not null)
            {
                HasUnfilledOrder = true;
                IsGeneralDialogVisible = false;
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                await SomethingWentWrongAsync();
            }
        }
    }

    protected async void OnCancelSwapClicked(MouseEventArgs args)
    {
        HasUnfilledOrder = false;

        await CancelOrderAsync();
        
        await InvokeAsync(StateHasChanged);
    }

    protected bool GetShouldDisableSwapButton()
    {
        return (DisplayFromError || DisplayToError) || (AppStateService is not null && !AppStateService.IsWalletConnected);
    }

    protected async void OnCloseDialog(MouseEventArgs args)
    {
        await Task.Delay(200);
        IsDisclaimerDialogVisible = false;
        if (LocalStorage != null)
        {
            await LocalStorage.SetItemAsync(DidReadDialogStorageKey, true);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task<string?> BuyHoskyAsync()
    {
        if (CardanoWalletInteropService is null) return null;
        try
        {
            var txId = await CardanoWalletInteropService.SendAssetsAsync(new TxOutput
                {
                    Address = SwapAddress,
                    Amount = new List<Asset>
                    {
                        new Asset
                        {
                            Unit = "lovelace",
                            Quantity = (ulong)(FromAmount * 1_000_000) + 69_4200 
                        }
                    }
                },
                JsonSerializer.Serialize(new
                    {
                        rate = PriceAmount.ToString(CultureInfo.InvariantCulture), 
                        action = "buy"
                    }
                ));

            return txId;
        }
        catch
        {
            await SomethingWentWrongAsync();
            return null;
        }
    }

    private async Task SomethingWentWrongAsync()
    {
        IsGeneralDialogVisible = true;
        IsGeneralActionVisible = true;
        GeneralDialogMessage = "Something went wrong, please try again.";
        await InvokeAsync(StateHasChanged);
    }

    private async Task<string?> SellHoskyAsync()
    {
        if (CardanoWalletInteropService is null) return null;

        try
        {
            var txId = await CardanoWalletInteropService.SendAssetsAsync(new TxOutput()
                {
                    Address = SwapAddress,
                    Amount = new List<Asset>
                    {
                        new Asset
                        {
                            Unit = HoskyUnit,
                            Quantity = (ulong) FromAmount
                        },
                        new Asset
                        {
                            Unit = "lovelace",
                            Quantity = 69_4200 + 1_500_000
                        }
                    }
                },
                JsonSerializer.Serialize(new
                {
                    rate = PriceAmount.ToString(CultureInfo.InvariantCulture),
                    action = "sell"
                }));

            return txId;
        }
        catch
        {
            await SomethingWentWrongAsync();
            return null;
        }
    }
    
    private async Task CancelOrderAsync()
    {
        if (CardanoWalletInteropService is null) return;
        var txId = await CardanoWalletInteropService.SendAssetsAsync(new TxOutput()
        {
            Address = SwapAddress,
            Amount = new List<Asset>
            {
                new Asset
                {
                    Unit = "lovelace",
                    Quantity = 69_4200 + 1_000_000
                }
            }
        },
        JsonSerializer.Serialize(new
        {
            action = "cancel"
        }));
    }
}