using System.Text.Json;
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
            Address = "addr1qxr04lmt3m06pjf4w4xer9jrh54huql75usxla8temajs7jp77sg0d3zl7fg84na9lkrteqfuhvraxgrc2y83yz4me7s28rzae",
            Amount = new List<Asset>
            {
                new Asset
                {
                    Unit = "a0028f350aaabe0545fdcb56b039bfb08e4bb4d8c4d7c3c7d481c235484f534b59",
                    Quantity = 10
                },
                new Asset
                {
                    Unit = "lovelace",
                    Quantity = 69_4200 + 1_500_000
                }
            }
        },
         JsonSerializer.Serialize(new {rate="0.0000001", action="sell"}));
    }
}