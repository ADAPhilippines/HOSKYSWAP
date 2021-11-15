import CardanoWasmLoader from "./Helpers/CardanoWasmLoader";
import IDotNetObjectRef from "./Interfaces/IDotNetObjectRef";
import CardanoWalletInteropErrorType from "./Enums/CardanoWalletInteropErrorType";
import CardanoWalletInteropError from "./Types/CardanoWalletInteropError";
import {Transaction, TransactionUnspentOutput} from "@emurgo/cardano-serialization-lib-browser";
import {Buffer} from "Buffer";
import Helper from "./Helpers/Helper";
import TxOutput from "./Types/TxOutput";

class CardanoWalletInterop {
    private objectRef: IDotNetObjectRef | null = null;
    private errorCallbackName: string = "OnError";
    private backendUrl: string = "";

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
            console.log("hello");
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
        
        if(unit === "lovelace")
        {
            return totalValue.coin().to_str();
        }
        else
        {
            let asset_bal = BigInt(0);
            const multiAssets = totalValue.multiasset()?.keys();
            if (multiAssets) {
                for (let j = 0; j < multiAssets.len(); j++) {
                    const policy = multiAssets.get(j);
                    const policyAssets = totalValue.multiasset()?.get(policy);
                    const assetNames = policyAssets?.keys();
                    if (assetNames && policyAssets)
                        for (let k = 0; k < assetNames.len(); k++) {
                            const policyAsset = assetNames.get(k);
                            const quantity = policyAssets.get(policyAsset);
                            const asset =
                                Buffer.from(policy.to_bytes()).toString('hex') +
                                Buffer.from(policyAsset.name()).toString('hex');
                            if (asset == unit) {
                                if (quantity?.to_str())
                                    asset_bal += BigInt(quantity.to_str());
                            }
                        }
                }
            }
            return asset_bal.toString();
        }
    }

    private static async EnsureCardanoWasmLoadedAsync() {
        if (CardanoWasmLoader.Cardano == null) {
            await CardanoWasmLoader.Load();
        }
    }

    public async SendAdaAsync(outputs: TxOutput[]): Promise<string | null> {
        let result: string | null = null;
        const transaction = await this.CreateNormalTx(outputs);
        if (transaction !== null) {
            //const signedTx = await this.signTxAsync(transaction);
            //if (signedTx != null) {
                //result = await this.SubmitTxAsync(signedTx);
            //}
        }
        return result;
    }

    private async signTxAsync(transaction: Transaction): Promise<Transaction | null> {
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

    private async CreateNormalTx(outputs: TxOutput[]): Promise<Transaction | null> {
        try {
            const latestBlock = await this.GetLatestBlockAsync();
            let protocolParams = await this.GetProtocolParametersAsync(latestBlock.epoch);

            const txBuilder = CardanoWasmLoader.Cardano.TransactionBuilder.new(
                CardanoWasmLoader.Cardano.LinearFee.new(
                    CardanoWasmLoader.Cardano.BigNum.from_str(protocolParams.min_fee_a.toString()),
                    CardanoWasmLoader.Cardano.BigNum.from_str(protocolParams.min_fee_b.toString())),
                CardanoWasmLoader.Cardano.BigNum.from_str(protocolParams.min_utxo.toString()),
                CardanoWasmLoader.Cardano.BigNum.from_str(protocolParams.pool_deposit.toString()),
                CardanoWasmLoader.Cardano.BigNum.from_str(protocolParams.key_deposit.toString()),
                0,
                0);

            const utxos = await CardanoWalletInterop.SelectUtxosAsync(4000000);
            utxos.forEach(utxo => {
                txBuilder.add_input(
                    utxo.output().address(),
                    utxo.input(),
                    utxo.output().amount());
            });

            outputs.forEach((output) => {
                txBuilder.add_output(
                    CardanoWasmLoader.Cardano.TransactionOutput.new(
                        CardanoWasmLoader.Cardano.Address.from_bech32(output.address),
                        CardanoWasmLoader.Cardano.Value.new(CardanoWasmLoader.Cardano.BigNum.from_str(output.amount.toString()))
                    )
                );
            });

            txBuilder.set_ttl(latestBlock.slot + 1000);

            const addressHex = (await window.cardano.getUsedAddresses())[0];
            const addressBuffer = Buffer.from(addressHex, "hex");
            const address = CardanoWasmLoader.Cardano.Address.from_bytes(addressBuffer);
            txBuilder.add_change_if_needed(address);

            const txBody = txBuilder.build();

            const transaction = CardanoWasmLoader.Cardano.Transaction.new(
                txBody,
                CardanoWasmLoader.Cardano.TransactionWitnessSet.new(), // witnesses
                undefined, // transaction metadata
            );

            if (transaction.to_bytes().length * 2 > protocolParams.max_tx_size)
                throw Error("Transaction is too big");

            return transaction;
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

    private static async SelectUtxosAsync(amount: number): Promise<TransactionUnspentOutput[]> {
        const utxosHex = await window.cardano.getUtxos();
        const utxos
            :
            TransactionUnspentOutput[] = utxosHex
            .map(utxo => CardanoWasmLoader.Cardano.TransactionUnspentOutput.from_bytes(Buffer.from(utxo, "hex")));

        const sortedUtxos = utxos.sort((a, b) => {
            const aAssetLen = a.output().amount().multiasset()?.len() ?? 0;
            const bAssetLen = b.output().amount().multiasset()?.len() ?? 0;
            if (aAssetLen > bAssetLen)
                return 1;
            else if (aAssetLen < bAssetLen)
                return -1;
            else
                return 0;
        });

        const selectedUtxos: TransactionUnspentOutput[] = [];
        for (let i in sortedUtxos) {
            selectedUtxos.push(sortedUtxos[i]);
            let sum = selectedUtxos
                .map(utxo => parseInt(utxo.output().amount().coin().to_str()))
                .reduce((p, n) => p + n);

            if (sum >= amount) break;
        }

        return selectedUtxos;
    }

    private async GetProtocolParametersAsync(epoch: number): Promise<ProtocolParameters> {
        let protocolParameters: ProtocolParameters | null;

        while (true) {
            protocolParameters = await this.GetFromBlockfrostAsync<ProtocolParameters>(`epochs/${epoch}/parameters`);
            if (protocolParameters !== null) {
                break;
            } else {
                await Helper.Delay(1000);
            }
        }
        return protocolParameters;
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
}

export default CardanoWalletInterop;