using System.Text.Json.Serialization;

namespace CurveAnalyzer.Core;

public enum OfzSeasonalityBucketKind
{
    Weekday = 0,
    Month = 1
}

public enum OfzSeasonalityBaselineQuality
{
    Insufficient = 0,
    Weak = 1,
    Strong = 2
}

public enum OfzSeasonalityFindingKind
{
    HighSeasonalActivity = 0,
    LowSeasonalActivity = 1,
    SeasonalityDataLimitation = 2
}

public sealed class OfzSeasonalityObservation
{
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; init; }
    public double? TotalValue { get; init; }
    public int? TotalNumTrades { get; init; }
    public int ActiveIssueCount { get; init; }
    public int ComparableIssueCount { get; init; }
    public int YieldUpCount { get; init; }
    public int YieldDownCount { get; init; }
    public int UnchangedCount { get; init; }
    public bool IsProvisional { get; init; }

    [JsonIgnore]
    public bool IsActive => TotalValue is > 0 || TotalNumTrades is > 0;
}

public sealed class OfzSeasonalityBucket
{
    public OfzSeasonalityBucketKind Kind { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public int ObservationCount { get; init; }
    public int ActiveDayCount { get; init; }
    public double? MedianTotalValue { get; init; }
    public double? AverageTotalValue { get; init; }
    public double? MedianNumTrades { get; init; }
    public double? MedianActiveIssueCount { get; init; }
    public double? UpDayShare { get; init; }
    public double? DownDayShare { get; init; }
    public OfzSeasonalityBaselineQuality BaselineQuality { get; init; } = OfzSeasonalityBaselineQuality.Insufficient;
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
}

public sealed class OfzSeasonalityFinding
{
    public OfzSeasonalityFindingKind Kind { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; init; }
    public OfzSeasonalityBucketKind BucketKind { get; init; }
    public string BucketKey { get; init; } = string.Empty;
    public string BucketLabel { get; init; } = string.Empty;
    public double? ActualValue { get; init; }
    public double? BaselineMedianValue { get; init; }
    public double? ValueRatio { get; init; }
    public int? ActualNumTrades { get; init; }
    public double? BaselineMedianNumTrades { get; init; }
    public int BaselineObservationCount { get; init; }
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
}

public sealed class OfzSeasonalityContext
{
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime StartDate { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime EndDate { get; init; }
    public int MinWeekdayBaselineObservations { get; init; }
    public int MinMonthBaselineObservations { get; init; }
    public double HighActivityRatioThreshold { get; init; }
    public double LowActivityRatioThreshold { get; init; }
    public int ObservationCount { get; init; }
    public IReadOnlyList<OfzSeasonalityBucket> WeekdayBuckets { get; init; } = [];
    public IReadOnlyList<OfzSeasonalityBucket> MonthBuckets { get; init; } = [];
    public IReadOnlyList<OfzSeasonalityFinding> Findings { get; init; } = [];
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
}
