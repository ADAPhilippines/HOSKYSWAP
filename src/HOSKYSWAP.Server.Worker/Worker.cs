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
using Microsoft.EntityFrameworkCore;
using HOSKYSWAP.Common;
using HOSKYSWAP.Data;

namespace HOSKYSWAP.Server.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly HoskyDbContext _dbContext;
    private string _assetPolicyId { get; set; } = string.Empty;
    private string _assetName { get; set; } = string.Empty;
    private string _walletSeed { get; set; } = string.Empty;
    private ulong _swapFee { get; set; } = 694200;
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
        _swapFee = ulong.Parse(_config["SwapFee"]);
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
        // Task.Run(async () =>
        // {
        //     // await SendTxAsync(
        //     //     "addr_test1qrnrqg4s73skqfyyj69mzr7clpe8s7ux9t8z6l55x2f2xuqra34p9pswlrq86nq63hna7p4vkrcrxznqslkta9eqs2nsmlqvnk",
        //     //     353460836,
        //     //     (_assetPolicyId, _assetName, 71666936),
        //     //     null
        //     // );

        //     // await SendTxAsync(
        //     //     _walletAddressString,
        //     //     10000000 + 694200,
        //     //     null,
        //     //     (7283, new { action = "buy", rate = "0.000002" })
        //     // );

        //     // await SendTxAsync(
        //     //     _walletAddressString,
        //     //     1500000 + 694200,
        //     //     (_assetPolicyId, _assetName, 100),
        //     //     (7283, new { action = "sell", rate = "0.000001" })
        //     // );

        //     // await SendTxAsync(
        //     //     _walletAddressString,
        //     //     1500000 + 694200,
        //     //     (_assetPolicyId, _assetName, 100),
        //     //     (7283, new { action = "sell", rate = "0.000003" })
        //     // );
        // }).ConfigureAwait(false).GetAwaiter().GetResult();

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
                    await SyncTxConfirmationsAsync(stoppingToken);
                    await SyncNewOrdersAsync(stoppingToken);
                    await MatchOrdersAsync(stoppingToken);
                    await Task.Delay(20000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Worker");
            }
        }
    }

    private async Task SyncTxConfirmationsAsync(CancellationToken stoppingToken)
    {
        if (_blockfrostAddressService is not null && _dbContext.Orders is not null && _blockfrostTransactionsService is not null)
        {
            var confirmingOrders = _dbContext.Orders.Where(o => o.Status == Status.Confirming).ToList();
            var uniqueTxHashes = confirmingOrders.Select(o => o.ExecuteTxId).Distinct().ToList();

            foreach (var txHash in uniqueTxHashes)
            {
                try
                {
                    var tx = await _blockfrostTransactionsService.GetAsync(txHash);
                    if (tx is not null)
                    {
                        confirmingOrders.ForEach(e => {
                            e.Status = Status.Filled;
                            e.UpdatedAt = DateTime.UtcNow;
                        });
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch
                {
                    continue;
                }
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
                if (_dbContext.Orders.Any(e =>
                    (e.TxHash == utxo.TxHash && e.TxIndexes.Contains(utxo.TxIndex)) ||
                    (e.TxHash == utxo.TxHash && (e.Status == Status.Ignored || e.Status == Status.Error))
                )) continue;

                var txUtxos = await _blockfrostTransactionsService.GetUtxosAsync(utxo.TxHash, stoppingToken);
                var siblingUtxos = utxos.Where(e => e.TxHash == utxo.TxHash);
                if (txUtxos.Inputs.Where(e => e.Address == _walletAddressString).Any())
                {
                    siblingUtxos.Select(e => e.TxIndex);
                    _dbContext.Orders.Add(new()
                    {
                        OwnerAddress = _walletAddressString,
                        TxHash = utxo.TxHash,
                        Action = string.Empty,
                        Rate = 0,
                        Total = 0,
                        Status = Status.Ignored,
                        TxIndexes = siblingUtxos.Select(e => e.TxIndex).ToList(),
                        ExecuteTxId = string.Empty
                    });
                }
                else
                {
                    var meta = await _blockfrostTransactionsService.GetMetadataAsync(utxo.TxHash, stoppingToken);
                    var hoskySwapMeta = meta.Where(m => m.Label == "7283").FirstOrDefault();
                    if (hoskySwapMeta is null)
                    {
                        _logger.LogInformation("No hosky swap meta found for tx {txHash}", utxo.TxHash);
                        _dbContext.Orders.Add(new()
                        {
                            OwnerAddress = txUtxos.Inputs.First().Address,
                            TxHash = utxo.TxHash,
                            Action = string.Empty,
                            Rate = 0,
                            Total = 0,
                            Status = Status.Error,
                            TxIndexes = siblingUtxos.Select(e => e.TxIndex).ToList(),
                            ExecuteTxId = string.Empty
                        });
                    }
                    else
                    {

                        if (hoskySwapMeta.JsonMetadata.TryGetProperty("action", out var actionProp) &&
                            hoskySwapMeta.JsonMetadata.TryGetProperty("rate", out var rateProp) &&
                            rateProp.GetString() != null &&
                            actionProp.GetString() != null)
                        {
                            var action = actionProp.GetString();
                            if (decimal.TryParse(rateProp.GetString(), out var rate) &&
                                action != null &&
                                (action == "buy" || action == "sell"))
                            {
                                var totalQuantity = 0UL;
                                var totalLovelaceQuantity = 0UL;
                                var unit = action == "buy" ? "lovelace" : $"{_assetPolicyId}{_assetName}";

                                siblingUtxos
                                    .Select(e => ulong.Parse(e.Amount.Where(a => a.Unit == unit).First().Quantity))
                                    .ToList().ForEach(e => totalQuantity += e);

                                siblingUtxos
                                    .Select(e => ulong.Parse(e.Amount.Where(a => a.Unit == "lovelace").First().Quantity))
                                    .ToList().ForEach(e => totalLovelaceQuantity += e);

                                if ((action == "buy" && totalQuantity >= 5000000 + 694200) ||
                                    (action == "sell" && totalLovelaceQuantity >= 1500000 + 694200 && totalQuantity * ((decimal)rate * 1000000) >= 4999997))
                                {

                                    if (action == "buy") totalQuantity -= 694200;
                                    _dbContext.Orders.Add(new()
                                    {
                                        OwnerAddress = txUtxos.Inputs.First().Address,
                                        TxHash = utxo.TxHash,
                                        Action = action,
                                        Rate = rate,
                                        Total = totalQuantity,
                                        Status = Status.Open,
                                        TxIndexes = siblingUtxos.Select(e => e.TxIndex).ToList(),
                                        ExecuteTxId = string.Empty
                                    });
                                }
                                else
                                {
                                    _dbContext.Orders.Add(new()
                                    {
                                        OwnerAddress = txUtxos.Inputs.First().Address,
                                        TxHash = utxo.TxHash,
                                        Action = action,
                                        Rate = rate,
                                        Total = totalQuantity,
                                        Status = Status.Error,
                                        TxIndexes = siblingUtxos.Select(e => e.TxIndex).ToList(),
                                        ExecuteTxId = string.Empty
                                    });
                                }
                            }
                            else
                            {
                                _dbContext.Orders.Add(new()
                                {
                                    OwnerAddress = txUtxos.Inputs.First().Address,
                                    TxHash = utxo.TxHash,
                                    Action = string.Empty,
                                    Rate = 0,
                                    Total = 0,
                                    Status = Status.Error,
                                    TxIndexes = siblingUtxos.Select(e => e.TxIndex).ToList(),
                                    ExecuteTxId = string.Empty
                                });
                            }
                        }
                        else
                        {
                            _dbContext.Orders.Add(new()
                            {
                                OwnerAddress = txUtxos.Inputs.First().Address,
                                TxHash = utxo.TxHash,
                                Action = string.Empty,
                                Rate = 0,
                                Total = 0,
                                Status = Status.Error,
                                TxIndexes = siblingUtxos.Select(e => e.TxIndex).ToList(),
                                ExecuteTxId = string.Empty
                            });
                        }
                    }
                }
                await _dbContext.SaveChangesAsync();
            }
        }
    }

    private async Task MatchOrdersAsync(CancellationToken stoppingToken)
    {
        if (_dbContext.Orders is not null)
        {
            var openOrders = await _dbContext.Orders
                .Where(e => e.Status == Status.Open)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            var txOrderData = new Dictionary<Order, Order>();
            var consumedOrders = new List<Order>();
            byte[]? txBytes = null;
            foreach (var order in openOrders)
            {
                if (consumedOrders.Any(e => e == order)) continue;

                if (order.Action == "buy")
                {
                    var buyOrder = order;

                    var matchOrder = openOrders.Where(e =>
                        e.Action == "sell" &&
                        e.Rate == buyOrder.Rate && ulong.Parse((buyOrder.Total - (ulong)(e.Total * e.Rate * 1000000)).ToString().Replace("-", string.Empty)) < 3 &&
                        !consumedOrders.Any(e1 => e == e1)
                    ).FirstOrDefault();

                    if (buyOrder is not null && matchOrder is not null)
                    {
                        txOrderData.Add(buyOrder, matchOrder);
                        consumedOrders.Add(buyOrder);
                        consumedOrders.Add(matchOrder);
                    }
                }
                else if (order.Action == "sell")
                {
                    var sellOrder = order;
                    var matchOrder = openOrders.Where(e =>
                        e.Action == "buy" &&
                        e.Rate == sellOrder.Rate &&
                        ulong.Parse((e.Total - (ulong)(sellOrder.Total * sellOrder.Rate * 1000000)).ToString().Replace("-", string.Empty)) < 3 &&
                        !consumedOrders.Any(e1 => e == e1)).FirstOrDefault();

                    if (sellOrder is not null && matchOrder is not null)
                    {
                        txOrderData.Add(sellOrder, matchOrder);
                        consumedOrders.Add(sellOrder);
                        consumedOrders.Add(matchOrder);
                    }
                }

                if (consumedOrders.Count <= 0) continue;
                var tempTxBytes = await BuildTxFromOrdersAsync(txOrderData);
                // check if tempTxBytes length is more than 16kb
                if (tempTxBytes is not null && tempTxBytes.Length > 16 * 1024) break;
                else txBytes = tempTxBytes;
            }

            if (txBytes is not null)
            {
                var txId = await SubmitTxBytesAsync(txBytes);
                if (txId.Length == 64)
                {
                    consumedOrders.ForEach(e =>
                    {
                        e.ExecuteTxId = txId;
                        e.Status = Status.Confirming;
                        e.UpdatedAt = DateTime.UtcNow;
                    });
                    await _dbContext.SaveChangesAsync();
                }
            }
        }
    }

    private async Task<byte[]?> BuildTxFromOrdersAsync(Dictionary<Order, Order> txOrderData)
    {
        if (_blockfrostBlockService is not null && _blockfrostEpochService is not null && _blockfrostTransactionsService is not null)
        {
            var latestBlock = await _blockfrostBlockService.GetLatestAsync();
            var protocolParam = await _blockfrostEpochService.GetLatestParametersAsync();


            var transactionBody = TransactionBodyBuilder.Create;
            var totalFee = (ulong)txOrderData.Count * 2 * 694200UL;

            foreach (var order in txOrderData.Keys)
            {
                var keyOrder = order;
                var valueOrder = txOrderData[order];

                foreach (var idx in keyOrder.TxIndexes)
                {
                    transactionBody.AddInput(keyOrder.TxHash.HexToByteArray(), (uint)idx);
                }

                foreach (var idx in valueOrder.TxIndexes)
                {
                    transactionBody.AddInput(valueOrder.TxHash.HexToByteArray(), (uint)idx);
                }

                if (keyOrder.Action == "buy")
                {
                    var keyTargetAmount = txOrderData[keyOrder].Total;
                    var valueTargetAmount = keyOrder.Total;

                    transactionBody.AddOutput(new Address(keyOrder.OwnerAddress), 1500000,
                        TokenBundleBuilder.Create.AddToken(_assetPolicyId.HexToByteArray(), _assetName.HexToByteArray(), keyTargetAmount));

                    transactionBody.AddOutput(new Address(txOrderData[keyOrder].OwnerAddress), valueTargetAmount);
                }
                else if (keyOrder.Action == "sell")
                {
                    var keyTargetAmount = txOrderData[keyOrder].Total;
                    var valueTargetAmount = keyOrder.Total;

                    transactionBody.AddOutput(new Address(keyOrder.OwnerAddress), keyTargetAmount);

                    transactionBody.AddOutput(new Address(txOrderData[keyOrder].OwnerAddress), 1500000,
                        TokenBundleBuilder.Create.AddToken(_assetPolicyId.HexToByteArray(), _assetName.HexToByteArray(), valueTargetAmount));
                }
            }

            transactionBody.AddOutput(_walletAddress, totalFee);

            transactionBody
                .SetTtl((uint)latestBlock.Slot + 1000)
                .SetFee(0);

            var witnesses = TransactionWitnessSetBuilder.Create
                .AddVKeyWitness(_walletPublicKey, _walletPrivateKey);

            var transactionBuilder = TransactionBuilder.Create
                .SetBody(transactionBody)
                .SetWitnesses(witnesses);

            var transaction = transactionBuilder.Build();

            var fee = transaction.CalculateFee((uint)protocolParam.MinFeeA, (uint)protocolParam.MinFeeB);

            transactionBody.SetFee(fee);
            transaction = transactionBuilder.Build();
            transaction.TransactionBody.TransactionOutputs.Last().Value.Coin -= fee;

            return transaction.Serialize();
        }

        return null;
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
        }
    }

    private async Task<string> SubmitTxBytesAsync(byte[] txBytes)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri($"https://cardano-{_blockfrostAPINetwork}.blockfrost.io/api/v0/") };
        httpClient.DefaultRequestHeaders.Add("project_id", _blockfrostAPIKey);
        var byteContent = new ByteArrayContent(txBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");
        var txResponse = await httpClient.PostAsync("tx/submit", byteContent);
        var txId = await txResponse.Content.ReadAsStringAsync();
        return txId.Replace("\"", string.Empty);
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
