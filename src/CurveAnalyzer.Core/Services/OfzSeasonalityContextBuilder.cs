namespace CurveAnalyzer.Core;

public sealed class OfzSeasonalityContextOptions
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime? InsightStartDate { get; init; }
    public DateTime? InsightEndDate { get; init; }
    public OfzSummarySignalScope SignalScope { get; init; } = OfzSummarySignalScope.AllDays;
    public int MinWeekdayBaselineObservations { get; init; } = 4;
    public int MinMonthBaselineObservations { get; init; } = 3;
    public double HighActivityRatioThreshold { get; init; } = 1.5;
    public double LowActivityRatioThreshold { get; init; } = 0.67;
    public double UnchangedYieldMoveThreshold { get; init; } = 0.01;
}

public static class OfzSeasonalityContextBuilder
{
    public static OfzSeasonalityContext Build(
        IEnumerable<OfzActivityMetric> metrics,
        OfzSeasonalityContextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        options ??= new OfzSeasonalityContextOptions();
        Validate(options);

        var observations = BuildObservations(metrics, options)
            .OrderBy(observation => observation.TradeDate)
            .ToList();

        var startDate = options.StartDate?.Date ?? observations.Select(item => item.TradeDate).DefaultIfEmpty(DateTime.MinValue).Min();
        var endDate = options.EndDate?.Date ?? observations.Select(item => item.TradeDate).DefaultIfEmpty(DateTime.MinValue).Max();
        var weekdayBuckets = BuildBuckets(observations, OfzSeasonalityBucketKind.Weekday, options).ToList();
        var monthBuckets = BuildBuckets(observations, OfzSeasonalityBucketKind.Month, options).ToList();
        var findings = BuildFindings(observations, options).ToList();
        var limitations = BuildLimitations(observations, weekdayBuckets, monthBuckets).ToList();

        return new OfzSeasonalityContext
        {
            StartDate = startDate,
            EndDate = endDate,
            MinWeekdayBaselineObservations = options.MinWeekdayBaselineObservations,
            MinMonthBaselineObservations = options.MinMonthBaselineObservations,
            HighActivityRatioThreshold = options.HighActivityRatioThreshold,
            LowActivityRatioThreshold = options.LowActivityRatioThreshold,
            ObservationCount = observations.Count,
            WeekdayBuckets = weekdayBuckets,
            MonthBuckets = monthBuckets,
            Findings = findings,
            Limitations = limitations
        };
    }

    private static IEnumerable<OfzSeasonalityObservation> BuildObservations(
        IEnumerable<OfzActivityMetric> metrics,
        OfzSeasonalityContextOptions options)
    {
        var startDate = options.StartDate?.Date;
        var endDate = options.EndDate?.Date;

        return metrics
            .Where(HasObservationData)
            .Where(metric => !startDate.HasValue || metric.TradeDate.Date >= startDate.Value)
            .Where(metric => !endDate.HasValue || metric.TradeDate.Date <= endDate.Value)
            .GroupBy(metric => metric.TradeDate.Date)
            .Select(group =>
            {
                var values = group
                    .Where(metric => metric.Value.HasValue)
                    .Select(metric => metric.Value!.Value)
                    .ToArray();
                var trades = group
                    .Where(metric => metric.NumTrades.HasValue)
                    .Select(metric => metric.NumTrades!.Value)
                    .ToArray();
                var comparable = group.Where(metric => metric.YieldMove.HasValue).ToArray();

                return new OfzSeasonalityObservation
                {
                    TradeDate = group.Key,
                    TotalValue = values.Length > 0 ? values.Sum() : null,
                    TotalNumTrades = trades.Length > 0 ? trades.Sum() : null,
                    ActiveIssueCount = group.Count(metric => metric.Value is > 0 || metric.NumTrades is > 0),
                    ComparableIssueCount = comparable.Length,
                    YieldUpCount = comparable.Count(metric => metric.YieldMove > options.UnchangedYieldMoveThreshold),
                    YieldDownCount = comparable.Count(metric => metric.YieldMove < -options.UnchangedYieldMoveThreshold),
                    UnchangedCount = comparable.Count(metric => Math.Abs(metric.YieldMove!.Value) <= options.UnchangedYieldMoveThreshold),
                    IsProvisional = false
                };
            });
    }

    private static IEnumerable<OfzSeasonalityBucket> BuildBuckets(
        IReadOnlyCollection<OfzSeasonalityObservation> observations,
        OfzSeasonalityBucketKind kind,
        OfzSeasonalityContextOptions options)
    {
        return observations
            .GroupBy(observation => GetBucket(observation.TradeDate, kind))
            .OrderBy(group => group.Key.SortOrder)
            .Select(group => BuildBucket(kind, group.Key, group.ToArray(), options));
    }

    private static OfzSeasonalityBucket BuildBucket(
        OfzSeasonalityBucketKind kind,
        SeasonalityBucketKey key,
        IReadOnlyCollection<OfzSeasonalityObservation> observations,
        OfzSeasonalityContextOptions options)
    {
        var values = observations
            .Where(observation => observation.TotalValue.HasValue)
            .Select(observation => observation.TotalValue!.Value)
            .ToArray();
        var trades = observations
            .Where(observation => observation.TotalNumTrades.HasValue)
            .Select(observation => (double)observation.TotalNumTrades!.Value)
            .ToArray();
        var activeIssueCounts = observations
            .Select(observation => (double)observation.ActiveIssueCount)
            .ToArray();
        var comparableBase = observations.Count(observation => observation.ComparableIssueCount > 0);
        var minBaseline = GetMinimumBaseline(kind, options);
        var quality = GetBaselineQuality(values.Length, minBaseline);
        List<OfzDataLimitation> limitations = [];

        if (quality == OfzSeasonalityBaselineQuality.Insufficient)
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.InsufficientBaseline,
                Scope = OfzSummaryScope.Seasonality,
                Text = $"{key.Label}: недостаточно наблюдений для устойчивой сезонной базы."
            });
        }

        return new OfzSeasonalityBucket
        {
            Kind = kind,
            Key = key.Key,
            Label = key.Label,
            SortOrder = key.SortOrder,
            ObservationCount = observations.Count,
            ActiveDayCount = observations.Count(observation => observation.IsActive),
            MedianTotalValue = Median(values),
            AverageTotalValue = values.Length > 0 ? values.Average() : null,
            MedianNumTrades = Median(trades),
            MedianActiveIssueCount = Median(activeIssueCounts),
            UpDayShare = comparableBase > 0 ? (double)observations.Sum(observation => observation.YieldUpCount) / observations.Sum(observation => observation.ComparableIssueCount) : null,
            DownDayShare = comparableBase > 0 ? (double)observations.Sum(observation => observation.YieldDownCount) / observations.Sum(observation => observation.ComparableIssueCount) : null,
            BaselineQuality = quality,
            Limitations = limitations
        };
    }

    private static IEnumerable<OfzSeasonalityFinding> BuildFindings(
        IReadOnlyList<OfzSeasonalityObservation> observations,
        OfzSeasonalityContextOptions options)
    {
        if (observations.Count == 0)
        {
            yield break;
        }

        var insightStartDate = options.InsightStartDate?.Date ?? observations.Min(observation => observation.TradeDate);
        var insightEndDate = options.InsightEndDate?.Date ?? observations.Max(observation => observation.TradeDate);
        if (insightStartDate > insightEndDate)
        {
            (insightStartDate, insightEndDate) = (insightEndDate, insightStartDate);
        }

        var candidates = observations
            .Where(observation => observation.TradeDate >= insightStartDate && observation.TradeDate <= insightEndDate)
            .ToList();
        if (options.SignalScope == OfzSummarySignalScope.LastAvailableDay && candidates.Count > 0)
        {
            var lastDate = candidates.Max(observation => observation.TradeDate);
            candidates = candidates.Where(observation => observation.TradeDate == lastDate).ToList();
        }

        foreach (var observation in candidates)
        {
            foreach (var kind in new[] { OfzSeasonalityBucketKind.Weekday, OfzSeasonalityBucketKind.Month })
            {
                var bucket = GetBucket(observation.TradeDate, kind);
                var previousValues = observations
                    .Where(item => item.TradeDate < observation.TradeDate)
                    .Where(item => GetBucket(item.TradeDate, kind).Key == bucket.Key)
                    .Where(item => item.TotalValue.HasValue)
                    .Select(item => item.TotalValue!.Value)
                    .ToArray();
                var minimumBaseline = GetMinimumBaseline(kind, options);
                if (previousValues.Length < minimumBaseline)
                {
                    continue;
                }

                var baselineMedianValue = Median(previousValues);
                if (!observation.TotalValue.HasValue || baselineMedianValue is null or <= 0)
                {
                    continue;
                }

                var ratio = observation.TotalValue.Value / baselineMedianValue.Value;
                var kindResult = ratio >= options.HighActivityRatioThreshold
                    ? OfzSeasonalityFindingKind.HighSeasonalActivity
                    : ratio <= options.LowActivityRatioThreshold
                        ? OfzSeasonalityFindingKind.LowSeasonalActivity
                        : (OfzSeasonalityFindingKind?)null;
                if (!kindResult.HasValue)
                {
                    continue;
                }

                var previousTrades = observations
                    .Where(item => item.TradeDate < observation.TradeDate)
                    .Where(item => GetBucket(item.TradeDate, kind).Key == bucket.Key)
                    .Where(item => item.TotalNumTrades.HasValue)
                    .Select(item => (double)item.TotalNumTrades!.Value)
                    .ToArray();

                yield return new OfzSeasonalityFinding
                {
                    Kind = kindResult.Value,
                    TradeDate = observation.TradeDate,
                    BucketKind = kind,
                    BucketKey = bucket.Key,
                    BucketLabel = bucket.Label,
                    ActualValue = observation.TotalValue,
                    BaselineMedianValue = baselineMedianValue,
                    ValueRatio = ratio,
                    ActualNumTrades = observation.TotalNumTrades,
                    BaselineMedianNumTrades = Median(previousTrades),
                    BaselineObservationCount = previousValues.Length
                };
            }
        }
    }

    private static IEnumerable<OfzDataLimitation> BuildLimitations(
        IReadOnlyCollection<OfzSeasonalityObservation> observations,
        IReadOnlyCollection<OfzSeasonalityBucket> weekdayBuckets,
        IReadOnlyCollection<OfzSeasonalityBucket> monthBuckets)
    {
        if (observations.Count == 0)
        {
            yield return new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.NoData,
                Scope = OfzSummaryScope.Seasonality,
                Text = "Нет activity metrics для построения сезонного контекста."
            };
            yield break;
        }

        if (weekdayBuckets.Concat(monthBuckets).All(bucket => bucket.BaselineQuality == OfzSeasonalityBaselineQuality.Insufficient))
        {
            yield return new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.InsufficientBaseline,
                Scope = OfzSummaryScope.Seasonality,
                Text = "Недостаточно истории для устойчивого сезонного baseline."
            };
        }
    }

    private static bool HasObservationData(OfzActivityMetric metric)
    {
        return metric.Value.HasValue ||
            metric.NumTrades.HasValue ||
            metric.YieldMove.HasValue ||
            metric.ActivityScore.HasValue;
    }

    private static int GetMinimumBaseline(
        OfzSeasonalityBucketKind kind,
        OfzSeasonalityContextOptions options)
    {
        return kind == OfzSeasonalityBucketKind.Weekday
            ? options.MinWeekdayBaselineObservations
            : options.MinMonthBaselineObservations;
    }

    private static OfzSeasonalityBaselineQuality GetBaselineQuality(int observationCount, int minimumBaseline)
    {
        if (observationCount < minimumBaseline)
        {
            return OfzSeasonalityBaselineQuality.Insufficient;
        }

        return observationCount >= minimumBaseline * 2
            ? OfzSeasonalityBaselineQuality.Strong
            : OfzSeasonalityBaselineQuality.Weak;
    }

    private static SeasonalityBucketKey GetBucket(DateTime tradeDate, OfzSeasonalityBucketKind kind)
    {
        return kind == OfzSeasonalityBucketKind.Weekday
            ? GetWeekdayBucket(tradeDate.DayOfWeek)
            : GetMonthBucket(tradeDate.Month);
    }

    private static SeasonalityBucketKey GetWeekdayBucket(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => new("Monday", "Пн", 1),
            DayOfWeek.Tuesday => new("Tuesday", "Вт", 2),
            DayOfWeek.Wednesday => new("Wednesday", "Ср", 3),
            DayOfWeek.Thursday => new("Thursday", "Чт", 4),
            DayOfWeek.Friday => new("Friday", "Пт", 5),
            DayOfWeek.Saturday => new("Saturday", "Сб", 6),
            _ => new("Sunday", "Вс", 7)
        };
    }

    private static SeasonalityBucketKey GetMonthBucket(int month)
    {
        return month switch
        {
            1 => new("January", "Январь", 1),
            2 => new("February", "Февраль", 2),
            3 => new("March", "Март", 3),
            4 => new("April", "Апрель", 4),
            5 => new("May", "Май", 5),
            6 => new("June", "Июнь", 6),
            7 => new("July", "Июль", 7),
            8 => new("August", "Август", 8),
            9 => new("September", "Сентябрь", 9),
            10 => new("October", "Октябрь", 10),
            11 => new("November", "Ноябрь", 11),
            _ => new("December", "Декабрь", 12)
        };
    }

    private static double? Median(double[] values)
    {
        if (values.Length == 0)
        {
            return null;
        }

        Array.Sort(values);
        var middle = values.Length / 2;
        return values.Length % 2 == 0
            ? (values[middle - 1] + values[middle]) / 2
            : values[middle];
    }

    private static void Validate(OfzSeasonalityContextOptions options)
    {
        if (options.StartDate.HasValue &&
            options.EndDate.HasValue &&
            options.StartDate.Value.Date > options.EndDate.Value.Date)
        {
            throw new ArgumentException("StartDate must be less than or equal to EndDate.", nameof(options));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MinWeekdayBaselineObservations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MinMonthBaselineObservations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.HighActivityRatioThreshold);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.LowActivityRatioThreshold);
        ArgumentOutOfRangeException.ThrowIfNegative(options.UnchangedYieldMoveThreshold);

        if (options.LowActivityRatioThreshold >= options.HighActivityRatioThreshold)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "LowActivityRatioThreshold must be less than HighActivityRatioThreshold.");
        }
    }

    private readonly record struct SeasonalityBucketKey(string Key, string Label, int SortOrder);
}
