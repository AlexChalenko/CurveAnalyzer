namespace CurveAnalyzer.Core;

public class OfzActivityHeatmapCell
{
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public OfzCouponType CouponType { get; init; }
    public string CouponTypeMarker { get; init; } = string.Empty;
    public DateTime TradeDate { get; init; }
    public double? ActivityScore { get; init; }
    public int ScoreBucket { get; init; }
    public double? Value { get; init; }
    public double? YieldMove { get; init; }
    public OfzActivityMetricStatus Status { get; init; }
}

public class OfzActivityIndexPoint
{
    public DateTime TradeDate { get; init; }
    public double TotalValue { get; init; }
    public int TotalNumTrades { get; init; }
    public int ActiveIssueCount { get; init; }
    public int RankableIssueCount { get; init; }
    public double? MedianActivityScore { get; init; }

    public double ActivityIndex => ActiveIssueCount * (MedianActivityScore ?? 0);
}

public class OfzDurationYieldScatterPoint
{
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public string DisplayMarker { get; init; } = string.Empty;
    public OfzCouponType CouponType { get; init; }
    public string CouponTypeMarker { get; init; } = string.Empty;
    public DateTime TradeDate { get; init; }
    public double DurationDays { get; init; }
    public double DurationYears => DurationDays / 365.25;
    public double Duration => DurationDays;
    public double Yield { get; init; }
    public double Value { get; init; }
    public int? NumTrades { get; init; }
    public double ActivityScore { get; init; }
    public int ScoreBucket { get; init; }
    public double? Spread { get; init; }
    public OfzSpreadSource SpreadSource { get; init; }
    public double? LiquidityScore { get; init; }
    public OfzLiquidityBucket? LiquidityBucket { get; init; }
    public OfzLiquidityMetricStatus? LiquidityStatus { get; init; }
    public OfzActivityMetricStatus Status { get; init; }
}

public class OfzIssueDetail
{
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public string DisplayMarker { get; init; } = string.Empty;
    public DateTime? MatDate { get; init; }
    public IReadOnlyList<OfzIssueDetailPoint> Points { get; init; } = [];
    public IReadOnlyList<OfzLiquidityMetric> LiquidityMetrics { get; init; } = [];
    public OfzLiquidityMetric? CurrentLiquiditySnapshot { get; init; }

    public bool HasPoints => Points.Count > 0;
    public bool HasYield => Points.Any(point => point.Yield.HasValue);
    public bool HasPrice => Points.Any(point => point.Price.HasValue);
    public bool HasSpread => Points.Any(point => point.Spread.HasValue) || LiquidityMetrics.Any(metric => metric.Spread.HasValue);
    public bool HasCurrentLiquiditySnapshot => CurrentLiquiditySnapshot is not null;
}

public class OfzIssueDetailPoint
{
    public string SecId { get; init; } = string.Empty;
    public DateTime TradeDate { get; init; }
    public double? Value { get; init; }
    public int? NumTrades { get; init; }
    public double? Price { get; init; }
    public double? Yield { get; init; }
    public double? Bid { get; init; }
    public double? Offer { get; init; }
    public double? Spread { get; init; }
    public double? ZSpread { get; init; }
    public double? ZSpreadAtWeightedAveragePrice { get; init; }
    public OfzSpreadSource SpreadSource { get; init; }
    public double? LiquidityScore { get; init; }
    public OfzLiquidityBucket? LiquidityBucket { get; init; }
    public OfzLiquidityMetricStatus? LiquidityStatus { get; init; }
}

public enum OfzActivityInsightKind
{
    MarketWideActivity = 0,
    RepeatedIssueActivity = 1,
    YieldMoveActivity = 2,
    BlockLikeActivity = 3,
    TradeCountActivity = 4,
    CouponTypeConcentration = 5,
    LiquiditySignal = 6
}

public class OfzActivityInsight
{
    public OfzActivityInsightKind Kind { get; init; }
    public int Severity { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DateTime? TradeDate { get; init; }
    public string? SecId { get; init; }
    public string? ShortName { get; init; }
    public OfzCouponType CouponType { get; init; }
    public string CouponTypeMarker { get; init; } = string.Empty;
    public double? ActivityScore { get; init; }
    public double? Value { get; init; }
    public int? NumTrades { get; init; }
    public double? YieldMove { get; init; }
    public double? Spread { get; init; }
    public double? LiquidityScore { get; init; }
    public OfzLiquidityBucket? LiquidityBucket { get; init; }
    public OfzLiquidityMetricStatus? LiquidityStatus { get; init; }
    public double? ZSpread { get; init; }
    public double? ZSpreadBp { get; init; }
    public double? GSpreadBp { get; init; }
}
