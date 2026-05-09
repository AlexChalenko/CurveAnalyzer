using System.Text.Json.Serialization;

namespace CurveAnalyzer.Core;

public enum OfzCashflowEventType
{
    Coupon = 0,
    Amortization = 1,
    Maturity = 2,
    Offer = 3,
    Buyback = 4,
    CallOption = 5,
    PutOption = 6
}

public enum OfzCashflowSourceKind
{
    Schedule = 0,
    Snapshot = 1,
    DescriptionFallback = 2
}

public sealed class OfzCashflowEvent
{
    public string SecId { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public OfzCashflowEventType EventType { get; set; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime EventDate { get; set; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? StartDate { get; set; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? EndDate { get; set; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime? RecordDate { get; set; }
    public double? Value { get; set; }
    public double? ValueRub { get; set; }
    public double? ValuePercent { get; set; }
    public double? FaceValue { get; set; }
    public double? InitialFaceValue { get; set; }
    public string? FaceUnit { get; set; }
    public double? Price { get; set; }
    public string? Agent { get; set; }
    public string? OfferType { get; set; }
    public OfzCashflowSourceKind SourceKind { get; set; } = OfzCashflowSourceKind.Schedule;
    public string? SourceLabel { get; set; }
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
    public bool IsProvisional { get; set; }
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];
}

public sealed class OfzIssueCashflowCalendar
{
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public OfzCouponType? CouponType { get; init; }
    public string? CouponTypeMarker { get; init; }
    public IReadOnlyList<OfzCashflowEvent> Events { get; init; } = [];
    public IReadOnlyList<OfzCashflowEvent> PeriodEvents { get; init; } = [];
    public IReadOnlyList<OfzCashflowEvent> NearPeriodEvents { get; init; } = [];
    public OfzCashflowEvent? PreviousEvent { get; init; }
    public OfzCashflowEvent? NextEvent { get; init; }
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];

    public bool HasEvents => Events.Count > 0;
}

public sealed class OfzCashflowActivityLink
{
    public string SecId { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime TradeDate { get; init; }
    public OfzCashflowEvent Event { get; init; } = new();
    public int DaysToEvent { get; init; }
    public double? Value { get; init; }
    public int? NumTrades { get; init; }
    public double? ActivityScore { get; init; }
    public double? YieldMove { get; init; }
}

public sealed class OfzCashflowContext
{
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime StartDate { get; init; }
    [JsonConverter(typeof(OfzDateJsonConverter))]
    public DateTime EndDate { get; init; }
    public int EventWindowDays { get; init; }
    public IReadOnlyList<OfzIssueCashflowCalendar> IssueCalendars { get; init; } = [];
    public IReadOnlyList<OfzCashflowEvent> Events { get; init; } = [];
    public IReadOnlyList<OfzCashflowEvent> UpcomingEvents { get; init; } = [];
    public IReadOnlyList<OfzCashflowActivityLink> ActivityLinks { get; init; } = [];
    public IReadOnlyList<OfzDataLimitation> Limitations { get; init; } = [];

    public bool HasEvents => Events.Count > 0 || UpcomingEvents.Count > 0;
}
