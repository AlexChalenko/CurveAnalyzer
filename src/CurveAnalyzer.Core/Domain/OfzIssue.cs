namespace CurveAnalyzer.Core;

public enum OfzCouponType
{
    Unknown = 0,
    Fixed = 1,
    Floating = 2,
    InflationLinked = 3,
    Amortized = 4,
    Currency = 5
}

public class OfzIssue
{
    private static readonly string[] RubleCurrencyCodes = ["RUB", "RUR", "SUR"];

    public string SecId { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? SecName { get; set; }
    public string? IssueName { get; set; }
    public string? Isin { get; set; }
    public DateTime? MatDate { get; set; }
    public double? FaceValue { get; set; }
    public double? InitialFaceValue { get; set; }
    public string? FaceUnit { get; set; }
    public string? CurrencyId { get; set; }
    public double? CouponPercent { get; set; }
    public double? CouponValue { get; set; }
    public int? CouponPeriod { get; set; }
    public DateTime? NextCouponDate { get; set; }
    public int? ListLevel { get; set; }
    public double? IssueSize { get; set; }
    public double? IssueSizePlaced { get; set; }
    public DateTime? MetadataLoadedAt { get; set; }
    public string? BondType { get; set; }
    public string? BondSubType { get; set; }
    public OfzCouponType? NormalizedCouponType { get; set; }
    public string? NormalizedTypeMarker { get; set; }
    public OfzClassificationReliability? ClassificationReliability { get; set; }
    public string? ClassificationSource { get; set; }
    public string? ClassificationEvidence { get; set; }
    public DateTime? ClassificationLoadedAt { get; set; }
    public bool? IsIndexedNominal { get; set; }
    public bool? IsAmortizing { get; set; }
    public string? NominalCurrency { get; set; }

    public bool IsRub => IsRubleCode(NominalCurrency) || IsRubleCode(FaceUnit) || IsRubleCode(CurrencyId);

    public bool IsStandardOfz =>
        IsRub &&
        (ContainsOfz(ShortName) || ContainsOfz(SecName)) &&
        !ContainsForeignCurrencyMarker(ShortName) &&
        !ContainsForeignCurrencyMarker(SecName);

    public string CurrencyMarker
    {
        get
        {
            var normalizedCurrency = FirstNonEmpty(NominalCurrency);
            if (!string.IsNullOrWhiteSpace(normalizedCurrency) &&
                !normalizedCurrency.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedCurrency;
            }

            var faceUnit = FirstNonEmpty(FaceUnit);
            if (!string.IsNullOrWhiteSpace(faceUnit) && !IsRubleCode(faceUnit))
            {
                return faceUnit;
            }

            var currencyId = FirstNonEmpty(CurrencyId);
            if (!string.IsNullOrWhiteSpace(currencyId) && !IsRubleCode(currencyId))
            {
                return currencyId;
            }

            if (IsRub)
            {
                return "RUB";
            }

            return FirstNonEmpty(FaceUnit, CurrencyId) ?? "Currency n/a";
        }
    }

    public OfzCouponType CouponType =>
        HasStoredClassification
            ? NormalizedCouponType ?? OfzCouponType.Unknown
            : OfzIssueClassifier.Classify(this).CouponType;

    public string CouponTypeMarker =>
        HasStoredClassification && !string.IsNullOrWhiteSpace(NormalizedTypeMarker)
            ? NormalizedTypeMarker
            : OfzIssueClassifier.GetCouponTypeMarker(CouponType);

    public string TypeMarker
    {
        get
        {
            if (CouponType != OfzCouponType.Unknown)
            {
                return CouponTypeMarker;
            }

            if (HasStoredClassification)
            {
                return CouponTypeMarker;
            }

            if (IsStandardOfz)
            {
                return "ОФЗ";
            }

            return FirstNonEmpty(BondSubType, BondType) ?? "Type n/a";
        }
    }

    public string DisplayMarker => $"{CurrencyMarker} / {TypeMarker}";

    public void ApplyClassification(OfzIssueClassification classification, DateTime? loadedAt = null)
    {
        ArgumentNullException.ThrowIfNull(classification);

        NormalizedCouponType = classification.CouponType;
        NormalizedTypeMarker = classification.CouponTypeMarker;
        ClassificationReliability = classification.Reliability;
        ClassificationSource = classification.Source;
        ClassificationEvidence = classification.Evidence;
        ClassificationLoadedAt = loadedAt ?? DateTime.UtcNow;
        IsIndexedNominal = classification.IsIndexedNominal;
        IsAmortizing = classification.IsAmortizing;
        NominalCurrency = classification.NominalCurrency;
    }

    private bool HasStoredClassification =>
        NormalizedCouponType.HasValue ||
        ClassificationReliability.HasValue ||
        !string.IsNullOrWhiteSpace(NormalizedTypeMarker);

    private static bool IsRubleCode(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            RubleCurrencyCodes.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContainsOfz(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            (value.Contains("ОФЗ", StringComparison.OrdinalIgnoreCase) ||
             value.Contains("OFZ", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsForeignCurrencyMarker(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("USD", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("EUR", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("вал", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }
}
