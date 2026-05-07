namespace CurveAnalyzer.Core;

public enum OfzClassificationReliability
{
    Unknown = 0,
    Reliable = 1,
    Inferred = 2,
    Conflict = 3
}

public static class OfzIssueClassificationSources
{
    public const string Unknown = "unknown";
    public const string History = "history";
    public const string Snapshot = "snapshot";
    public const string MetadataFields = "metadata-fields";
    public const string SeriesFallback = "series-fallback";
}

public sealed class OfzIssueClassification
{
    public OfzCouponType CouponType { get; init; } = OfzCouponType.Unknown;
    public string CouponTypeMarker { get; init; } = OfzIssueClassifier.GetCouponTypeMarker(OfzCouponType.Unknown);
    public OfzClassificationReliability Reliability { get; init; } = OfzClassificationReliability.Unknown;
    public string Source { get; init; } = OfzIssueClassificationSources.Unknown;
    public string? Evidence { get; init; }
    public IReadOnlyList<string> Limitations { get; init; } = [];
    public bool? IsIndexedNominal { get; init; }
    public bool? IsAmortizing { get; init; }
    public string NominalCurrency { get; init; } = "Unknown";
}
