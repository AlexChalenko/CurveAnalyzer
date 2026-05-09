using System.Text.Json.Serialization;

namespace CurveAnalyzer.Core;

public enum OfzExternalFactorKind
{
    PolicyRate = 0,
    MarketIndex = 1,
    DerivedOfzMetric = 2,
    Currency = 3,
    Commodity = 4
}

public enum OfzExternalFactorScope
{
    Market = 0,
    Segment = 1,
    Issue = 2,
    Derived = 3
}

public enum OfzExternalFactorSource
{
    Missing = 0,
    CbrKeyRate = 1,
    MoexIndex = 2,
    OfzDerived = 3
}

public enum OfzExternalFactorAvailability
{
    Missing = 0,
    Historical = 1,
    SnapshotOnly = 2,
    Provisional = 3,
    InsufficientHistory = 4
}

public enum OfzExternalFactorLinkKind
{
    ActivityWithFactorMove = 0,
    ActivityWithoutFactorMove = 1,
    YieldMoveWithFactorMove = 2,
    MissingFactor = 3
}

public sealed class OfzExternalFactorObservation
{
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; init; }
    public double? Value { get; init; }
    public double? DailyChange { get; init; }
    public double? DailyChangePercent { get; init; }
    public OfzMarketIndexDirection? Direction { get; init; }
    public bool IsMeaningful { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? PreviousTradeDate { get; init; }
    public string SourceLabel { get; init; } = string.Empty;
    public bool IsSnapshot { get; init; }
    public bool IsProvisional { get; init; }
}

public sealed class OfzExternalFactorSeries
{
    public OfzExternalFactorKind Kind { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Unit { get; init; }
    public OfzExternalFactorScope Scope { get; init; }
    public OfzExternalFactorSource Source { get; init; } = OfzExternalFactorSource.Missing;
    public OfzExternalFactorAvailability Availability { get; init; } = OfzExternalFactorAvailability.Missing;
    public IReadOnlyList<OfzExternalFactorObservation> Observations { get; init; } = [];
    public OfzExternalFactorObservation? LatestObservation { get; init; }
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];

    [JsonIgnore]
    public bool HasValue => Observations.Any(observation => observation.Value.HasValue);
}

public sealed class OfzExternalFactorActivityLink
{
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; init; }
    public string FactorCode { get; init; } = string.Empty;
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? FactorObservationDate { get; init; }
    public OfzExternalFactorLinkKind LinkKind { get; init; }
    public double? ActivityValue { get; init; }
    public double? ActivityScore { get; init; }
    public double? YieldMove { get; init; }
    public string Text { get; init; } = string.Empty;
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
}

public sealed class OfzExternalFactorsContext
{
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime StartDate { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime EndDate { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime InsightStartDate { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime InsightEndDate { get; init; }
    public int ObservationCount { get; init; }
    public int MissingFactorCount { get; init; }
    public IReadOnlyList<OfzExternalFactorSeries> FactorSeries { get; init; } = [];
    public IReadOnlyList<OfzExternalFactorActivityLink> Links { get; init; } = [];
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
}
