using Blockfrost.Api;
using Blockfrost.Api.Services;
using Blockfrost.Api.Extensions;
using System.Text.Json;
using CardanoSharp.Wallet;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Keys;
using CardanoSharp.Wallet.Utilities;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using System.Net.Http.Headers;
using Blockfrost.Api.Models;

namespace HOSKYSWAP.Server.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly HoskyDbContext _dbContext;
    private string _assetPolicyId { get; set; } = string.Empty;
    private string _assetName { get; set; } = string.Empty;
    private string _walletSeed { get; set; } = string.Empty;
    private string _blockfrostAPIKey { get; set; } = string.Empty;
    private string _blockfrostAPINetwork { get; set; } = string.Empty;
    private ServiceProvider? _blockfrostServiceProvider { get; set; } = null;
    private IEpochsService? _blockfrostEpochService { get; set; } = null;
    private IBlocksService? _blockfrostBlockService { get; set; } = null;
    private ITransactionsService? _blockfrostTransactionsService { get; set; } = null;
    private IAddressesService? _blockfrostAddressService { get; set; } = null;
    private string _walletAddressString { get; set; } = string.Empty;
    private Address? _walletAddress { get; set; } = null;
    private PublicKey? _walletPublicKey { get; set; } = null;
    private PrivateKey? _walletPrivateKey { get; set; } = null;

    public Worker(ILogger<Worker> logger, IConfiguration config, HoskyDbContext dbContext)
    {
        // Load Configurations
        _logger = logger;
        _config = config;
        _dbContext = dbContext;
        _assetPolicyId = _config["AssetPolicyId"];
        _assetName = _config["AssetName"];
        _walletSeed = _config["WalletSeed"];
        _blockfrostAPIKey = _config["BlockfrostAPIKey"];
        _blockfrostAPINetwork = _config["BlockfrostAPINetwork"];
        _blockfrostServiceProvider = new ServiceCollection().AddBlockfrost(_blockfrostAPINetwork, _blockfrostAPIKey).BuildServiceProvider();
        _blockfrostEpochService = _blockfrostServiceProvider.GetService<IEpochsService>();
        _blockfrostBlockService = _blockfrostServiceProvider.GetService<IBlocksService>();
        _blockfrostTransactionsService = _blockfrostServiceProvider.GetService<ITransactionsService>();
        _blockfrostAddressService = _blockfrostServiceProvider.GetService<IAddressesService>();

        // Initialization Procedures
        RestoreWalletFromSeed();

        // Tests
        // MintFakeHoskyAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        Task.Run(async () =>
        {
            await SendTxAsync(
                _walletAddressString,
                10000000 + 694200,
                null,
                (7283, new { action = "buy", rate = "0.000001" })
            );

            await SendTxAsync(
                _walletAddressString,
                10000000 + 694200,
                null,
                (7283, new { action = "buy", rate = "0.000002" })
            );

            await SendTxAsync(
                _walletAddressString,
                1500000 + 694200,
                (_assetPolicyId, _assetName, 100),
                (7283, new { action = "sell", rate = "0.000001" })
            );

            await SendTxAsync(
                _walletAddressString,
                1500000 + 694200,
                (_assetPolicyId, _assetName, 100),
                (7283, new { action = "sell", rate = "0.000003" })
            );
        }).ConfigureAwait(false).GetAwaiter().GetResult();

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_blockfrostEpochService is not null)
                {
                    var networkParams = await _blockfrostEpochService.GetLatestParametersAsync(stoppingToken);
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await SyncNewOrdersAsync(stoppingToken);
                    await Task.Delay(20000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Worker");
            }
        }
    }

    private async Task SyncNewOrdersAsync(CancellationToken stoppingToken)
    {
        if (_blockfrostAddressService is not null && _dbContext.Orders is not null && _blockfrostTransactionsService is not null)
        {
            var utxos = await GetWalletUTXOAsync();
            foreach (var utxo in utxos)
            {
                if (!_dbContext.Orders.Where(o => o.TxHash == utxo.TxHash && o.TxIndex == utxo.TxIndex).Any())
                {
                    var meta = await _blockfrostTransactionsService.GetMetadataAsync(utxo.TxHash, stoppingToken);
                    var hoskyMeta = meta.Where(m => m.Label == "7283").FirstOrDefault();
                    if (hoskyMeta is not null)
                    {
                        var action = hoskyMeta.JsonMetadata.GetProperty("action").GetString();
                        var rate = double.Parse(hoskyMeta.JsonMetadata.GetProperty("rate").GetString() ?? throw new Exception("Rate is null"));
                        
                        switch (action)
                        {
                            case "action":
                                
                                break;
                        }
                    }
                }
            }
        }
    }

    private async Task SendTxAsync(string address, ulong lovelaceToSend, (string, string, ulong)? asset, (int, object)? metadata = null)
    {
        if (_blockfrostBlockService is not null && _blockfrostEpochService is not null && _blockfrostTransactionsService is not null)
        {
            var latestBlock = await _blockfrostBlockService.GetLatestAsync();
            var protocolParam = await _blockfrostEpochService.GetLatestParametersAsync();

            var totalLovelace = 0UL;
            var inputs = new Dictionary<byte[], long>();
            var utxoAssets = new List<(byte[], byte[], ulong)>();

            (await GetWalletUTXOAsync()).ForEach(e =>
            {
                inputs.Add(e.TxHash.HexToByteArray(), e.TxIndex);
                e.Amount.ToList().ForEach(a =>
                {
                    if (a.Unit == "lovelace") totalLovelace += ulong.Parse(a.Quantity);
                    else
                    {
                        var policyIdBytes = a.Unit.Substring(0, 56).HexToByteArray();
                        var assetName = a.Unit.Substring(56).HexToByteArray();
                        var quantity = ulong.Parse(a.Quantity);
                        var searchPredicate = ((byte[], byte[], ulong) e) => e.Item1.SequenceEqual(policyIdBytes) && e.Item2.SequenceEqual(assetName);
                        if (utxoAssets.Any(searchPredicate))
                        {
                            var existingAssetEntry = utxoAssets.First(searchPredicate);
                            var existingAssetIndex = utxoAssets.IndexOf(existingAssetEntry);
                            existingAssetEntry.Item3 += quantity;
                            utxoAssets[existingAssetIndex] = existingAssetEntry;
                        }
                        else
                        {
                            utxoAssets.Add((policyIdBytes, assetName, quantity));
                        }
                    }
                });
            });

            var transactionBody = TransactionBodyBuilder.Create;

            inputs.Keys.ToList().ForEach(k => transactionBody.AddInput(k, (uint)inputs[k]));

            var assetsToSpend = utxoAssets.Where(e =>
                asset.HasValue &&
                e.Item1.SequenceEqual(asset.Value.Item1.HexToByteArray()) &&
                e.Item2.SequenceEqual(asset.Value.Item2.HexToByteArray()) &&
                e.Item3 >= asset.Value.Item3).ToList();

            // Send to Target
            if (asset is not null && assetsToSpend is not null && assetsToSpend.Count >= 1)
            {
                transactionBody.AddOutput(new Address(address), lovelaceToSend, TokenBundleBuilder.Create
                    .AddToken(asset.Value.Item1.HexToByteArray(), asset.Value.Item2.HexToByteArray(), asset.Value.Item3));

                // Create a token change if there are any
                var changeAsset = assetsToSpend.First();
                var changeAssetDelta = changeAsset.Item3 - asset.Value.Item3;
                if (changeAssetDelta > 0)
                {
                    transactionBody.AddOutput(_walletAddress, 2000000, TokenBundleBuilder.Create
                        .AddToken(asset.Value.Item1.HexToByteArray(), asset.Value.Item2.HexToByteArray(), changeAssetDelta));
                    totalLovelace -= 2000000;
                }

                utxoAssets.Remove(changeAsset);

                totalLovelace -= lovelaceToSend;
            }
            else
            {
                transactionBody.AddOutput(new Address(address), lovelaceToSend);
                totalLovelace -= lovelaceToSend;
            }

            // Change
            if (utxoAssets.Count > 0)
            {
                var changeTokens = TokenBundleBuilder.Create;
                utxoAssets.ForEach(e => changeTokens.AddToken(e.Item1, e.Item2, e.Item3));
                transactionBody.AddOutput(_walletAddress, totalLovelace, changeTokens);
            }
            else if (totalLovelace > 1200000)
            {
                transactionBody.AddOutput(_walletAddress, totalLovelace);
            }
            else if (totalLovelace > 0 && totalLovelace < 1200000)
            {
                throw new Exception("Not enough funds...");
            }


            transactionBody
                .SetTtl((uint)latestBlock.Slot + 1000)
                .SetFee(0);

            var witnesses = TransactionWitnessSetBuilder.Create
              .AddVKeyWitness(_walletPublicKey, _walletPrivateKey);

            var transactionBuilder = TransactionBuilder.Create
                .SetBody(transactionBody)
                .SetWitnesses(witnesses);


            if (metadata is not null)
            {
                var auxData = AuxiliaryDataBuilder.Create
                    .AddMetadata(metadata.Value.Item1, metadata.Value.Item2);
                transactionBuilder.SetAuxData(auxData);
            }

            var transaction = transactionBuilder.Build();

            var fee = transaction.CalculateFee((uint)protocolParam.MinFeeA, (uint)protocolParam.MinFeeB);

            transactionBody.SetFee(fee);
            transaction = transactionBuilder.Build();
            transaction.TransactionBody.TransactionOutputs.Last().Value.Coin -= fee;

            var txBytes = transaction.Serialize();
            await SubmitTxBytesAsync(txBytes);
        }
    }

    private async Task<List<AddressUtxoContentResponse>> GetWalletUTXOAsync()
    {
        List<AddressUtxoContentResponse> result = new();
        int page = 1;
        if (_blockfrostAddressService is not null)
        {
            AddressUtxoContentResponseCollection tempRes = new AddressUtxoContentResponseCollection();
            do
            {
                tempRes = await _blockfrostAddressService.GetUtxosAsync(_walletAddressString, page: page++);
                result.AddRange(tempRes);
            } while (!(tempRes.Count < 100));
        }
        return result;
    }

    private async Task MintFakeHoskyAsync()
    {
        if (_blockfrostBlockService is not null && _blockfrostEpochService is not null && _blockfrostTransactionsService is not null)
        {
            var latestBlock = await _blockfrostBlockService.GetLatestAsync();
            var protocolParam = await _blockfrostEpochService.GetLatestParametersAsync();
            // Generate a Key Pair for your new Policy
            var keyPair = KeyPair.GenerateKeyPair();
            var policySkey = keyPair.PrivateKey;
            var policyVkey = keyPair.PublicKey;
            var policyKeyHash = HashUtility.Blake2b244(policyVkey.Key);

            // Create a Policy Script with a type of Script All
            var policyScriptBuilder = ScriptAllBuilder.Create.SetScript(NativeScriptBuilder.Create.SetKeyHash(policyKeyHash));
            var policyScript = policyScriptBuilder.Build();

            // Generate the Policy Id
            var policyId = policyScript.GetPolicyId();

            string tokenName = "HOSKY";
            ulong tokenQuantity = 1_000_000_000_000_000;

            var tokenAsset = TokenBundleBuilder.Create
                .AddToken(policyId, tokenName.ToBytes(), tokenQuantity);

            var transactionBody = TransactionBodyBuilder.Create
                .AddInput("0f91229652cc192a613eebae2c5ebda459b58d29c211b8a228f9b6ed5967b1c0".HexToByteArray(), 0)
                // Sending to Base Address, includes 100 ADA and the Token we are minting
                .AddOutput(new Address("addr_test1vqc9ekv93a55g6m59ucceh8v83he3hyve6eawm79dczezsqn8cms9"), 2000000, tokenAsset)
                .AddOutput(new Address("addr_test1vqc9ekv93a55g6m59ucceh8v83he3hyve6eawm79dczezsqn8cms9"), 998000000)
                .SetMint(tokenAsset)
                .SetTtl((uint)latestBlock.Slot + 1000)
                .SetFee(0);

            var witnesses = TransactionWitnessSetBuilder.Create
                .AddVKeyWitness(policyVkey, policySkey)
                .AddVKeyWitness(_walletPublicKey, _walletPrivateKey)
                .SetNativeScript(policyScriptBuilder);

            var transactionBuilder = TransactionBuilder.Create
                 .SetBody(transactionBody)
                 .SetWitnesses(witnesses);

            var transaction = transactionBuilder.Build();

            var fee = transaction.CalculateFee((uint)protocolParam.MinFeeA, (uint)protocolParam.MinFeeB);

            transactionBody.SetFee(fee);
            transaction = transactionBuilder.Build();
            transaction.TransactionBody.TransactionOutputs.Last().Value.Coin -= fee;

            var txBytes = transaction.Serialize();
            await SubmitTxBytesAsync(txBytes);
        }
    }

    private async Task SubmitTxBytesAsync(byte[] txBytes)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri($"https://cardano-{_blockfrostAPINetwork}.blockfrost.io/api/v0/") };
        httpClient.DefaultRequestHeaders.Add("project_id", _blockfrostAPIKey);
        var byteContent = new ByteArrayContent(txBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");
        var txResponse = await httpClient.PostAsync("tx/submit", byteContent);
        var txId = await txResponse.Content.ReadAsStringAsync();
        _logger.LogInformation($"Tx Submitted: {txId}", DateTimeOffset.Now);
        await IsTxConfirmedAsync(txId);
        _logger.LogInformation($"Tx Confirmed: {txId}", DateTimeOffset.Now);
    }

    async Task<bool> IsTxConfirmedAsync(string txId)
    {
        var result = false;
        if (_blockfrostTransactionsService is not null)
        {

            while (!result)
            {
                try
                {
                    var tx = await _blockfrostTransactionsService.GetAsync(txId.Replace("\"", string.Empty));
                    result = true;
                }
                catch
                {
                    await Task.Delay(10000);
                }
            }
        }
        return result;
    }

    private void RestoreWalletFromSeed()
    {
        var mnemonicService = new MnemonicService();
        var addressService = new AddressService();

        var mnemonicObj = mnemonicService.Restore(_walletSeed, WordLists.English);
        var masterKey = mnemonicObj.GetRootKey();

        var paymentPrv = masterKey.Derive("m/1852'/1815'/0'/0/0");
        var paymentPub = paymentPrv.GetPublicKey(false);

        var stakePrv = masterKey.Derive("m/1852'/1815'/0'/2/0");
        var stakePub = stakePrv.GetPublicKey(false);

        _walletAddress = addressService.GetAddress(
            paymentPub,
            stakePub,
            WalletNetworkType,
            AddressType.Enterprise
        );

        _walletAddressString = _walletAddress.ToString();
        _walletPublicKey = paymentPub;
        _walletPrivateKey = paymentPrv;
    }

    private NetworkType WalletNetworkType => _blockfrostAPINetwork switch
    {
        "mainnet" => NetworkType.Mainnet,
        _ => NetworkType.Testnet
    };
}
