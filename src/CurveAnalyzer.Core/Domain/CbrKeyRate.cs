namespace CurveAnalyzer.Core;

public sealed class CbrKeyRate
{
    public DateTime Date { get; init; }
    public double Rate { get; init; }
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;
}
