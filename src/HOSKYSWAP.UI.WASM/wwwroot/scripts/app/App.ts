import CardanoWalletInterop from "./CardanoWalletInterop";
import IdenticonHelper from "./Helpers/IdenticonHelper";
import Helper from "./Helpers/Helper";

window.CardanoWalletInterop = new CardanoWalletInterop();
window.GenerateIdenticon = IdenticonHelper.GenerateIdenticon;
window.ScrollElementIntoView = Helper.ScrollElementIntoView;