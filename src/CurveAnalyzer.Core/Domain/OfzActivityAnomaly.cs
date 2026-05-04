namespace CurveAnalyzer.Core;

public class OfzActivityAnomaly
{
    public int Rank { get; init; }
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public DateTime TradeDate { get; init; }
    public double Value { get; init; }
    public int? NumTrades { get; init; }
    public double ActivityScore { get; init; }
    public double? BaselineMedianValue { get; init; }
    public int BaselineDays { get; init; }
    public double? YieldMove { get; init; }
    public double? Duration { get; init; }
    public string CurrencyMarker { get; init; } = string.Empty;
    public OfzCouponType CouponType { get; init; }
    public string CouponTypeMarker { get; init; } = string.Empty;
    public string TypeMarker { get; init; } = string.Empty;
    public string DisplayMarker { get; init; } = string.Empty;
    public OfzActivityMetricStatus Status { get; init; }
}
