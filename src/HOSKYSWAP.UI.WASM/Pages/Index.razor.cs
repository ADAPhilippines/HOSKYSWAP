using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace HOSKYSWAP.UI.WASM.Pages;

public partial class IndexBase : ComponentBase
{
    [Inject] private ILocalStorageService? LocalStorage { get; set; }
    protected string ToToken { get; set; } = "HOSKY";
    protected string FromToken { get; set; } = "ADA";
    protected decimal FromAmount { get; set; } = 5m;
    protected decimal ToAmount { get; set; }
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Check here
            if (LocalStorage != null)
            {
                var didRead = await LocalStorage.GetItemAsync<bool?>(DidReadDialogStorageKey);
                if (didRead == false || didRead == null)
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
        await InvokeAsync(StateHasChanged);
    }

    protected async void OnCancelSwapClicked(MouseEventArgs args)
    {
        HasUnfilledOrder = false;
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
}