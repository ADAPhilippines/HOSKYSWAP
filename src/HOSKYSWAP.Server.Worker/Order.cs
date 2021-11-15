namespace HOSKYSWAP.Server.Worker;

public enum Status
{
    Open,
    Filled,
    Canceled,
}

public record Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerAddress { get; set; } = string.Empty;
    public string TxHash { get; set; } = string.Empty;
    public int TxIndex { get; set; } = 0;
    public string Action { get; set; } = string.Empty;
    public decimal Rate { get; set; } = 0;
    public ulong Total { get; set; } = 0;
    public Status Status { get; set; } = Status.Open;
}