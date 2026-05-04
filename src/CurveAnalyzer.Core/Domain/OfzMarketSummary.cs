using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurveAnalyzer.Core;

public enum OfzSummaryFindingKind
{
    MarketActivity = 0,
    SegmentConcentration = 1,
    RepeatedIssue = 2,
    YieldMove = 3,
    WeakLiquidity = 4,
    SpreadSignal = 5,
    DataQuality = 6
}

public enum OfzSummaryScope
{
    Market = 0,
    Segment = 1,
    Issue = 2,
    Date = 3,
    Liquidity = 4,
    Spread = 5,
    DataQuality = 6
}

public enum OfzDataLimitationKind
{
    NoData = 0,
    InsufficientBaseline = 1,
    MissingSpread = 2,
    MissingQuotes = 3,
    SnapshotOnly = 4,
    Provisional = 5,
    CurrencyMixed = 6,
    ShortRange = 7
}

public enum OfzIssueFocusReason
{
    TopValue = 0,
    TopScore = 1,
    WeakLiquidity = 2,
    YieldMove = 3,
    RepeatedActivity = 4
}

public enum OfzSummarySignalScope
{
    AllDays = 0,
    LastAvailableDay = 1
}

public enum OfzSummaryDrillDownTarget
{
    HeatmapDate = 0,
    IssueDetail = 1,
    SegmentDetail = 2,
    WeakLiquidityTable = 3,
    ActivityTable = 4
}

public class OfzMarketSummary
{
    public const string CurrentSchemaVersion = "1.0";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime StartDate { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime EndDate { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public OfzCouponType? CouponTypeFilter { get; init; }
    public string? CouponTypeMarker { get; init; }
    public OfzSummarySignalScope SignalScope { get; init; } = OfzSummarySignalScope.AllDays;
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime InsightStartDate { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime InsightEndDate { get; init; }
    public IReadOnlyList<OfzSummaryFinding> Findings { get; init; } = [];
    public IReadOnlyList<OfzSegmentSummary> Segments { get; init; } = [];
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
    public OfzSummarySourceCounts SourceCounts { get; init; } = new();
}

public class OfzSummaryFinding
{
    public string Id { get; init; } = string.Empty;
    public OfzSummaryFindingKind Kind { get; init; }
    public int Priority { get; init; }
    public OfzSummaryScope Scope { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public OfzFindingEvidence Evidence { get; init; } = new();
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
    public OfzSummaryDrillDown? DrillDown { get; init; }
}

public class OfzFindingEvidence
{
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? TradeDate { get; init; }
    public string? SecId { get; init; }
    public string? ShortName { get; init; }
    public OfzCouponType? CouponType { get; init; }
    public string? CouponTypeMarker { get; init; }
    public int? IssueCount { get; init; }
    public int? ActiveIssueCount { get; init; }
    public int? RankableIssueCount { get; init; }
    public double? TotalValue { get; init; }
    public double? Value { get; init; }
    public int? TotalNumTrades { get; init; }
    public int? NumTrades { get; init; }
    public double? ActivityScore { get; init; }
    public double? MedianActivityScore { get; init; }
    public double? MaxActivityScore { get; init; }
    public double? YieldMove { get; init; }
    public double? YieldValue { get; init; }
    public double? Duration { get; init; }
    public double? Spread { get; init; }
    public OfzSpreadSource? SpreadSource { get; init; }
    public double? LiquidityScore { get; init; }
    public OfzLiquidityBucket? LiquidityBucket { get; init; }
    public OfzLiquidityMetricStatus? LiquidityStatus { get; init; }
    public double? ZSpread { get; init; }
    public double? ZSpreadBp { get; init; }
    public double? GSpreadBp { get; init; }
    public bool IsSnapshot { get; init; }
    public bool IsProvisional { get; init; }
}

public class OfzSegmentSummary
{
    public OfzCouponType CouponType { get; init; }
    public string CouponTypeMarker { get; init; } = string.Empty;
    public int IssueCount { get; init; }
    public int ActiveIssueCount { get; init; }
    public int RankableIssueCount { get; init; }
    public double TotalValue { get; init; }
    public int TotalNumTrades { get; init; }
    public double? MedianActivityScore { get; init; }
    public double? MaxActivityScore { get; init; }
    public int WeakLiquidityCount { get; init; }
    public int MissingQuotesCount { get; init; }
    public int SnapshotOnlyCount { get; init; }
    public IReadOnlyList<OfzIssueFocus> TopIssues { get; init; } = [];
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];

    [JsonIgnore]
    public string TotalValueUnitMarker => CouponType == OfzCouponType.Currency ? "вал." : "RUB";
}

public class OfzIssueFocus
{
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public string? CouponTypeMarker { get; init; }
    public string? DisplayMarker { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? TradeDate { get; init; }
    public double? Value { get; init; }
    public int? NumTrades { get; init; }
    public double? ActivityScore { get; init; }
    public double? YieldMove { get; init; }
    public double? Duration { get; init; }
    public double? Spread { get; init; }
    public double? LiquidityScore { get; init; }
    public OfzLiquidityBucket? LiquidityBucket { get; init; }
    public OfzLiquidityMetricStatus? LiquidityStatus { get; init; }
    public bool IsSnapshot { get; init; }
    public OfzIssueFocusReason Reason { get; init; }
}

public class OfzDataLimitation
{
    public OfzDataLimitationKind Kind { get; init; }
    public OfzSummaryScope Scope { get; init; }
    public string Text { get; init; } = string.Empty;
    public string? SecId { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? TradeDate { get; init; }
    public OfzCouponType? CouponType { get; init; }
}

public class OfzSummaryDrillDown
{
    public OfzSummaryDrillDownTarget Target { get; init; }
    public string? SecId { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? TradeDate { get; init; }
    public OfzCouponType? CouponType { get; init; }
}

public class OfzSummarySourceCounts
{
    public int Issues { get; init; }
    public int Trades { get; init; }
    public int ActivityMetrics { get; init; }
    public int LiquidityMetrics { get; init; }
    public int SnapshotLiquidityMetrics { get; init; }
    public int Dates { get; init; }
}

public sealed class OfzDateJsonConverter : JsonConverterFactory
{
    private const string DateFormat = "yyyy-MM-dd";

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(DateTime) || typeToConvert == typeof(DateTime?);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return typeToConvert == typeof(DateTime)
            ? new DateTimeConverter()
            : new NullableDateTimeConverter();
    }

    private sealed class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return DateTime.ParseExact(value!, DateFormat, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Date.ToString(DateFormat, CultureInfo.InvariantCulture));
        }
    }

    private sealed class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var value = reader.GetString();
            return DateTime.ParseExact(value!, DateFormat, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.Date.ToString(DateFormat, CultureInfo.InvariantCulture));
                return;
            }

            writer.WriteNullValue();
        }
    }
}
