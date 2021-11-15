using Microsoft.AspNetCore.Components;
using prototype.Models;
using prototype.Services;

namespace prototype.Pages;

public partial class Counter
{
    [Inject] private CardanoWalletInteropService? CardanoWalletInteropService { get; set; }

    private async void OnCounterBtnClicked()
    {
        if (CardanoWalletInteropService is null) return;
        
        if(!await CardanoWalletInteropService.IsWalletConnectedAsync())
            await CardanoWalletInteropService.ConnectWalletAsync();

        Console.WriteLine(await CardanoWalletInteropService.GetBalanceAsync());
        Console.WriteLine(await CardanoWalletInteropService.GetBalanceAsync("a0028f350aaabe0545fdcb56b039bfb08e4bb4d8c4d7c3c7d481c235484f534b59"));

        await CardanoWalletInteropService.SendAssetsAsync(new TxOutput()
        {
            Address = "",
            Amount = new List<Asset>()
            {
                new Asset()
                {
                    Unit = "lovelace",
                    Quantity = 100000000
                }
            }
        });
    }
}