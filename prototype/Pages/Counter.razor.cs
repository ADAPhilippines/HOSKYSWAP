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
            "addr_test1qph75avrwfstny4hall2ufw2q7w3znpy9qrn4tjve348vdf3sd5n2afpnvv9kuc7ga4gnrurvl99vdj4dk30u2wwjzcq8dhr45";
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
        
        var tx = await CardanoWalletInteropService.GetTransactionAsync(txId);
        Console.WriteLine(tx.Hash.ToString());
    }
}