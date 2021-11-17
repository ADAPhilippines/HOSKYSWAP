namespace HOSKYSWAP.Common;

public enum Status
{
    Open,
    Filled,
    Cancelled,
    Error,
    Ignored,
    Confirming,
    Cancelling
}

public record Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerAddress { get; set; } = string.Empty;
    public string TxHash { get; set; } = string.Empty;
    public List<long> TxIndexes { get; set; } = new();
    public string Action { get; set; } = string.Empty;
    public decimal Rate { get; set; } = 0;
    public ulong Total { get; set; } = 0;
    public Status Status { get; set; } = Status.Open;
    public string ExecuteTxId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}