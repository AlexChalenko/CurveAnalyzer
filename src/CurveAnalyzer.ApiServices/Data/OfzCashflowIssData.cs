using System.Text.Json;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.ApiServices.Data;

internal static class OfzCashflowIssData
{
    public static OfzCashflowEvent? MapCoupon(
        IssJsonTable table,
        JsonElement row,
        string fallbackSecId,
        DateTime loadedAt)
    {
        var secId = table.GetString(row, "secid") ?? fallbackSecId;
        var eventDate = table.GetDate(row, "coupondate");
        if (string.IsNullOrWhiteSpace(secId) || !eventDate.HasValue)
        {
            return null;
        }

        var recordDate = table.GetDate(row, "recorddate");
        var faceUnit = table.GetString(row, "faceunit");
        var couponValues = NormalizeCouponValues(
            table.GetDouble(row, "value"),
            table.GetDouble(row, "value_rub"),
            table.GetDouble(row, "valueprc"),
            faceUnit);
        return new OfzCashflowEvent
        {
            SecId = secId,
            SourceKey = CreateSourceKey("coupon", eventDate.Value, recordDate, table.GetString(row, "primary_boardid")),
            ShortName = table.GetString(row, "name"),
            EventType = OfzCashflowEventType.Coupon,
            EventDate = eventDate.Value.Date,
            RecordDate = recordDate,
            StartDate = table.GetDate(row, "startdate"),
            Value = couponValues.Value,
            ValueRub = couponValues.ValueRub,
            ValuePercent = couponValues.ValuePercent,
            FaceValue = table.GetDouble(row, "facevalue"),
            InitialFaceValue = table.GetDouble(row, "initialfacevalue"),
            FaceUnit = faceUnit,
            SourceKind = OfzCashflowSourceKind.Schedule,
            SourceLabel = "bondization.coupons",
            LoadedAt = loadedAt
        };
    }

    public static OfzCashflowEvent? MapAmortization(
        IssJsonTable table,
        JsonElement row,
        string fallbackSecId,
        DateTime loadedAt)
    {
        var secId = table.GetString(row, "secid") ?? fallbackSecId;
        var eventDate = table.GetDate(row, "amortdate");
        if (string.IsNullOrWhiteSpace(secId) || !eventDate.HasValue)
        {
            return null;
        }

        var dataSource = table.GetString(row, "data_source");
        var eventType = string.Equals(dataSource, "maturity", StringComparison.OrdinalIgnoreCase)
            ? OfzCashflowEventType.Maturity
            : OfzCashflowEventType.Amortization;

        return new OfzCashflowEvent
        {
            SecId = secId,
            SourceKey = CreateSourceKey(eventType == OfzCashflowEventType.Maturity ? "maturity" : "amortization", eventDate.Value, null, dataSource),
            ShortName = table.GetString(row, "name"),
            EventType = eventType,
            EventDate = eventDate.Value.Date,
            Value = table.GetDouble(row, "value"),
            ValueRub = table.GetDouble(row, "value_rub"),
            ValuePercent = table.GetDouble(row, "valueprc"),
            FaceValue = table.GetDouble(row, "facevalue"),
            InitialFaceValue = table.GetDouble(row, "initialfacevalue"),
            FaceUnit = table.GetString(row, "faceunit"),
            SourceKind = OfzCashflowSourceKind.Schedule,
            SourceLabel = "bondization.amortizations",
            LoadedAt = loadedAt
        };
    }

    public static OfzCashflowEvent? MapOffer(
        IssJsonTable table,
        JsonElement row,
        string fallbackSecId,
        DateTime loadedAt)
    {
        var secId = table.GetString(row, "secid") ?? fallbackSecId;
        var eventDate = table.GetDate(row, "offerdate");
        if (string.IsNullOrWhiteSpace(secId) || !eventDate.HasValue)
        {
            return null;
        }

        var startDate = table.GetDate(row, "offerdatestart");
        var endDate = table.GetDate(row, "offerdateend");
        var offerType = table.GetString(row, "offertype");
        return new OfzCashflowEvent
        {
            SecId = secId,
            SourceKey = CreateSourceKey("offer", eventDate.Value, startDate, offerType),
            ShortName = table.GetString(row, "name"),
            EventType = OfzCashflowEventType.Offer,
            EventDate = eventDate.Value.Date,
            StartDate = startDate,
            EndDate = endDate,
            Value = table.GetDouble(row, "value"),
            FaceValue = table.GetDouble(row, "facevalue"),
            FaceUnit = table.GetString(row, "faceunit"),
            Price = table.GetDouble(row, "price"),
            Agent = table.GetString(row, "agent"),
            OfferType = offerType,
            SourceKind = OfzCashflowSourceKind.Schedule,
            SourceLabel = "bondization.offers",
            LoadedAt = loadedAt
        };
    }

    public static IReadOnlyList<OfzCashflowEvent> MapSnapshotFallbackEvents(
        IssJsonTable table,
        JsonElement row,
        string fallbackSecId,
        DateTime loadedAt)
    {
        var secId = table.GetString(row, "SECID") ?? fallbackSecId;
        if (string.IsNullOrWhiteSpace(secId))
        {
            return [];
        }

        var shortName = table.GetString(row, "SHORTNAME") ?? table.GetString(row, "SECNAME") ?? secId;
        var faceValue = table.GetDouble(row, "FACEVALUE");
        var faceUnit = table.GetString(row, "FACEUNIT");
        List<OfzCashflowEvent> events = [];

        var couponValues = NormalizeCouponValues(
            table.GetDouble(row, "COUPONVALUE"),
            null,
            table.GetDouble(row, "COUPONPERCENT"),
            faceUnit);
        AddSnapshotEvent(
            events,
            secId,
            shortName,
            OfzCashflowEventType.Coupon,
            table.GetDate(row, "NEXTCOUPON") ?? table.GetDate(row, "COUPONDATE"),
            loadedAt,
            value: couponValues.Value,
            valueRub: couponValues.ValueRub,
            valuePercent: couponValues.ValuePercent,
            faceValue: faceValue,
            faceUnit: faceUnit);
        AddSnapshotEvent(
            events,
            secId,
            shortName,
            OfzCashflowEventType.Offer,
            table.GetDate(row, "OFFERDATE"),
            loadedAt,
            faceValue: faceValue,
            faceUnit: faceUnit);
        AddSnapshotEvent(
            events,
            secId,
            shortName,
            OfzCashflowEventType.Maturity,
            table.GetDate(row, "MATDATE"),
            loadedAt,
            value: faceValue,
            valueRub: faceValue,
            valuePercent: 100,
            faceValue: faceValue,
            faceUnit: faceUnit);
        AddSnapshotEvent(
            events,
            secId,
            shortName,
            OfzCashflowEventType.Buyback,
            table.GetDate(row, "BUYBACKDATE"),
            loadedAt,
            faceValue: faceValue,
            faceUnit: faceUnit);
        AddSnapshotEvent(
            events,
            secId,
            shortName,
            OfzCashflowEventType.CallOption,
            table.GetDate(row, "CALLOPTIONDATE"),
            loadedAt,
            faceValue: faceValue,
            faceUnit: faceUnit);
        AddSnapshotEvent(
            events,
            secId,
            shortName,
            OfzCashflowEventType.PutOption,
            table.GetDate(row, "PUTOPTIONDATE"),
            loadedAt,
            faceValue: faceValue,
            faceUnit: faceUnit);

        return events;
    }

    private static void AddSnapshotEvent(
        ICollection<OfzCashflowEvent> events,
        string secId,
        string shortName,
        OfzCashflowEventType eventType,
        DateTime? eventDate,
        DateTime loadedAt,
        double? value = null,
        double? valueRub = null,
        double? valuePercent = null,
        double? faceValue = null,
        string? faceUnit = null)
    {
        if (!eventDate.HasValue)
        {
            return;
        }

        events.Add(new OfzCashflowEvent
        {
            SecId = secId,
            SourceKey = CreateSourceKey($"snapshot-{eventType.ToString().ToLowerInvariant()}", eventDate.Value, null, null),
            ShortName = shortName,
            EventType = eventType,
            EventDate = eventDate.Value.Date,
            Value = value,
            ValueRub = valueRub,
            ValuePercent = valuePercent,
            FaceValue = faceValue,
            FaceUnit = faceUnit,
            SourceKind = OfzCashflowSourceKind.Snapshot,
            SourceLabel = "securities.snapshot",
            LoadedAt = loadedAt,
            IsProvisional = true
        });
    }

    private static (double? Value, double? ValueRub, double? ValuePercent) NormalizeCouponValues(
        double? value,
        double? valueRub,
        double? valuePercent,
        string? faceUnit)
    {
        var normalizedValue = NullIfZero(value);
        var normalizedValueRub = NullIfZero(valueRub);
        return (
            normalizedValue,
            normalizedValueRub ?? (IsRubFaceUnit(faceUnit) ? normalizedValue : null),
            NullIfZero(valuePercent));
    }

    private static double? NullIfZero(double? value)
    {
        const double tolerance = 0.0000001;
        return value.HasValue && Math.Abs(value.Value) < tolerance
            ? null
            : value;
    }

    private static bool IsRubFaceUnit(string? faceUnit)
    {
        return string.Equals(faceUnit, "RUB", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(faceUnit, "SUR", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateSourceKey(
        string prefix,
        DateTime eventDate,
        DateTime? extraDate,
        string? extraText)
    {
        var extra = !string.IsNullOrWhiteSpace(extraText)
            ? extraText.Trim()
            : extraDate?.ToString("yyyy-MM-dd") ?? "none";
        return $"{prefix}:{eventDate:yyyy-MM-dd}:{extra}";
    }
}
