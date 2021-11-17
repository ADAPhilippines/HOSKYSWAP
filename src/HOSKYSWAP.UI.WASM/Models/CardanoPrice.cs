namespace HOSKYSWAP.UI.WASM.Models;

public class CardanoPrice
{
    public PriceUsd Cardano { get; set; } = new PriceUsd();
}

public class PriceUsd
{
    public decimal USD { get; set; }
}