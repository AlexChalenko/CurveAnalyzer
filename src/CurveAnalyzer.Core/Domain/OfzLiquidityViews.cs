namespace CurveAnalyzer.Core;

public enum OfzSpreadSource
{
    Missing = 0,
    Provided = 1,
    CalculatedFromBidOffer = 2
}

public enum OfzLiquidityBucket
{
    Good = 0,
    Normal = 1,
    Weak = 2,
    Problem = 3,
    MissingData = 4
}

public enum OfzLiquidityMetricStatus
{
    Ready = 0,
    MissingQuotes = 1,
    MissingDepth = 2,
    SnapshotOnly = 3,
    InsufficientActivity = 4,
    NoData = 5
}

public enum OfzSpreadSignalKind
{
    WideSpread = 0,
    ActivityWithWideSpread = 1,
    MissingQuotesOnActiveDay = 2,
    ImprovingSpread = 3,
    DepthImbalance = 4,
    SpreadOutlier = 5,
    ZSpreadContext = 6
}

public class OfzLiquiditySnapshot
{
    public string BoardId { get; init; } = "TQOB";
    public string SecId { get; init; } = string.Empty;
    public DateTime ObservedAt { get; init; }
    public DateTime TradeDate { get; init; }
    public double? Bid { get; init; }
    public double? Offer { get; init; }
    public double? Spread { get; init; }
    public double? BidDepth { get; init; }
    public double? OfferDepth { get; init; }
    public double? BidDepthTotal { get; init; }
    public double? OfferDepthTotal { get; init; }
    public int? NumBids { get; init; }
    public int? NumOffers { get; init; }
    public double? ValueToday { get; init; }
    public double? VolumeToday { get; init; }
    public int? NumTrades { get; init; }
    public double? EffectiveYield { get; init; }
    public double? EffectiveYieldAtWeightedAveragePrice { get; init; }
    public double? Duration { get; init; }
    public double? DurationAtWeightedAveragePrice { get; init; }
    public double? ZSpread { get; init; }
    public double? ZSpreadAtWeightedAveragePrice { get; init; }
    public double? ZSpreadBp { get; init; }
    public double? GSpreadBp { get; init; }
    public double? ImpliedFloatingRate { get; init; }
    public double? ImpliedInflation { get; init; }
    public double? ImpliedCbrRate { get; init; }
    public bool IsProvisional { get; init; }
}

public class OfzLiquidityMetric
{
    public string SecId { get; init; } = string.Empty;
    public DateTime TradeDate { get; init; }
    public double? Bid { get; init; }
    public double? Offer { get; init; }
    public double? Spread { get; init; }
    public OfzSpreadSource SpreadSource { get; init; }
    public double? BidDepthTotal { get; init; }
    public double? OfferDepthTotal { get; init; }
    public double? Value { get; init; }
    public int? NumTrades { get; init; }
    public double? ZSpread { get; init; }
    public double? ZSpreadAtWeightedAveragePrice { get; init; }
    public double? ZSpreadBp { get; init; }
    public double? GSpreadBp { get; init; }
    public double? LiquidityScore { get; init; }
    public OfzLiquidityBucket LiquidityBucket { get; init; }
    public OfzLiquidityMetricStatus Status { get; init; }
    public bool IsSnapshot { get; init; }
    public bool IsProvisional { get; init; }
    public DateTime? ObservedAt { get; init; }
}

public class OfzWeakLiquidityItem
{
    public int Rank { get; init; }
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public DateTime TradeDate { get; init; }
    public OfzCouponType CouponType { get; init; }
    public string CouponTypeMarker { get; init; } = string.Empty;
    public double? Spread { get; init; }
    public OfzSpreadSource SpreadSource { get; init; }
    public double? LiquidityScore { get; init; }
    public OfzLiquidityBucket LiquidityBucket { get; init; }
    public OfzLiquidityMetricStatus Status { get; init; }
    public double? Value { get; init; }
    public int? NumTrades { get; init; }
    public bool IsSnapshot { get; init; }
    public bool IsProvisional { get; init; }
}

public class OfzIssueLiquidityProfile
{
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public string DisplayMarker { get; init; } = string.Empty;
    public IReadOnlyList<OfzLiquidityMetric> HistoricalMetrics { get; init; } = [];
    public OfzLiquidityMetric? CurrentSnapshotMetric { get; init; }

    public bool HasHistoricalSpread => HistoricalMetrics.Any(metric => metric.Spread.HasValue);
    public bool HasCurrentSnapshot => CurrentSnapshotMetric is not null;
}

public class OfzSpreadSignal
{
    public OfzSpreadSignalKind Kind { get; init; }
    public int Severity { get; init; }
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public DateTime TradeDate { get; init; }
    public OfzCouponType CouponType { get; init; }
    public string CouponTypeMarker { get; init; } = string.Empty;
    public double? Value { get; init; }
    public int? NumTrades { get; init; }
    public double? Spread { get; init; }
    public double? LiquidityScore { get; init; }
    public OfzLiquidityBucket? LiquidityBucket { get; init; }
    public OfzLiquidityMetricStatus? LiquidityStatus { get; init; }
    public double? ZSpread { get; init; }
    public double? ZSpreadBp { get; init; }
    public double? GSpreadBp { get; init; }
    public string Text { get; init; } = string.Empty;
}
