namespace CurveAnalyzer.Core;

public static class OfzActivityAnalyzer
{
    public const int DefaultBaselineWindow = 20;
    public const int DefaultMinimumBaselineDays = 10;
    public const double DefaultMinimumBaselineMedianValue = 10_000_000;

    public static IReadOnlyList<OfzActivityMetric> CalculateMetrics(
        IEnumerable<OfzDailyTrade> trades,
        int baselineWindow = DefaultBaselineWindow,
        int minimumBaselineDays = DefaultMinimumBaselineDays,
        double minimumBaselineMedianValue = DefaultMinimumBaselineMedianValue)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baselineWindow, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumBaselineDays, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumBaselineMedianValue);

        List<OfzActivityMetric> metrics = [];

        foreach (var issueTrades in trades
            .Where(trade => !string.IsNullOrWhiteSpace(trade.SecId))
            .GroupBy(trade => trade.SecId)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            List<double> previousPositiveValues = [];
            double? previousYield = null;

            foreach (var trade in issueTrades.OrderBy(trade => trade.TradeDate))
            {
                var currentYield = trade.PreferredYield;
                var value = trade.Value;
                var baselineValues = previousPositiveValues
                    .TakeLast(baselineWindow)
                    .ToArray();

                var metric = CreateMetric(
                    trade,
                    value,
                    baselineValues,
                    previousYield,
                    currentYield,
                    minimumBaselineDays,
                    minimumBaselineMedianValue);

                metrics.Add(metric);

                if (value is > 0)
                {
                    previousPositiveValues.Add(value.Value);
                }

                if (currentYield.HasValue)
                {
                    previousYield = currentYield.Value;
                }
            }
        }

        return metrics
            .OrderBy(metric => metric.SecId, StringComparer.Ordinal)
            .ThenBy(metric => metric.TradeDate)
            .ToList();
    }

    public static IReadOnlyList<OfzActivityAnomaly> GetTopAnomalies(
        IEnumerable<OfzActivityMetric> metrics,
        IEnumerable<OfzIssue> issues,
        int topCount = 50,
        double minimumBaselineMedianValue = DefaultMinimumBaselineMedianValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topCount);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumBaselineMedianValue);

        var issuesBySecId = issues
            .GroupBy(issue => issue.SecId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        return metrics
            .Where(metric => metric.IsRankable && metric.BaselineMedianValue >= minimumBaselineMedianValue)
            .OrderByDescending(metric => metric.ActivityScore)
            .ThenByDescending(metric => metric.Value)
            .ThenBy(metric => metric.SecId, StringComparer.Ordinal)
            .Take(topCount)
            .Select((metric, index) =>
            {
                issuesBySecId.TryGetValue(metric.SecId, out var issue);

                return new OfzActivityAnomaly
                {
                    Rank = index + 1,
                    SecId = metric.SecId,
                    ShortName = issue?.ShortName ?? metric.SecId,
                    TradeDate = metric.TradeDate,
                    Value = metric.Value!.Value,
                    NumTrades = metric.NumTrades,
                    ActivityScore = metric.ActivityScore!.Value,
                    BaselineMedianValue = metric.BaselineMedianValue,
                    BaselineDays = metric.BaselineDays,
                    YieldMove = metric.YieldMove,
                    Duration = metric.Duration,
                    CurrencyMarker = issue?.CurrencyMarker ?? string.Empty,
                    CouponType = issue?.CouponType ?? OfzCouponType.Unknown,
                    CouponTypeMarker = issue?.CouponTypeMarker ?? string.Empty,
                    TypeMarker = issue?.TypeMarker ?? string.Empty,
                    DisplayMarker = issue?.DisplayMarker ?? string.Empty,
                    Status = metric.Status
                };
            })
            .ToList();
    }

    public static IReadOnlyList<OfzActivityHeatmapCell> BuildHeatmapCells(
        IEnumerable<OfzActivityMetric> metrics,
        IEnumerable<OfzIssue> issues,
        DateTime startDate,
        DateTime endDate)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;

        if (startDate > endDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var rangeMetrics = metrics
            .Where(metric => !string.IsNullOrWhiteSpace(metric.SecId))
            .Where(metric => metric.TradeDate.Date >= startDate && metric.TradeDate.Date <= endDate)
            .GroupBy(metric => new { metric.SecId, TradeDate = metric.TradeDate.Date })
            .Select(group => group
                .OrderByDescending(metric => metric.ActivityScore ?? -1)
                .ThenByDescending(metric => metric.Value ?? -1)
                .First())
            .ToList();

        var dates = rangeMetrics
            .Select(metric => metric.TradeDate.Date)
            .Distinct()
            .OrderBy(date => date)
            .ToArray();
        if (dates.Length == 0)
        {
            return [];
        }

        var issuesBySecId = issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue.SecId))
            .GroupBy(issue => issue.SecId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var metricsByKey = rangeMetrics.ToDictionary(
            metric => (metric.SecId, TradeDate: metric.TradeDate.Date));

        var orderedSecIds = rangeMetrics
            .Select(metric => metric.SecId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(secId => issuesBySecId.TryGetValue(secId, out var issue) ? issue.ShortName : secId, StringComparer.CurrentCulture)
            .ThenBy(secId => secId, StringComparer.Ordinal)
            .ToArray();

        List<OfzActivityHeatmapCell> cells = [];
        foreach (var secId in orderedSecIds)
        {
            issuesBySecId.TryGetValue(secId, out var issue);
            var shortName = string.IsNullOrWhiteSpace(issue?.ShortName)
                ? secId
                : issue.ShortName;

            foreach (var date in dates)
            {
                cells.Add(metricsByKey.TryGetValue((secId, date), out var metric)
                    ? CreateHeatmapCell(metric, issue, shortName)
                    : new OfzActivityHeatmapCell
                    {
                        SecId = secId,
                        ShortName = shortName,
                        CouponType = issue?.CouponType ?? OfzCouponType.Unknown,
                        CouponTypeMarker = issue?.CouponTypeMarker ?? string.Empty,
                        TradeDate = date,
                        ScoreBucket = 0,
                        Status = OfzActivityMetricStatus.NoData
                    });
            }
        }

        return cells;
    }

    public static OfzIssueDetail BuildIssueDetail(
        string secId,
        IEnumerable<OfzDailyTrade> trades,
        OfzIssue? issue = null)
    {
        if (string.IsNullOrWhiteSpace(secId))
        {
            throw new ArgumentException("SECID is required.", nameof(secId));
        }

        var points = trades
            .Where(trade => string.Equals(trade.SecId, secId, StringComparison.Ordinal))
            .OrderBy(trade => trade.TradeDate)
            .Select(trade => new OfzIssueDetailPoint
            {
                SecId = trade.SecId,
                TradeDate = trade.TradeDate.Date,
                Value = trade.Value,
                NumTrades = trade.NumTrades,
                Price = trade.PreferredPrice,
                Yield = trade.PreferredYield
            })
            .ToList();

        return new OfzIssueDetail
        {
            SecId = secId,
            ShortName = string.IsNullOrWhiteSpace(issue?.ShortName) ? secId : issue.ShortName,
            DisplayMarker = issue?.DisplayMarker ?? string.Empty,
            MatDate = issue?.MatDate,
            Points = points
        };
    }

    public static IReadOnlyList<OfzActivityIndexPoint> BuildActivityIndex(
        IEnumerable<OfzActivityMetric> metrics)
    {
        return metrics
            .Where(metric => !string.IsNullOrWhiteSpace(metric.SecId))
            .Where(metric => metric.Value is > 0)
            .GroupBy(metric => metric.TradeDate.Date)
            .Select(group =>
            {
                var rankableMetrics = group
                    .Where(metric => metric.IsRankable)
                    .ToArray();
                var rankableActivityScores = rankableMetrics
                    .Select(metric => metric.ActivityScore!.Value)
                    .ToArray();

                return new OfzActivityIndexPoint
                {
                    TradeDate = group.Key,
                    TotalValue = group.Sum(metric => metric.Value ?? 0),
                    TotalNumTrades = group.Sum(metric => metric.NumTrades is > 0 ? metric.NumTrades.Value : 0),
                    ActiveIssueCount = group.Select(metric => metric.SecId).Distinct(StringComparer.Ordinal).Count(),
                    RankableIssueCount = rankableMetrics.Select(metric => metric.SecId).Distinct(StringComparer.Ordinal).Count(),
                    MedianActivityScore = rankableActivityScores.Length > 0 ? Median(rankableActivityScores) : null
                };
            })
            .OrderBy(point => point.TradeDate)
            .ToList();
    }

    public static IReadOnlyList<OfzDurationYieldScatterPoint> BuildDurationYieldScatter(
        IEnumerable<OfzActivityMetric> metrics,
        IEnumerable<OfzIssue> issues,
        DateTime? tradeDate = null,
        int maxPoints = 80,
        double minimumActivityScore = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPoints);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumActivityScore);

        var issuesBySecId = issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue.SecId))
            .GroupBy(issue => issue.SecId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var scatterMetrics = metrics
            .Where(metric => !tradeDate.HasValue || metric.TradeDate.Date == tradeDate.Value.Date)
            .Where(metric => metric.IsRankable)
            .Where(metric => metric.ActivityScore >= minimumActivityScore)
            .Where(metric => metric.Duration is > 0)
            .Where(metric => metric.YieldValue is > 0)
            .Where(metric => metric.Value is > 0)
            .GroupBy(metric => metric.SecId, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(metric => metric.ActivityScore)
                .ThenByDescending(metric => metric.Value)
                .ThenByDescending(metric => metric.TradeDate)
                .First())
            .OrderByDescending(metric => metric.ActivityScore)
            .ThenByDescending(metric => metric.Value)
            .ThenBy(metric => metric.SecId, StringComparer.Ordinal)
            .Take(maxPoints);

        return scatterMetrics
            .Select(metric =>
            {
                var issue = GetIssue(issuesBySecId, metric.SecId);

                return new OfzDurationYieldScatterPoint
                {
                    SecId = metric.SecId,
                    ShortName = GetShortName(issue, metric.SecId),
                    DisplayMarker = issue?.DisplayMarker ?? string.Empty,
                    CouponType = issue?.CouponType ?? OfzCouponType.Unknown,
                    CouponTypeMarker = issue?.CouponTypeMarker ?? string.Empty,
                    TradeDate = metric.TradeDate,
                    DurationDays = metric.Duration!.Value,
                    Yield = metric.YieldValue!.Value,
                    Value = metric.Value!.Value,
                    NumTrades = metric.NumTrades,
                    ActivityScore = metric.ActivityScore!.Value,
                    ScoreBucket = GetScoreBucket(metric),
                    Status = metric.Status
                };
            })
            .OrderBy(point => point.DurationDays)
            .ThenBy(point => point.Yield)
            .ToList();
    }

    public static IReadOnlyList<OfzActivityInsight> BuildActivityInsights(
        IEnumerable<OfzActivityMetric> metrics,
        IEnumerable<OfzIssue> issues,
        int maxInsights = 6,
        double minimumInterestingScore = 2,
        double minimumYieldMove = 0.1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxInsights);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumInterestingScore);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumYieldMove);

        var issuesBySecId = issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue.SecId))
            .GroupBy(issue => issue.SecId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var interestingMetrics = metrics
            .Where(metric => metric.ActivityScore is not null && metric.ActivityScore.Value >= minimumInterestingScore)
            .Where(metric => metric.Value is > 0)
            .OrderByDescending(metric => metric.ActivityScore)
            .ThenByDescending(metric => metric.Value)
            .ToList();

        if (interestingMetrics.Count == 0)
        {
            return [];
        }

        List<OfzActivityInsight> insights = [];
        AddMarketWideActivityInsight(insights, interestingMetrics);
        AddRepeatedIssueActivityInsight(insights, interestingMetrics, issuesBySecId);
        AddYieldMoveActivityInsight(insights, interestingMetrics, issuesBySecId, minimumYieldMove);
        AddBlockLikeActivityInsight(insights, interestingMetrics, issuesBySecId);
        AddTradeCountActivityInsight(insights, interestingMetrics, issuesBySecId);
        AddCouponTypeConcentrationInsight(insights, interestingMetrics, issuesBySecId);

        return insights
            .OrderByDescending(insight => insight.Severity)
            .ThenBy(insight => insight.Kind)
            .Take(maxInsights)
            .ToList();
    }

    private static OfzActivityMetric CreateMetric(
        OfzDailyTrade trade,
        double? value,
        IReadOnlyList<double> baselineValues,
        double? previousYield,
        double? currentYield,
        int minimumBaselineDays,
        double minimumBaselineMedianValue)
    {
        if (value is null or <= 0)
        {
            return CreateMetric(
                trade,
                value,
                baselineValues.Count,
                null,
                null,
                previousYield,
                currentYield,
                OfzActivityMetricStatus.MissingValue);
        }

        if (baselineValues.Count == 0)
        {
            return CreateMetric(
                trade,
                value,
                0,
                null,
                null,
                previousYield,
                currentYield,
                OfzActivityMetricStatus.MissingBaseline);
        }

        if (baselineValues.Count < minimumBaselineDays)
        {
            return CreateMetric(
                trade,
                value,
                baselineValues.Count,
                Median(baselineValues),
                null,
                previousYield,
                currentYield,
                OfzActivityMetricStatus.InsufficientBaseline);
        }

        var baselineMedian = Median(baselineValues);
        if (baselineMedian < minimumBaselineMedianValue)
        {
            return CreateMetric(
                trade,
                value,
                baselineValues.Count,
                baselineMedian,
                null,
                previousYield,
                currentYield,
                OfzActivityMetricStatus.InsufficientBaseline);
        }

        var activityScore = baselineMedian > 0 ? value / baselineMedian : null;
        var status = currentYield.HasValue && previousYield.HasValue
            ? OfzActivityMetricStatus.Ready
            : OfzActivityMetricStatus.MissingYield;

        return CreateMetric(
            trade,
            value,
            baselineValues.Count,
            baselineMedian,
            activityScore,
            previousYield,
            currentYield,
            status);
    }

    private static OfzActivityMetric CreateMetric(
        OfzDailyTrade trade,
        double? value,
        int baselineDays,
        double? baselineMedian,
        double? activityScore,
        double? previousYield,
        double? currentYield,
        OfzActivityMetricStatus status)
    {
        return new OfzActivityMetric
        {
            SecId = trade.SecId,
            TradeDate = trade.TradeDate.Date,
            Value = value,
            BaselineMedianValue = baselineMedian,
            BaselineDays = baselineDays,
            ActivityScore = activityScore,
            NumTrades = trade.NumTrades,
            YieldValue = currentYield,
            PreviousYieldValue = previousYield,
            YieldMove = currentYield.HasValue && previousYield.HasValue
                ? currentYield.Value - previousYield.Value
                : null,
            Duration = trade.Duration,
            Status = status
        };
    }

    private static OfzActivityHeatmapCell CreateHeatmapCell(OfzActivityMetric metric, OfzIssue? issue, string shortName)
    {
        return new OfzActivityHeatmapCell
        {
            SecId = metric.SecId,
            ShortName = shortName,
            CouponType = issue?.CouponType ?? OfzCouponType.Unknown,
            CouponTypeMarker = issue?.CouponTypeMarker ?? string.Empty,
            TradeDate = metric.TradeDate.Date,
            ActivityScore = metric.ActivityScore,
            ScoreBucket = GetScoreBucket(metric),
            Value = metric.Value,
            YieldMove = metric.YieldMove,
            Status = metric.Status
        };
    }

    private static void AddMarketWideActivityInsight(
        ICollection<OfzActivityInsight> insights,
        IReadOnlyList<OfzActivityMetric> metrics)
    {
        var dateGroup = metrics
            .GroupBy(metric => metric.TradeDate.Date)
            .Select(group => new
            {
                TradeDate = group.Key,
                Count = group.Count(),
                TotalValue = group.Sum(metric => metric.Value ?? 0),
                MaxScore = group.Max(metric => metric.ActivityScore ?? 0)
            })
            .Where(group => group.Count >= 3)
            .OrderByDescending(group => group.Count)
            .ThenByDescending(group => group.TotalValue)
            .FirstOrDefault();

        if (dateGroup is null)
        {
            return;
        }

        insights.Add(new OfzActivityInsight
        {
            Kind = OfzActivityInsightKind.MarketWideActivity,
            Severity = dateGroup.Count,
            Title = $"Активная дата {dateGroup.TradeDate:dd.MM.yyyy}",
            Text = $"{dateGroup.Count} выпусков имели activity score выше порога; суммарный оборот {dateGroup.TotalValue:N0} RUB.",
            TradeDate = dateGroup.TradeDate,
            ActivityScore = dateGroup.MaxScore,
            Value = dateGroup.TotalValue
        });
    }

    private static void AddRepeatedIssueActivityInsight(
        ICollection<OfzActivityInsight> insights,
        IReadOnlyList<OfzActivityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var repeated = metrics
            .GroupBy(metric => metric.SecId, StringComparer.Ordinal)
            .Select(group => new
            {
                SecId = group.Key,
                Count = group.Count(),
                MaxMetric = group.OrderByDescending(metric => metric.ActivityScore).First()
            })
            .Where(group => group.Count >= 2)
            .OrderByDescending(group => group.Count)
            .ThenByDescending(group => group.MaxMetric.ActivityScore)
            .FirstOrDefault();

        if (repeated is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, repeated.SecId);
        insights.Add(new OfzActivityInsight
        {
            Kind = OfzActivityInsightKind.RepeatedIssueActivity,
            Severity = repeated.Count + 2,
            Title = "Повторяющиеся всплески выпуска",
            Text = $"{GetShortName(issue, repeated.SecId)} появлялся в активных днях {repeated.Count} раза; максимальный score {repeated.MaxMetric.ActivityScore:N2}.",
            TradeDate = repeated.MaxMetric.TradeDate,
            SecId = repeated.SecId,
            ShortName = GetShortName(issue, repeated.SecId),
            CouponType = issue?.CouponType ?? OfzCouponType.Unknown,
            CouponTypeMarker = issue?.CouponTypeMarker ?? string.Empty,
            ActivityScore = repeated.MaxMetric.ActivityScore,
            Value = repeated.MaxMetric.Value,
            NumTrades = repeated.MaxMetric.NumTrades,
            YieldMove = repeated.MaxMetric.YieldMove
        });
    }

    private static void AddYieldMoveActivityInsight(
        ICollection<OfzActivityInsight> insights,
        IReadOnlyList<OfzActivityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        double minimumYieldMove)
    {
        var metric = metrics
            .Where(metric => Math.Abs(metric.YieldMove ?? 0) >= minimumYieldMove)
            .OrderByDescending(metric => Math.Abs(metric.YieldMove ?? 0))
            .ThenByDescending(metric => metric.ActivityScore)
            .FirstOrDefault();

        if (metric is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, metric.SecId);
        insights.Add(new OfzActivityInsight
        {
            Kind = OfzActivityInsightKind.YieldMoveActivity,
            Severity = 5,
            Title = "Всплеск с движением доходности",
            Text = $"{GetShortName(issue, metric.SecId)}: score {metric.ActivityScore:N2}, yield Δ {metric.YieldMove:N2} п.п. на {metric.TradeDate:dd.MM.yyyy}.",
            TradeDate = metric.TradeDate,
            SecId = metric.SecId,
            ShortName = GetShortName(issue, metric.SecId),
            CouponType = issue?.CouponType ?? OfzCouponType.Unknown,
            CouponTypeMarker = issue?.CouponTypeMarker ?? string.Empty,
            ActivityScore = metric.ActivityScore,
            Value = metric.Value,
            NumTrades = metric.NumTrades,
            YieldMove = metric.YieldMove
        });
    }

    private static void AddBlockLikeActivityInsight(
        ICollection<OfzActivityInsight> insights,
        IReadOnlyList<OfzActivityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var metric = metrics
            .Where(metric => metric.Value >= 1_000_000_000)
            .Where(metric => metric.NumTrades is > 0 and <= 250)
            .OrderByDescending(metric => metric.Value!.Value / metric.NumTrades!.Value)
            .FirstOrDefault();

        if (metric is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, metric.SecId);
        insights.Add(new OfzActivityInsight
        {
            Kind = OfzActivityInsightKind.BlockLikeActivity,
            Severity = 4,
            Title = "Крупный оборот при малом числе сделок",
            Text = $"{GetShortName(issue, metric.SecId)}: оборот {metric.Value:N0} RUB при {metric.NumTrades:N0} сделках.",
            TradeDate = metric.TradeDate,
            SecId = metric.SecId,
            ShortName = GetShortName(issue, metric.SecId),
            CouponType = issue?.CouponType ?? OfzCouponType.Unknown,
            CouponTypeMarker = issue?.CouponTypeMarker ?? string.Empty,
            ActivityScore = metric.ActivityScore,
            Value = metric.Value,
            NumTrades = metric.NumTrades,
            YieldMove = metric.YieldMove
        });
    }

    private static void AddTradeCountActivityInsight(
        ICollection<OfzActivityInsight> insights,
        IReadOnlyList<OfzActivityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var metric = metrics
            .Where(metric => metric.NumTrades >= 1_000)
            .OrderByDescending(metric => metric.NumTrades)
            .ThenByDescending(metric => metric.ActivityScore)
            .FirstOrDefault();

        if (metric is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, metric.SecId);
        insights.Add(new OfzActivityInsight
        {
            Kind = OfzActivityInsightKind.TradeCountActivity,
            Severity = 3,
            Title = "Широкая торговая активность",
            Text = $"{GetShortName(issue, metric.SecId)}: {metric.NumTrades:N0} сделок, score {metric.ActivityScore:N2}.",
            TradeDate = metric.TradeDate,
            SecId = metric.SecId,
            ShortName = GetShortName(issue, metric.SecId),
            CouponType = issue?.CouponType ?? OfzCouponType.Unknown,
            CouponTypeMarker = issue?.CouponTypeMarker ?? string.Empty,
            ActivityScore = metric.ActivityScore,
            Value = metric.Value,
            NumTrades = metric.NumTrades,
            YieldMove = metric.YieldMove
        });
    }

    private static void AddCouponTypeConcentrationInsight(
        ICollection<OfzActivityInsight> insights,
        IReadOnlyList<OfzActivityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var typedMetrics = metrics
            .Select(metric => new { Metric = metric, Issue = GetIssue(issuesBySecId, metric.SecId) })
            .Where(item => item.Issue?.CouponType is not null and not OfzCouponType.Unknown)
            .ToList();
        if (typedMetrics.Count < 2)
        {
            return;
        }

        var group = typedMetrics
            .GroupBy(item => item.Issue!.CouponType)
            .Select(group => new
            {
                CouponType = group.Key,
                CouponTypeMarker = group.First().Issue!.CouponTypeMarker,
                Count = group.Count(),
                TotalValue = group.Sum(item => item.Metric.Value ?? 0)
            })
            .Where(group => group.Count >= 2 && group.Count >= Math.Ceiling(typedMetrics.Count / 2d))
            .OrderByDescending(group => group.Count)
            .ThenByDescending(group => group.TotalValue)
            .FirstOrDefault();

        if (group is null)
        {
            return;
        }

        insights.Add(new OfzActivityInsight
        {
            Kind = OfzActivityInsightKind.CouponTypeConcentration,
            Severity = group.Count + 1,
            Title = $"Концентрация в сегменте {group.CouponTypeMarker}",
            Text = $"{group.Count} активных записей относятся к типу {group.CouponTypeMarker}; суммарный оборот {group.TotalValue:N0} RUB.",
            CouponType = group.CouponType,
            CouponTypeMarker = group.CouponTypeMarker,
            Value = group.TotalValue
        });
    }

    private static OfzIssue? GetIssue(IReadOnlyDictionary<string, OfzIssue> issuesBySecId, string secId)
    {
        return issuesBySecId.TryGetValue(secId, out var issue)
            ? issue
            : null;
    }

    private static string GetShortName(OfzIssue? issue, string secId)
    {
        return string.IsNullOrWhiteSpace(issue?.ShortName)
            ? secId
            : issue.ShortName;
    }

    private static int GetScoreBucket(OfzActivityMetric metric)
    {
        if (!metric.ActivityScore.HasValue)
        {
            return metric.Status == OfzActivityMetricStatus.NoData ? 0 : 1;
        }

        var score = metric.ActivityScore.Value;
        return score switch
        {
            >= 10 => 5,
            >= 5 => 4,
            >= 2 => 3,
            >= 1 => 2,
            _ => 1
        };
    }

    private static double Median(IReadOnlyCollection<double> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;

        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2
            : ordered[middle];
    }
}
