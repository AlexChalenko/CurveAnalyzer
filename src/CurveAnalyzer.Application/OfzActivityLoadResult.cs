using CurveAnalyzer.Core;

namespace CurveAnalyzer.Application;

public sealed record OfzActivityLoadResult(
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<OfzIssue> Issues,
    IReadOnlyList<OfzDailyTrade> Trades,
    IReadOnlyList<OfzActivityMetric> Metrics)
{
    public IReadOnlyList<OfzLiquiditySnapshot> LiquiditySnapshots { get; init; } = [];
    public IReadOnlyList<OfzLiquidityMetric> LiquidityMetrics { get; init; } = [];
    public IReadOnlyList<OfzLiquidityMetric> SnapshotLiquidityMetrics { get; init; } = [];

    public bool HasProvisionalLiquiditySnapshots =>
        LiquiditySnapshots.Any(snapshot => snapshot.IsProvisional);
}
