namespace CurveAnalyzer.Core;

public sealed class OfzCashflowContextOptions
{
    public int EventWindowDays { get; init; } = 3;
    public int UpcomingEventCount { get; init; } = 12;
    public int UpcomingLookAheadDays { get; init; } = 370;
}

public static class OfzCashflowContextBuilder
{
    public static OfzCashflowContext Build(
        IEnumerable<OfzCashflowEvent> events,
        IEnumerable<OfzIssue> issues,
        IEnumerable<OfzActivityMetric> activityMetrics,
        DateTime startDate,
        DateTime endDate,
        OfzCashflowContextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(activityMetrics);
        options ??= new OfzCashflowContextOptions();
        Validate(startDate, endDate, options);

        var rangeStart = startDate.Date;
        var rangeEnd = endDate.Date;
        var issuesBySecId = issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue.SecId))
            .GroupBy(issue => issue.SecId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var secIds = issuesBySecId.Keys.ToHashSet(StringComparer.Ordinal);
        var orderedEvents = NormalizeEvents(events)
            .Where(item => secIds.Contains(item.SecId))
            .ToList();
        var contextStart = rangeStart.AddDays(-options.EventWindowDays);
        var contextEnd = rangeEnd.AddDays(options.EventWindowDays);
        var upcomingEnd = rangeEnd.AddDays(options.UpcomingLookAheadDays);
        var contextEvents = orderedEvents
            .Where(item => item.EventDate.Date >= contextStart && item.EventDate.Date <= contextEnd)
            .ToList();
        var upcomingEvents = orderedEvents
            .Where(item => item.EventDate.Date >= rangeEnd && item.EventDate.Date <= upcomingEnd)
            .OrderBy(item => item.EventDate)
            .ThenBy(item => GetEventTypePriority(item.EventType))
            .ThenBy(item => item.SecId, StringComparer.Ordinal)
            .Take(options.UpcomingEventCount)
            .ToList();
        var calendars = BuildCalendars(issuesBySecId, orderedEvents, rangeStart, rangeEnd, options);
        var links = BuildActivityLinks(activityMetrics, contextEvents, issuesBySecId, options);
        var limitations = BuildLimitations(issuesBySecId, orderedEvents, contextEvents);

        return new OfzCashflowContext
        {
            StartDate = rangeStart,
            EndDate = rangeEnd,
            EventWindowDays = options.EventWindowDays,
            IssueCalendars = calendars,
            Events = contextEvents,
            UpcomingEvents = upcomingEvents,
            ActivityLinks = links,
            Limitations = limitations
        };
    }

    private static IReadOnlyList<OfzIssueCashflowCalendar> BuildCalendars(
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        IReadOnlyList<OfzCashflowEvent> events,
        DateTime startDate,
        DateTime endDate,
        OfzCashflowContextOptions options)
    {
        var nearStart = startDate.AddDays(-options.EventWindowDays);
        var nearEnd = endDate.AddDays(options.EventWindowDays);

        return issuesBySecId.Values
            .Select(issue =>
            {
                var issueEvents = events
                    .Where(item => string.Equals(item.SecId, issue.SecId, StringComparison.Ordinal))
                    .OrderBy(item => item.EventDate)
                    .ThenBy(item => GetEventTypePriority(item.EventType))
                    .ThenBy(item => item.SourceKey, StringComparer.Ordinal)
                    .ToList();
                var periodEvents = issueEvents
                    .Where(item => item.EventDate.Date >= startDate && item.EventDate.Date <= endDate)
                    .ToList();
                var nearPeriodEvents = issueEvents
                    .Where(item => item.EventDate.Date >= nearStart && item.EventDate.Date <= nearEnd)
                    .ToList();

                return new OfzIssueCashflowCalendar
                {
                    SecId = issue.SecId,
                    ShortName = string.IsNullOrWhiteSpace(issue.ShortName) ? issue.SecId : issue.ShortName,
                    CouponType = issue.CouponType,
                    CouponTypeMarker = issue.CouponTypeMarker,
                    Events = issueEvents,
                    PeriodEvents = periodEvents,
                    NearPeriodEvents = nearPeriodEvents,
                    PreviousEvent = issueEvents.LastOrDefault(item => item.EventDate.Date < startDate),
                    NextEvent = issueEvents.FirstOrDefault(item => item.EventDate.Date >= endDate),
                    Limitations = issueEvents.Count == 0
                        ? [CreateLimitation(OfzDataLimitationKind.NoCashflowData, issue.SecId, null, "Для выпуска нет календарных событий в cache.")]
                        : []
                };
            })
            .Where(calendar => calendar.HasEvents || calendar.Limitations.Count > 0)
            .ToList();
    }

    private static IReadOnlyList<OfzCashflowActivityLink> BuildActivityLinks(
        IEnumerable<OfzActivityMetric> activityMetrics,
        IReadOnlyList<OfzCashflowEvent> events,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzCashflowContextOptions options)
    {
        return activityMetrics
            .Where(metric => !string.IsNullOrWhiteSpace(metric.SecId))
            .Where(metric => metric.Value is > 0 || metric.NumTrades is > 0 || metric.ActivityScore is > 0)
            .Select(metric =>
            {
                var eventMatch = events
                    .Where(item => string.Equals(item.SecId, metric.SecId, StringComparison.Ordinal))
                    .Select(item => new
                    {
                        Event = item,
                        DaysToEvent = (int)(item.EventDate.Date - metric.TradeDate.Date).TotalDays
                    })
                    .Where(item => Math.Abs(item.DaysToEvent) <= options.EventWindowDays)
                    .OrderBy(item => Math.Abs(item.DaysToEvent))
                    .ThenBy(item => GetEventTypePriority(item.Event.EventType))
                    .ThenBy(item => item.Event.EventDate)
                    .FirstOrDefault();

                if (eventMatch is null)
                {
                    return null;
                }

                issuesBySecId.TryGetValue(metric.SecId, out var issue);
                return new OfzCashflowActivityLink
                {
                    SecId = metric.SecId,
                    ShortName = string.IsNullOrWhiteSpace(issue?.ShortName) ? metric.SecId : issue.ShortName,
                    TradeDate = metric.TradeDate.Date,
                    Event = eventMatch.Event,
                    DaysToEvent = eventMatch.DaysToEvent,
                    Value = metric.Value,
                    NumTrades = metric.NumTrades,
                    ActivityScore = metric.ActivityScore,
                    YieldMove = metric.YieldMove
                };
            })
            .OfType<OfzCashflowActivityLink>()
            .GroupBy(item => new { item.SecId, item.TradeDate, item.Event.EventType, item.Event.EventDate })
            .Select(group => group
                .OrderByDescending(item => item.ActivityScore ?? 0)
                .ThenByDescending(item => item.Value ?? 0)
                .First())
            .OrderBy(item => Math.Abs(item.DaysToEvent))
            .ThenByDescending(item => item.Value ?? 0)
            .ThenBy(item => item.SecId, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<OfzDataLimitation> BuildLimitations(
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        IReadOnlyList<OfzCashflowEvent> allEvents,
        IReadOnlyList<OfzCashflowEvent> contextEvents)
    {
        List<OfzDataLimitation> limitations = [];

        if (issuesBySecId.Count > 0 && allEvents.Count == 0)
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.NoCashflowData,
                Scope = OfzSummaryScope.Cashflow,
                Text = "Календарные события для активных выпусков отсутствуют."
            });
        }

        if (contextEvents.Any(item => item.Value is null && item.ValueRub is null && item.ValuePercent is null))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.MissingCashflowField,
                Scope = OfzSummaryScope.Cashflow,
                Text = "Часть cashflow events не имеет суммы или процента; поля не заменяются нулями."
            });
        }

        if (contextEvents.Any(item => item.SourceKind != OfzCashflowSourceKind.Schedule))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.SnapshotOnly,
                Scope = OfzSummaryScope.Cashflow,
                Text = "Часть cashflow events построена по snapshot/fallback metadata."
            });
        }

        if (contextEvents.Any(item => item.IsProvisional))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.Provisional,
                Scope = OfzSummaryScope.Cashflow,
                Text = "Часть cashflow events предварительная."
            });
        }

        return limitations;
    }

    private static IReadOnlyList<OfzCashflowEvent> NormalizeEvents(IEnumerable<OfzCashflowEvent> events)
    {
        return events
            .Where(item => !string.IsNullOrWhiteSpace(item.SecId))
            .Where(item => item.EventDate != default)
            .Select(NormalizeEvent)
            .GroupBy(item => new { item.SecId, item.EventType, item.EventDate, item.SourceKind, item.SourceKey })
            .Select(group => group.OrderByDescending(item => item.LoadedAt).First())
            .OrderBy(item => item.SecId, StringComparer.Ordinal)
            .ThenBy(item => item.EventDate)
            .ThenBy(item => GetEventTypePriority(item.EventType))
            .ThenBy(item => item.SourceKind)
            .ThenBy(item => item.SourceKey, StringComparer.Ordinal)
            .ToList();
    }

    private static OfzCashflowEvent NormalizeEvent(OfzCashflowEvent item)
    {
        var sourceKey = string.IsNullOrWhiteSpace(item.SourceKey)
            ? $"{item.EventType}:{item.EventDate:yyyy-MM-dd}:{item.SourceKind}"
            : item.SourceKey.Trim();
        var faceUnit = NormalizeText(item.FaceUnit);
        var value = NormalizeFinite(item.Value);
        var valueRub = NormalizeFinite(item.ValueRub) ?? (IsRubFaceUnit(faceUnit) ? value : null);

        return new OfzCashflowEvent
        {
            SecId = item.SecId.Trim(),
            SourceKey = sourceKey,
            ShortName = NormalizeText(item.ShortName),
            EventType = item.EventType,
            EventDate = item.EventDate.Date,
            StartDate = item.StartDate?.Date,
            EndDate = item.EndDate?.Date,
            RecordDate = item.RecordDate?.Date,
            Value = value,
            ValueRub = valueRub,
            ValuePercent = NormalizeFinite(item.ValuePercent),
            FaceValue = NormalizeFinite(item.FaceValue),
            InitialFaceValue = NormalizeFinite(item.InitialFaceValue),
            FaceUnit = faceUnit,
            Price = NormalizeFinite(item.Price),
            Agent = NormalizeText(item.Agent),
            OfferType = NormalizeText(item.OfferType),
            SourceKind = item.SourceKind,
            SourceLabel = NormalizeText(item.SourceLabel),
            LoadedAt = item.LoadedAt == default ? DateTime.UtcNow : item.LoadedAt,
            IsProvisional = item.IsProvisional,
            Limitations = item.Limitations
        };
    }

    private static int GetEventTypePriority(OfzCashflowEventType eventType)
    {
        return eventType switch
        {
            OfzCashflowEventType.Coupon => 0,
            OfzCashflowEventType.Amortization => 1,
            OfzCashflowEventType.Maturity => 2,
            OfzCashflowEventType.Offer => 3,
            OfzCashflowEventType.Buyback => 4,
            OfzCashflowEventType.CallOption => 5,
            OfzCashflowEventType.PutOption => 6,
            _ => 99
        };
    }

    private static OfzDataLimitation CreateLimitation(
        OfzDataLimitationKind kind,
        string? secId,
        DateTime? tradeDate,
        string text)
    {
        return new OfzDataLimitation
        {
            Kind = kind,
            Scope = OfzSummaryScope.Cashflow,
            SecId = secId,
            TradeDate = tradeDate,
            Text = text
        };
    }

    private static double? NormalizeFinite(double? value)
    {
        return value.HasValue && double.IsFinite(value.Value) ? value.Value : null;
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsRubFaceUnit(string? faceUnit)
    {
        return string.Equals(faceUnit, "RUB", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(faceUnit, "SUR", StringComparison.OrdinalIgnoreCase);
    }

    private static void Validate(DateTime startDate, DateTime endDate, OfzCashflowContextOptions options)
    {
        if (startDate.Date > endDate.Date)
        {
            throw new ArgumentException("StartDate must be less than or equal to EndDate.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(options.EventWindowDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.UpcomingEventCount);
        ArgumentOutOfRangeException.ThrowIfNegative(options.UpcomingLookAheadDays);
    }
}
