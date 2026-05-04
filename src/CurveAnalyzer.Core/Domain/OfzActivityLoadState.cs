namespace CurveAnalyzer.Core;

public enum OfzActivityLoadStatus
{
    Loaded = 0,
    NoData = 1,
    Failed = 2
}

public class OfzActivityLoadState
{
    public string BoardId { get; set; } = "TQOB";
    public DateTime TradeDate { get; set; }
    public OfzActivityLoadStatus Status { get; set; }
    public bool IsProvisional { get; set; }
    public int RowsLoaded { get; set; }
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}
