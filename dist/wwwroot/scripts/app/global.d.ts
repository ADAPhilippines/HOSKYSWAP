import CardanoWalletInterop from "./CardanoWalletInterop";
import ICardanoDAPPConnector from "./Interfaces/ICardanoDAPPConnector";
import IDotNetObjectRef from "./Interfaces/IDotNetObjectRef";

declare global {
    interface Window {
        CardanoWalletInterop: CardanoWalletInterop;
        cardano: ICardanoDAPPConnector;
        GenerateIdenticon: (str:string) => string;
    }
}