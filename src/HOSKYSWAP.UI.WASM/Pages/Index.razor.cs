using System.Globalization;
using System.Text.Json;
using HOSKYSWAP.UI.WASM.Services.JSInterop;
using Blazored.LocalStorage;
using HOSKYSWAP.UI.WASM.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace HOSKYSWAP.UI.WASM.Pages;

public partial class IndexBase : ComponentBase
{
    [Inject] private CardanoWalletInteropService? CardanoWalletInteropService { get; set; }
    [Inject] private ILocalStorageService? LocalStorage { get; set; }
    protected string ToToken { get; set; } = "HOSKY";
    protected string FromToken { get; set; } = "ADA";
    protected decimal FromAmount { get; set; } = 5m;
    protected decimal ToAmount { get; set; } = 5000000;
    protected decimal PriceAmount { get; set; } = 0.000001m;
    protected double BuyRatioWidth { get; set; } = 70;
    protected double SellRatioWidth { get; set; } = 30;
    protected bool DisplayToError { get; set; }
    protected bool DisplayFromError { get; set; }
    protected string MinimumADAErrorMessage = "Minimum $ADA to swap is 5 $ADA";
    protected bool HasUnfilledOrder { get; set; }
    protected DialogOptions DialogOptions = new() {FullWidth = true, DisableBackdropClick = true};
    protected bool IsDialogVisible { get; set; }
    private string DidReadDialogStorageKey = "DidReadDialog";
    private string SwapAddress { get; set; } = "addr_test1vqc9ekv93a55g6m59ucceh8v83he3hyve6eawm79dczezsqn8cms9";
    private string HoskyUnit { get; set; } = "88672eaaf6f5c5fb59ffa5b978016207dbbf769014c6870d31adc4de484f534b59";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Check here
            if (LocalStorage is not null)
            {
                var didRead = await LocalStorage.GetItemAsync<bool?>(DidReadDialogStorageKey);
                if (didRead is false or null)
                {
                    IsDialogVisible = true;
                }
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    protected async void OnFromAmountChange(decimal fromAmount)
    {
        FromAmount = fromAmount;
        if (fromAmount > 0 && PriceAmount > 0)
        {
            if (FromToken == "ADA")
            {
                ToAmount = Math.Round(FromAmount / PriceAmount, MidpointRounding.ToZero);
            }
            else
            {
                ToAmount = FromAmount * PriceAmount;
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
                FromAmount = Math.Round(ToAmount / PriceAmount, MidpointRounding.ToZero);
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
        ValidateForm();
        await InvokeAsync(StateHasChanged);
    }

    private void ValidateForm()
    {
        if (FromAmount < 5 && FromToken == "ADA")
        {
            DisplayFromError = true;
        }
        else
        {
            DisplayFromError = false;
        }

        if (ToAmount < 5 && ToToken == "ADA")
        {
            DisplayToError = true;
        }
        else
        {
            DisplayToError = false;
        }
    }

    protected async void OnSubmitSwapClicked(MouseEventArgs args)
    {
        HasUnfilledOrder = true;

        switch (FromToken)
        {
            case "ADA":
                await BuyHoskyAsync();
                break;
            case "HOSKY":
                await SellHoskyAsync();
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    protected async void OnCancelSwapClicked(MouseEventArgs args)
    {
        HasUnfilledOrder = false;

        await CancelOrderAsync();
        
        await InvokeAsync(StateHasChanged);
    }

    protected bool GetShouldDisableSwapButton()
    {
        return DisplayFromError || DisplayToError;
    }

    protected async void OnCloseDialog(MouseEventArgs args)
    {
        await Task.Delay(200);
        IsDialogVisible = false;
        if (LocalStorage != null)
        {
            await LocalStorage.SetItemAsync(DidReadDialogStorageKey, true);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task BuyHoskyAsync()
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
                    Quantity = (ulong) Math.Floor(FromAmount * 1_000_000) + 69_4200 
                }
            }
        },
        JsonSerializer.Serialize(new
        {
            rate = PriceAmount.ToString(CultureInfo.InvariantCulture), 
            action = "buy"
        }));
    }

    private async Task SellHoskyAsync()
    {
        if (CardanoWalletInteropService is null) return;
        var txId = await CardanoWalletInteropService.SendAssetsAsync(new TxOutput()
        {
            Address = SwapAddress,
            Amount = new List<Asset>
            {
                new Asset
                {
                    Unit = HoskyUnit,
                    Quantity = (ulong) Math.Floor(FromAmount)
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