namespace CurveAnalyzer.Core;

public enum OfzActivityMetricStatus
{
    Ready = 0,
    InsufficientBaseline = 1,
    MissingValue = 2,
    MissingBaseline = 3,
    MissingYield = 4,
    NoData = 5
}

public class OfzActivityMetric
{
    public string SecId { get; init; } = string.Empty;
    public DateTime TradeDate { get; init; }
    public double? Value { get; init; }
    public double? BaselineMedianValue { get; init; }
    public int BaselineDays { get; init; }
    public double? ActivityScore { get; init; }
    public int? NumTrades { get; init; }
    public double? YieldValue { get; init; }
    public double? PreviousYieldValue { get; init; }
    public double? YieldMove { get; init; }
    public double? Duration { get; init; }
    public OfzActivityMetricStatus Status { get; init; }

    public bool IsRankable =>
        ActivityScore.HasValue &&
        ActivityScore.Value > 0 &&
        Status is OfzActivityMetricStatus.Ready or OfzActivityMetricStatus.MissingYield;
}
