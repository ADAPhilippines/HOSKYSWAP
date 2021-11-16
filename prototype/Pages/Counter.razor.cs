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
        const string hosky = "88672eaaf6f5c5fb59ffa5b978016207dbbf769014c6870d31adc4de484f534b59";
        const string swapAddress =
            "addr_test1vqc9ekv93a55g6m59ucceh8v83he3hyve6eawm79dczezsqn8cms9";
        if (CardanoWalletInteropService is null) return;
        
        if(!await CardanoWalletInteropService.IsWalletConnectedAsync())
            await CardanoWalletInteropService.ConnectWalletAsync();
        
        Console.WriteLine(await CardanoWalletInteropService.GetWalletAddressAsync());
        Console.WriteLine(await CardanoWalletInteropService.GetBalanceAsync());
        Console.WriteLine(await CardanoWalletInteropService.GetBalanceAsync(hosky));

        var txId = await CardanoWalletInteropService.SendAssetsAsync(new TxOutput()
        {
            Address = swapAddress,
            Amount = new List<Asset>
            {
                new Asset
                {
                    Unit = hosky,
                    Quantity = 20_000_000
                },
                new Asset
                {
                    Unit = "lovelace",
                    Quantity = 69_4200 + 1_500_000
                }
            }
        },
         JsonSerializer.Serialize(new {rate="0.9", action="sell"}));
        
        var tx = await CardanoWalletInteropService.GetTransactionAsync(txId);
        Console.WriteLine(tx.Hash.ToString());
    }
}