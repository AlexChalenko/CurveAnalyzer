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

    public bool IsRub => IsRubleCode(FaceUnit) || IsRubleCode(CurrencyId);

    public bool IsStandardOfz =>
        IsRub &&
        (ContainsOfz(ShortName) || ContainsOfz(SecName)) &&
        !ContainsForeignCurrencyMarker(ShortName) &&
        !ContainsForeignCurrencyMarker(SecName);

    public string CurrencyMarker
    {
        get
        {
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

    public OfzCouponType CouponType => ClassifyCouponType();

    public string CouponTypeMarker => CouponType switch
    {
        OfzCouponType.Fixed => "ОФЗ-ПД",
        OfzCouponType.Floating => "ОФЗ-ПК",
        OfzCouponType.InflationLinked => "ОФЗ-ИН",
        OfzCouponType.Amortized => "ОФЗ-АД",
        OfzCouponType.Currency => "Валютная",
        _ => "Тип n/a"
    };

    public string TypeMarker
    {
        get
        {
            if (CouponType != OfzCouponType.Unknown)
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

    private OfzCouponType ClassifyCouponType()
    {
        var text = string.Join(
            ' ',
            new[] { SecId, BondType, BondSubType, ShortName, SecName, IssueName, FaceUnit, CurrencyId }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (ContainsAny(text, "валют", "cny", "usd", "eur") ||
            (!IsRub && FirstNonEmpty(FaceUnit, CurrencyId) is not null))
        {
            return OfzCouponType.Currency;
        }

        if (ContainsAny(text, "линкер", "индексируем", "офз-ин", "ОФЗ-ИН"))
        {
            return OfzCouponType.InflationLinked;
        }

        if (ContainsAny(text, "амортиз", "офз-ад", "ОФЗ-АД"))
        {
            return OfzCouponType.Amortized;
        }

        if (ContainsAny(text, "флоат", "переменн", "офз-пк", "ОФЗ-ПК"))
        {
            return OfzCouponType.Floating;
        }

        if (ContainsAny(text, "фикс", "постоянн", "офз-пд", "ОФЗ-ПД"))
        {
            return OfzCouponType.Fixed;
        }

        if (ContainsAny(text, "SU520", "ОФЗ 520", "OFZ 520"))
        {
            return OfzCouponType.InflationLinked;
        }

        if (ContainsAny(text, "SU460", "ОФЗ 460", "OFZ 460"))
        {
            return OfzCouponType.Amortized;
        }

        if (ContainsAny(text, "SU290", "ОФЗ 290", "OFZ 290"))
        {
            return OfzCouponType.Floating;
        }

        if (ContainsAny(text, "SU262", "ОФЗ 262", "OFZ 262"))
        {
            return OfzCouponType.Fixed;
        }

        return OfzCouponType.Unknown;
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }
}
