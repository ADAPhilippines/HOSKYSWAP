import CardanoWasmLoader from "./Helpers/CardanoWasmLoader";
import IDotNetObjectRef from "./Interfaces/IDotNetObjectRef";
import CardanoWalletInteropErrorType from "./Enums/CardanoWalletInteropErrorType";
import CardanoWalletInteropError from "./Types/CardanoWalletInteropError";
import {min_fee, Transaction, TransactionUnspentOutput} from "@emurgo/cardano-serialization-lib-browser";
import {Buffer} from "Buffer";
import Helper from "./Helpers/Helper";
import TxOutput from "./Types/TxOutput";
import ProtocolParameters from "./Types/ProtocolParameters";
import Block from "./Types/Block";
import Tx from "./Types/Tx";

class CardanoWalletInterop {
    private objectRef: IDotNetObjectRef | null = null;
    private errorCallbackName: string = "OnError";
    private hoskySwapServerUrl: string = "https://hoskyswap-r9dc3.ondigitalocean.app";

    public async IsWalletConnectedAsync(): Promise<boolean | null> {
        if (window.cardano) {
            return await window.cardano.isEnabled();
        } else {
            let err: CardanoWalletInteropError = {
                type: CardanoWalletInteropErrorType.noWalletError,
                message: "No compatible wallet detected!"
            }
            await this.ThrowErrorAsync(err);
            return false;
        }
    }

    public async ConnectWalletAsync(): Promise<boolean> {
        let result = false;
        try {
            result = await window.cardano.enable();
        } catch (e: any) {
            console.error("Connect Wallet Error: ", e);
            let err: CardanoWalletInteropError = {
                type: CardanoWalletInteropErrorType.connectWalletError,
                message: "Failed to connect to a compatible wallet."
            }
            await this.ThrowErrorAsync(err);
        }
        return result;
    }

    public async GetWalletAddressAsync(): Promise<string> {
        await CardanoWalletInterop.EnsureCardanoWasmLoadedAsync();
        let result = "";
        try {
            //handle change
            const addressHex = (await window.cardano.getUsedAddresses())[0];
            const addressBuffer = Buffer.from(addressHex, "hex");
            const address = CardanoWasmLoader.Cardano.Address.from_bytes(addressBuffer);
            return address.to_bech32();
        } catch (e: any) {
            console.error("Error in obtaining wallet address: ", e);
            let err: CardanoWalletInteropError = {
                type: CardanoWalletInteropErrorType.connectWalletError,
                message: "Error in obtaining wallet address"
            }
            await this.ThrowErrorAsync(err);
        }
        return result;
    }

    public async GetBalanceAsync(unit: string = "lovelace"): Promise<string> {
        await CardanoWalletInterop.EnsureCardanoWasmLoadedAsync();

        const utxosHex = await window.cardano.getUtxos();
        const utxos: TransactionUnspentOutput[] = utxosHex
            .map(utxo => CardanoWasmLoader.Cardano.TransactionUnspentOutput.from_bytes(Buffer.from(utxo, "hex")));

        const zeroVal = CardanoWasmLoader.Cardano.BigNum.from_str("0");
        let totalValue = CardanoWasmLoader.Cardano.Value.new(zeroVal);
        utxos.forEach(utxo => {
            totalValue = totalValue.checked_add(utxo.output().amount());
        });

        if (unit === "lovelace") {
            return totalValue.coin().to_str();
        } else {
            const scriptHash = CardanoWasmLoader.Cardano.ScriptHash.from_bytes(Helper.HexToBytes(unit.slice(0, 56)));
            const assetNameHash = CardanoWasmLoader.Cardano.AssetName.new(Helper.HexToBytes(unit.slice(56)));
            const policyAssets = totalValue.multiasset()?.get(scriptHash);
            const assetQuantity = policyAssets?.get(assetNameHash);
            return assetQuantity?.to_str() ?? "0";
        }
    }

    private static async EnsureCardanoWasmLoadedAsync() {
        if (CardanoWasmLoader.Cardano == null) {
            await CardanoWasmLoader.Load();
        }
    }

    public async SendAssetsAsync(output: TxOutput, metadata: string): Promise<string | null> {
        let result: string | null = null;
        const transaction = await this.CreateNormalTx(output, metadata);
        if (transaction !== null) {
            const signedTx = await this.signTxAsync(transaction);
            if (signedTx != null) {
                result = await this.SubmitTxAsync(signedTx);
            }
        }
        return result;
    }

    private async signTxAsync(transaction: Transaction): Promise<Transaction | null> {
        await CardanoWalletInterop.EnsureCardanoWasmLoadedAsync();
        let result: Transaction | null = null;
        try {
            const transactionHex = Buffer.from(transaction.to_bytes()).toString("hex");
            const witnesses = await window.cardano.signTx(transactionHex);
            
            const txWitnesses = transaction.witness_set();
            const txVkeys = txWitnesses.vkeys();
            const txScripts = txWitnesses.native_scripts();

            const addWitnesses = CardanoWasmLoader.Cardano.TransactionWitnessSet.from_bytes(
                Buffer.from(witnesses, "hex")
            );

            const addVkeys = addWitnesses.vkeys();
            const addScripts = addWitnesses.native_scripts();

            const totalVkeys = CardanoWasmLoader.Cardano.Vkeywitnesses.new();
            const totalScripts = CardanoWasmLoader.Cardano.NativeScripts.new();

            if (txVkeys) {
                for (let i = 0; i < txVkeys.len(); i++) {
                    totalVkeys.add(txVkeys.get(i));
                }
            }
            if (txScripts) {
                for (let i = 0; i < txScripts.len(); i++) {
                    totalScripts.add(txScripts.get(i));
                }
            }
            if (addVkeys) {
                for (let i = 0; i < addVkeys.len(); i++) {
                    totalVkeys.add(addVkeys.get(i));
                }
            }
            if (addScripts) {
                for (let i = 0; i < addScripts.len(); i++) {
                    totalScripts.add(addScripts.get(i));
                }
            }

            const totalWitnesses = CardanoWasmLoader.Cardano.TransactionWitnessSet.new();
            totalWitnesses.set_vkeys(totalVkeys);
            totalWitnesses.set_native_scripts(totalScripts);

            result = CardanoWasmLoader.Cardano.Transaction.new(
                transaction.body(),
                totalWitnesses,
                transaction.auxiliary_data()
            );
        } catch (e: any) {
            console.error("Error in signing Tx:", e)
            let err: CardanoWalletInteropError = {
                type: CardanoWalletInteropErrorType.signTxError,
                message: e.info
            }
            await this.ThrowErrorAsync(err);
        }
        return result;
    }

    private async CreateNormalTx(output: TxOutput, metadata: string = ""): Promise<Transaction | null> {
        try {
            await CardanoWalletInterop.EnsureCardanoWasmLoadedAsync();
            let changeValue = CardanoWasmLoader.Cardano.Value.new(CardanoWasmLoader.Cardano.BigNum.from_str("0"));
            const utxos = await CardanoWalletInterop.SelectUtxosAsync(output);
            const inputs = CardanoWasmLoader.Cardano.TransactionInputs.new();
            utxos.forEach(utxo => {
                inputs.add(CardanoWasmLoader.Cardano.TransactionInput.new(
                    utxo.input().transaction_id(),
                    utxo.input().index()
                ));
                
                changeValue = changeValue.checked_add(utxo.output().amount());
            });

            const lovelaceAsset = output.amount.find((asset) => asset.unit === 'lovelace');
            const lovelaceQuantity = lovelaceAsset?.quantity ?? 0;

            let outputValue = CardanoWasmLoader.Cardano.Value.new(
                CardanoWasmLoader.Cardano.BigNum.from_str(lovelaceQuantity.toString())
            );

            //Current implementation only supports 1 type of token + lovelace in an output
            if (output.amount.length > 1) {
                const multiAsset = CardanoWasmLoader.Cardano.MultiAsset.new();
                let asset = output.amount.find(asset => asset.unit !== 'lovelace');
                if (asset) {
                    const assetsValue = CardanoWasmLoader.Cardano.Assets.new();
                    assetsValue.insert(
                        CardanoWasmLoader.Cardano.AssetName.new(Buffer.from(asset.unit.slice(56), 'hex')),
                        CardanoWasmLoader.Cardano.BigNum.from_str(asset.quantity.toString())
                    );

                    multiAsset.insert(
                        CardanoWasmLoader.Cardano.ScriptHash.from_bytes(Buffer.from(asset.unit.slice(0, 56), 'hex')),
                        assetsValue
                    );
                }

                outputValue.set_multiasset(multiAsset);
            }

            const rawOutputs = CardanoWasmLoader.Cardano.TransactionOutputs.new();
            // sent to recipient
            rawOutputs.add(
                CardanoWasmLoader.Cardano.TransactionOutput.new(
                    CardanoWasmLoader.Cardano.Address.from_bech32(output.address),
                    outputValue
                )
            )
            changeValue = changeValue.checked_sub(outputValue);
            
            //handle change
            const ownAddressHex = (await window.cardano.getUsedAddresses())[0];
            const ownAddressBuffer = Buffer.from(ownAddressHex, "hex");
            const ownAddress = CardanoWasmLoader.Cardano.Address.from_bytes(ownAddressBuffer);
            rawOutputs.add(CardanoWasmLoader.Cardano.TransactionOutput.new(
                ownAddress,
                changeValue
            ));
            
            //construct metadata
            //add metadata to the tx
            const generalMetadata = CardanoWasmLoader.Cardano.GeneralTransactionMetadata.new();
            generalMetadata.insert(
                CardanoWasmLoader.Cardano.BigNum.from_str("7283"),
                CardanoWasmLoader.Cardano.encode_json_str_to_metadatum(metadata, 0)
            );
            let _metadata = CardanoWasmLoader.Cardano.AuxiliaryData.new();
            _metadata.set_metadata(generalMetadata);
            
            //create dummy witness to account for in fees calculation
            let dummyWitnesses = CardanoWasmLoader.Cardano.TransactionWitnessSet.new();
            let vKeys = CardanoWasmLoader.Cardano.Vkeywitnesses.new();
            vKeys.add(CardanoWasmLoader.Cardano.Vkeywitness.from_bytes(
                Buffer.from("8258208814c250f40bfc74d6c64f02fc75a54e68a9a8b3736e408d9820a6093d5e38b95840f04a036fa56b180af6537b2bba79cec75191dc47419e1fd8a4a892e7d84b7195348b3989c15f1e7b895c5ccee65a1931615b4bdb8bbbd01e6170db7a6831310c","hex")
            ));
            dummyWitnesses.set_vkeys(vKeys);

            const protocolParams = await this.GetProtocolParametersAsync();
            const latestBlock = await this.GetLatestBlockAsync();
            const rawTxBody = CardanoWasmLoader.Cardano.TransactionBody.new(
                inputs,
                rawOutputs,
                CardanoWasmLoader.Cardano.BigNum.from_str("0"),
                latestBlock.slot + 1000
            );
            
            rawTxBody.set_auxiliary_data_hash(
                CardanoWasmLoader.Cardano.hash_auxiliary_data(_metadata)
            );
            
            const rawTx = CardanoWasmLoader.Cardano.Transaction.new(
                rawTxBody,
                dummyWitnesses,
                _metadata
            );

            let fee = CardanoWasmLoader.Cardano.min_fee(rawTx, CardanoWasmLoader.Cardano.LinearFee.new(
                CardanoWasmLoader.Cardano.BigNum.from_str(protocolParams.min_fee_a.toString()),
                CardanoWasmLoader.Cardano.BigNum.from_str(protocolParams.min_fee_b.toString())));

            fee = fee.checked_add(CardanoWasmLoader.Cardano.BigNum.from_str("5000"));
            
            //subtract fee from initial changeValue
            changeValue = changeValue.checked_sub(CardanoWasmLoader.Cardano.Value.new(fee));
            
            //construct final output with fees considered
            const finalOutputs = CardanoWasmLoader.Cardano.TransactionOutputs.new();
            finalOutputs.add(
                CardanoWasmLoader.Cardano.TransactionOutput.new(
                    CardanoWasmLoader.Cardano.Address.from_bech32(output.address),
                    outputValue
                )
            )
            
            finalOutputs.add(
                CardanoWasmLoader.Cardano.TransactionOutput.new(
                    ownAddress,
                    changeValue
                )
            )
            
            //construct final transaction
            const finalTxBody = CardanoWasmLoader.Cardano.TransactionBody.new(
                inputs,
                finalOutputs,
                fee,
                latestBlock.slot + 1000
            );

            _metadata = CardanoWasmLoader.Cardano.AuxiliaryData.new();
            _metadata.set_metadata(generalMetadata);
            finalTxBody.set_auxiliary_data_hash(
                CardanoWasmLoader.Cardano.hash_auxiliary_data(_metadata)
            );
            
            const finalTx = CardanoWasmLoader.Cardano.Transaction.new(
                finalTxBody,
                CardanoWasmLoader.Cardano.TransactionWitnessSet.new(),
                _metadata
            );

            if (finalTx.to_bytes().length * 2 > protocolParams.max_tx_size)
                throw Error("Transaction is too big");

            return finalTx;
        } catch (e: any) {
            console.error("Error in Creating Tx:", e);
            let err: CardanoWalletInteropError = {
                type: CardanoWalletInteropErrorType.createTxError,
                message: e
            }
            await this.ThrowErrorAsync(err);
            return null;
        }
    }

    private static async SelectUtxosAsync(txOutput: TxOutput): Promise<TransactionUnspentOutput[]> {
        await CardanoWalletInterop.EnsureCardanoWasmLoadedAsync();
        const utxosHex = await window.cardano.getUtxos();
        const utxos: TransactionUnspentOutput[] = utxosHex
            .map(utxo => CardanoWasmLoader.Cardano.TransactionUnspentOutput.from_bytes(Buffer.from(utxo, "hex")));
        
        let sortedUtxos = utxos.sort((a, b) => {
            const aAssetLen = a.output().amount().multiasset()?.len() ?? 0;
            const bAssetLen = b.output().amount().multiasset()?.len() ?? 0;
            
            const aAssetCoin = parseInt(a.output().amount().coin().to_str());
            const bAssetCoin = parseInt(b.output().amount().coin().to_str());
            if (aAssetLen > bAssetLen)
                return 1;
            if (aAssetLen < bAssetLen)
                return -1;
            if (aAssetCoin > bAssetCoin)
                return -1;
            if (aAssetCoin < bAssetCoin)
                return 1;
            else
                return 0;
        });
        
        const selectedUtxos: TransactionUnspentOutput[] = [];
        if (txOutput.amount.length == 1 && txOutput.amount[0].unit == "lovelace") {
            for (let i in sortedUtxos) {
                selectedUtxos.push(sortedUtxos[i]);
                let sum = selectedUtxos
                    .map(utxo => parseInt(utxo.output().amount().coin().to_str()))
                    .reduce((p, n) => p + n);

                if (sum >= txOutput.amount[0].quantity + 1_500_000) break;
            }
        } else if (txOutput.amount.length > 1) {
            const asset = txOutput.amount.find(asset => asset.unit !== "lovelace");
            const lovelace = txOutput.amount.find(asset => asset.unit === "lovelace");
            if (asset && lovelace) {
                const scriptHash = CardanoWasmLoader.Cardano.ScriptHash.from_bytes(Helper.HexToBytes(asset.unit.slice(0, 56)));
                const assetNameHash = CardanoWasmLoader.Cardano.AssetName.new(Helper.HexToBytes(asset.unit.slice(56)));
                
                sortedUtxos = sortedUtxos.sort((a, b) => {
                    const aPolicyAssets = a.output().amount().multiasset()?.get(scriptHash);
                    const aAssetQuantity = aPolicyAssets?.get(assetNameHash) ??
                        CardanoWasmLoader.Cardano.BigNum.from_str("0");

                    const bPolicyAssets = b.output().amount().multiasset()?.get(scriptHash);
                    const bAssetQuantity = bPolicyAssets?.get(assetNameHash) ??
                        CardanoWasmLoader.Cardano.BigNum.from_str("0");

                    const aQ = parseInt(aAssetQuantity.to_str());
                    const bQ = parseInt(bAssetQuantity.to_str());
                    if (aQ < bQ)
                        return 1;
                    else if (aQ > bQ)
                        return -1;
                    else
                        return 0;
                });

                for (let i in sortedUtxos) {
                    selectedUtxos.push(sortedUtxos[i]);
                    let sumToken = selectedUtxos
                        .map(utxo => {
                            const policyAssets = utxo.output().amount().multiasset()?.get(scriptHash);
                            return parseInt((policyAssets?.get(assetNameHash) ??
                                CardanoWasmLoader.Cardano.BigNum.from_str("0")).to_str());
                        })
                        .reduce((p, n) => p + n);

                    let sumLovelace = selectedUtxos
                        .map(utxo => parseInt(utxo.output().amount().coin().to_str()))
                        .reduce((p, n) => p + n);

                    if (sumToken >= asset.quantity && sumLovelace >= (lovelace.quantity + 1_500_000)) break;
                }
            }
        }

        return selectedUtxos;
    }

    private async SubmitTxAsync(transaction: Transaction): Promise<string | null> {
        const response = await fetch(`${this.hoskySwapServerUrl}/tx/submit`, {
            headers: {
                "Content-Type": "application/cbor"
            },
            method: "POST",
            body: Buffer.from(transaction.to_bytes())
        });
        const responseBody = await response.json();
        if (responseBody.status != 200) {
            console.error(responseBody);
            await this.ThrowErrorAsync(responseBody);
            return null;
        } else {
            return responseBody.result;
        }
    }

    private async GetTransactionAsync(hash: string): Promise<Tx | null> {
        let transaction: Tx | null = null;
        while (true) {
            transaction = await this.FetchDataAsync<Tx>(`txs/${hash}`);
            if (transaction !== null)
                break;
            else
                await Helper.Delay(15000);
        }
        return transaction;
    }

    private async GetProtocolParametersAsync(): Promise<ProtocolParameters> {
        let protocolParameters: ProtocolParameters | null;

        while (true) {
            protocolParameters = await this.FetchDataAsync<ProtocolParameters>(`parameters`);
            if (protocolParameters !== null) {
                break;
            } else {
                await Helper.Delay(1000);
            }
        }
        return protocolParameters;
    }

    private async GetLatestBlockAsync(): Promise<Block> {
        let latestBlock: Block | null;
        while (true) {
            latestBlock = await this.FetchDataAsync<Block>("blocks/latest");
            if (latestBlock !== null) {
                break;
            } else {
                await Helper.Delay(1000);
            }
        }
        return latestBlock;
    }

    private async FetchDataAsync<T>(endpoint: string): Promise<T | null> {
        try {
            const response = await fetch(`${this.hoskySwapServerUrl}/${endpoint}`);
            const responseBody = await response.json();
            if (responseBody.error) {
                return null;
            } else {
                return responseBody;
            }
        } catch (e) {
            return null;
        }
    }

    private async ThrowErrorAsync(e: CardanoWalletInteropError): Promise<void> {
        if (this.objectRef) {
            await this.objectRef?.invokeMethodAsync(this.errorCallbackName, e);
        }
    }

    public SetErrorHandlerCallback(objectRef: IDotNetObjectRef, callbackName: string) {
        this.objectRef = objectRef;
        this.errorCallbackName = callbackName;
    }
    
    public HasNami = () => !!window.cardano;
}

export default CardanoWalletInterop;