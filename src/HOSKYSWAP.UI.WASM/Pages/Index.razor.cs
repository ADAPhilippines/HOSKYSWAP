
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace HOSKYSWAP.UI.WASM.Pages;
public partial class IndexBase : ComponentBase
{
    protected string ToToken { get; set; } = "HOSKY";
    protected string FromToken { get; set; } = "ADA";
    
    protected double FromAmount { get; set; } 
    protected double ToAmount { get; set; }
    protected double PriceAmount { get; set; } = 0.000001;
    protected double BuyRatioWidth { get; set; } = 70;
    protected double SellRatioWidth { get; set; } = 30;


    protected async void OnFromAmountChange(double fromAmount)
    {
        FromAmount = fromAmount;
        ToAmount = FromAmount / PriceAmount;
        await InvokeAsync(StateHasChanged);
    }
    
    protected async void OnSwapClicked(MouseEventArgs args)
    {
        (FromToken, ToToken) = (ToToken, FromToken);
        (FromAmount, ToAmount) = (ToAmount, FromAmount);
        await InvokeAsync(StateHasChanged);
    }
}