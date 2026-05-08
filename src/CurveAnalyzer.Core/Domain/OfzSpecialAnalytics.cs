namespace CurveAnalyzer.Core;

public enum OfzSpecialMetricKind
{
    ImpliedFloatingRate = 0,
    ImpliedCbrRate = 1,
    ImpliedFloatingRateSpread = 2,
    ImpliedInflation = 3
}

public enum OfzSpecialMetricSource
{
    Missing = 0,
    History = 1,
    Snapshot = 2,
    Derived = 3,
    CbrKeyRate = 4
}

public enum OfzSpecialMetricAvailability
{
    Missing = 0,
    Historical = 1,
    SnapshotOnly = 2,
    Provisional = 3,
    InsufficientHistory = 4
}

public class OfzSpecialIssueContext
{
    public static OfzSpecialIssueContext None { get; } = new();

    public OfzCouponType CouponType { get; init; } = OfzCouponType.Unknown;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<OfzSpecialMetricValue> Metrics { get; init; } = [];
    public IReadOnlyList<OfzSpecialMetricSeries> Series { get; init; } = [];
    public IReadOnlyList<string> Limitations { get; init; } = [];

    public bool HasContext => CouponType is OfzCouponType.Floating or OfzCouponType.InflationLinked;
    public bool HasMetrics => Metrics.Count > 0;
    public bool HasSeries => Series.Any(series => series.HasHistoricalSeries);
    public bool HasLimitations => Limitations.Count > 0;
}

public class OfzSpecialMetricValue
{
    public OfzSpecialMetricKind Kind { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public double? Value { get; init; }
    public DateTime? ObservedAt { get; init; }
    public OfzSpecialMetricSource Source { get; init; } = OfzSpecialMetricSource.Missing;
    public OfzSpecialMetricAvailability Availability { get; init; } = OfzSpecialMetricAvailability.Missing;
    public bool IsProvisional { get; init; }

    public bool HasValue => Value.HasValue;
}

public class OfzSpecialMetricSeries
{
    public OfzSpecialMetricKind Kind { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public IReadOnlyList<OfzSpecialMetricSeriesPoint> Points { get; init; } = [];
    public OfzSpecialMetricAvailability Availability { get; init; } = OfzSpecialMetricAvailability.Missing;

    public bool HasHistoricalSeries => Points.Count >= 2;
}

public class OfzSpecialMetricSeriesPoint
{
    public DateTime TradeDate { get; init; }
    public double Value { get; init; }
    public OfzSpecialMetricSource Source { get; init; } = OfzSpecialMetricSource.History;
    public bool IsProvisional { get; init; }
}
