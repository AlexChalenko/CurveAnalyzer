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
    public OfzCouponType CouponType { get; init; } = OfzCouponType.Unknown;
    public string CouponTypeMarker { get; init; } = OfzIssueClassifier.GetCouponTypeMarker(OfzCouponType.Unknown);
    public OfzClassificationReliability ClassificationReliability { get; init; } = OfzClassificationReliability.Unknown;
    public string ClassificationSource { get; init; } = OfzIssueClassificationSources.Unknown;
    public string? ClassificationEvidence { get; init; }
    public DateTime? ClassificationLoadedAt { get; init; }
    public IReadOnlyList<string> ClassificationLimitations { get; init; } = [];
    public bool? IsIndexedNominal { get; init; }
    public bool? IsAmortizing { get; init; }
    public string NominalCurrency { get; init; } = "Unknown";
    public DateTime? MatDate { get; init; }
    public IReadOnlyList<OfzIssueDetailPoint> Points { get; init; } = [];
    public IReadOnlyList<OfzLiquidityMetric> LiquidityMetrics { get; init; } = [];
    public OfzLiquidityMetric? CurrentLiquiditySnapshot { get; init; }
    public OfzSpecialIssueContext SpecialContext { get; init; } = OfzSpecialIssueContext.None;

    public bool HasPoints => Points.Count > 0;
    public bool HasYield => Points.Any(point => point.Yield.HasValue);
    public bool HasPrice => Points.Any(point => point.Price.HasValue);
    public bool HasSpread => Points.Any(point => point.Spread.HasValue) || LiquidityMetrics.Any(metric => metric.Spread.HasValue);
    public bool HasCurrentLiquiditySnapshot => CurrentLiquiditySnapshot is not null;
    public bool HasSpecialContext => SpecialContext.HasContext;
    public bool HasClassificationEvidence => !string.IsNullOrWhiteSpace(ClassificationEvidence);
    public bool HasClassificationLimitations => ClassificationLimitations.Count > 0;

    public string ClassificationReliabilityText => ClassificationReliability switch
    {
        OfzClassificationReliability.Reliable => "надежная",
        OfzClassificationReliability.Inferred => "эвристика",
        OfzClassificationReliability.Conflict => "конфликт",
        _ => "unknown"
    };

    public string IndexedNominalText => IsIndexedNominal switch
    {
        true => "да",
        false => "нет",
        _ => "n/a"
    };

    public string AmortizingText => IsAmortizing switch
    {
        true => "да",
        false => "нет",
        _ => "n/a"
    };

    public string ClassificationLoadedAtText =>
        ClassificationLoadedAt.HasValue
            ? ClassificationLoadedAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            : "n/a";
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
    public double? ImpliedFloatingRate { get; init; }
    public double? ImpliedInflation { get; init; }
    public double? ImpliedCbrRate { get; init; }
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
