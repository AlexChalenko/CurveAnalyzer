using System.Text.Json.Serialization;

namespace CurveAnalyzer.Core;

public enum OfzMarketIndexRole
{
    WholeMarket = 0,
    DurationSegment = 1
}

public enum OfzMarketIndexReturnKind
{
    Price = 0,
    TotalReturn = 1
}

public enum OfzIndexDurationBucket
{
    All = 0,
    UpTo1Y = 1,
    OneToThreeY = 2,
    ThreeToFiveY = 3,
    SevenYPlus = 4,
    Unknown = 5
}

public enum OfzMarketIndexSourceKind
{
    History = 0,
    Snapshot = 1
}

public enum OfzMarketIndexDirection
{
    Unknown = 0,
    Up = 1,
    Down = 2,
    Flat = 3,
    Mixed = 4
}

public sealed class OfzMarketIndexSeries
{
    public string SecId { get; init; } = string.Empty;
    public string? ShortName { get; init; }
    public string? Name { get; init; }
    public OfzMarketIndexRole Role { get; init; }
    public OfzMarketIndexReturnKind ReturnKind { get; init; }
    public OfzIndexDurationBucket DurationBucket { get; init; } = OfzIndexDurationBucket.Unknown;
    public string? CurrencyId { get; init; }
    public bool IsRequired { get; init; }

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(ShortName)
            ? ShortName
            : !string.IsNullOrWhiteSpace(Name) ? Name : SecId;
}

public sealed class OfzMarketIndexPoint
{
    public string SecId { get; set; } = string.Empty;
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; set; }
    public string? ShortName { get; set; }
    public string? Name { get; set; }
    public double? Close { get; set; }
    public double? Open { get; set; }
    public double? High { get; set; }
    public double? Low { get; set; }
    public double? Value { get; set; }
    public double? Yield { get; set; }
    public double? Duration { get; set; }
    public string? CurrencyId { get; set; }
    public OfzMarketIndexSourceKind SourceKind { get; set; } = OfzMarketIndexSourceKind.History;
    public DateTime? ObservedAt { get; set; }
    public DateTime? TradeSessionDate { get; set; }
    public DateTime? RecalcDate { get; set; }
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
    public bool IsProvisional { get; set; }
}

public sealed class OfzIndexDailyMove
{
    public string SecId { get; init; } = string.Empty;
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? PreviousTradeDate { get; init; }
    public double? Close { get; init; }
    public double? PreviousClose { get; init; }
    public double? Change { get; init; }
    public double? ChangePercent { get; init; }
    public double? Yield { get; init; }
    public double? YieldChange { get; init; }
    public OfzMarketIndexDirection Direction { get; init; } = OfzMarketIndexDirection.Unknown;
    public bool IsMeaningful { get; init; }
    public OfzDataLimitation? Limitation { get; init; }
}

public sealed class OfzIndexContextPoint
{
    public string SecId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public OfzMarketIndexRole Role { get; init; }
    public OfzMarketIndexReturnKind ReturnKind { get; init; }
    public OfzIndexDurationBucket DurationBucket { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; init; }
    public double? Close { get; init; }
    public double? DailyChange { get; init; }
    public double? DailyChangePercent { get; init; }
    public double? Yield { get; init; }
    public double? YieldChange { get; init; }
    public double? Duration { get; init; }
    public double? Value { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? PreviousTradeDate { get; init; }
    public OfzMarketIndexDirection Direction { get; init; } = OfzMarketIndexDirection.Unknown;
    public bool IsMeaningful { get; init; }
    public OfzMarketIndexSourceKind SourceKind { get; init; }
    public bool IsProvisional { get; init; }
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];

    [JsonIgnore]
    public string SourceLabel => SourceKind == OfzMarketIndexSourceKind.Snapshot ? "snapshot" : "history";
}

public sealed class OfzIndexContextDay
{
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; init; }
    public IReadOnlyList<OfzIndexContextPoint> Points { get; init; } = [];
    public OfzMarketIndexDirection MarketDirection { get; init; } = OfzMarketIndexDirection.Unknown;
    public bool HasMeaningfulMove { get; init; }
    public bool IsProvisional { get; init; }
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];

    [JsonIgnore]
    public IReadOnlyList<OfzIndexContextPoint> WholeMarketPoints =>
        Points.Where(point => point.Role == OfzMarketIndexRole.WholeMarket).ToArray();

    [JsonIgnore]
    public IReadOnlyList<OfzIndexContextPoint> SegmentPoints =>
        Points.Where(point => point.Role == OfzMarketIndexRole.DurationSegment).ToArray();

    [JsonIgnore]
    public OfzIndexContextPoint? PriceIndexPoint =>
        WholeMarketPoints.FirstOrDefault(point => point.ReturnKind == OfzMarketIndexReturnKind.Price);

    [JsonIgnore]
    public OfzIndexContextPoint? TotalReturnIndexPoint =>
        WholeMarketPoints.FirstOrDefault(point => point.ReturnKind == OfzMarketIndexReturnKind.TotalReturn);
}

public sealed class OfzIndexSegmentContext
{
    public OfzIndexDurationBucket Bucket { get; init; }
    public string? PriceSeriesSecId { get; init; }
    public string? TotalReturnSeriesSecId { get; init; }
    public int PointCount { get; init; }
    public OfzIndexContextPoint? LatestPricePoint { get; init; }
    public OfzIndexContextPoint? LatestTotalReturnPoint { get; init; }
    public double? PeriodChange { get; init; }
    public double? PeriodChangePercent { get; init; }
    public double? LatestYield { get; init; }
    public double? LatestDuration { get; init; }
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
}

public sealed class OfzIndexContext
{
    public IReadOnlyList<OfzMarketIndexSeries> Series { get; init; } = [];
    public IReadOnlyList<OfzIndexContextDay> Days { get; init; } = [];
    public IReadOnlyList<OfzIndexSegmentContext> Segments { get; init; } = [];
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
}
