using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Services;

public sealed class OfzCashflowContextBuilderTests
{
    private static readonly DateTime StartDate = new(2026, 05, 04);
    private static readonly DateTime EndDate = new(2026, 05, 08);

    [Fact]
    public void Build_FiltersEventsToSelectedAndNearPeriodWindows()
    {
        var context = OfzCashflowContextBuilder.Build(
            [
                CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Coupon, StartDate.AddDays(-3)),
                CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Offer, StartDate.AddDays(-4)),
                CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Maturity, EndDate.AddDays(2)),
                CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Coupon, EndDate.AddDays(8))
            ],
            [Issue("SU26238RMFS4")],
            [],
            StartDate,
            EndDate,
            new OfzCashflowContextOptions { EventWindowDays = 3, UpcomingEventCount = 10, UpcomingLookAheadDays = 30 });

        Assert.Equal(2, context.Events.Count);
        Assert.Contains(context.Events, item => item.EventDate == StartDate.AddDays(-3));
        Assert.Contains(context.Events, item => item.EventDate == EndDate.AddDays(2));

        var calendar = Assert.Single(context.IssueCalendars);
        Assert.Equal(2, calendar.NearPeriodEvents.Count);
        Assert.Empty(calendar.PeriodEvents);
        Assert.DoesNotContain(calendar.NearPeriodEvents, item => item.EventDate == StartDate.AddDays(-4));
    }

    [Fact]
    public void Build_PreservesMissingNumericFieldsAndAddsLimitation()
    {
        var context = OfzCashflowContextBuilder.Build(
            [CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Coupon, StartDate, value: null, valueRub: null, valuePercent: null)],
            [Issue("SU26238RMFS4")],
            [],
            StartDate,
            EndDate);

        var cashflowEvent = Assert.Single(context.Events);
        Assert.Null(cashflowEvent.Value);
        Assert.Null(cashflowEvent.ValueRub);
        Assert.Null(cashflowEvent.ValuePercent);
        Assert.Contains(context.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.MissingCashflowField);
    }

    [Fact]
    public void Build_FillsRubValueFromValueWhenValueRubMissing()
    {
        var context = OfzCashflowContextBuilder.Build(
            [CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Coupon, StartDate, value: 12.34, valueRub: null, valuePercent: 6.1, faceUnit: "SUR")],
            [Issue("SU26238RMFS4")],
            [],
            StartDate,
            EndDate);

        var cashflowEvent = Assert.Single(context.Events);
        Assert.Equal(12.34, cashflowEvent.Value);
        Assert.Equal(12.34, cashflowEvent.ValueRub);
    }

    [Fact]
    public void Build_LinksIssueActivityInsideConfiguredWindowOnly()
    {
        var context = OfzCashflowContextBuilder.Build(
            [CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Coupon, EndDate)],
            [Issue("SU26238RMFS4")],
            [
                ActivityMetric("SU26238RMFS4", EndDate.AddDays(-3), 5, 900_000_000, 120),
                ActivityMetric("SU26238RMFS4", EndDate.AddDays(-4), 8, 1_200_000_000, 140)
            ],
            StartDate,
            EndDate,
            new OfzCashflowContextOptions { EventWindowDays = 3 });

        var link = Assert.Single(context.ActivityLinks);
        Assert.Equal(EndDate.AddDays(-3), link.TradeDate);
        Assert.Equal(3, link.DaysToEvent);
        Assert.Equal(OfzCashflowEventType.Coupon, link.Event.EventType);
    }

    private static OfzIssue Issue(string secId)
    {
        return new OfzIssue
        {
            SecId = secId,
            ShortName = "ОФЗ 26238",
            SecName = "ОФЗ 26238",
            BondType = "Фикс с известным купоном",
            FaceUnit = "SUR",
            CurrencyId = "SUR"
        };
    }

    private static OfzCashflowEvent CashflowEvent(
        string secId,
        OfzCashflowEventType eventType,
        DateTime eventDate,
        double? value = 10,
        double? valueRub = 10,
        double? valuePercent = 5,
        string? faceUnit = null)
    {
        return new OfzCashflowEvent
        {
            SecId = secId,
            SourceKey = $"{eventType}:{eventDate:yyyy-MM-dd}",
            ShortName = "ОФЗ 26238",
            EventType = eventType,
            EventDate = eventDate,
            Value = value,
            ValueRub = valueRub,
            ValuePercent = valuePercent,
            FaceUnit = faceUnit,
            SourceKind = OfzCashflowSourceKind.Schedule,
            LoadedAt = new DateTime(2026, 05, 08, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    private static OfzActivityMetric ActivityMetric(
        string secId,
        DateTime tradeDate,
        double activityScore,
        double value,
        int numTrades)
    {
        return new OfzActivityMetric
        {
            SecId = secId,
            TradeDate = tradeDate,
            Value = value,
            ActivityScore = activityScore,
            BaselineMedianValue = 100_000_000,
            BaselineDays = 10,
            NumTrades = numTrades,
            YieldValue = 12.5,
            PreviousYieldValue = 12.4,
            YieldMove = 0.1,
            Duration = 900,
            Status = OfzActivityMetricStatus.Ready
        };
    }
}
