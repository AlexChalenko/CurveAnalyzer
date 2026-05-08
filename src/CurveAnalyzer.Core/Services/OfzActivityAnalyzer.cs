namespace CurveAnalyzer.Core;

public static class OfzActivityAnalyzer
{
    public const int DefaultBaselineWindow = 20;
    public const int DefaultMinimumBaselineDays = 10;
    public const double DefaultMinimumBaselineMedianValue = 10_000_000;
    private const double GoodLiquidityScoreLimit = 5;
    private const double NormalLiquidityScoreLimit = 20;
    private const double WeakLiquidityScoreLimit = 50;

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

    public static IReadOnlyList<OfzLiquidityMetric> CalculateLiquidityMetrics(IEnumerable<OfzDailyTrade> trades)
    {
        return trades
            .Where(trade => !string.IsNullOrWhiteSpace(trade.SecId))
            .Select(CreateLiquidityMetric)
            .OrderBy(metric => metric.SecId, StringComparer.Ordinal)
            .ThenBy(metric => metric.TradeDate)
            .ToList();
    }

    public static IReadOnlyList<OfzLiquidityMetric> CalculateSnapshotLiquidityMetrics(IEnumerable<OfzLiquiditySnapshot> snapshots)
    {
        return snapshots
            .Where(snapshot => !string.IsNullOrWhiteSpace(snapshot.SecId))
            .Select(CreateLiquidityMetric)
            .OrderBy(metric => metric.SecId, StringComparer.Ordinal)
            .ThenBy(metric => metric.TradeDate)
            .ThenBy(metric => metric.ObservedAt)
            .ToList();
    }

    public static OfzIssueLiquidityProfile BuildIssueLiquidityProfile(
        string secId,
        IEnumerable<OfzDailyTrade> trades,
        OfzLiquiditySnapshot? currentSnapshot = null,
        OfzIssue? issue = null)
    {
        if (string.IsNullOrWhiteSpace(secId))
        {
            throw new ArgumentException("SECID is required.", nameof(secId));
        }

        var historicalMetrics = CalculateLiquidityMetrics(trades)
            .Where(metric => string.Equals(metric.SecId, secId, StringComparison.Ordinal))
            .OrderBy(metric => metric.TradeDate)
            .ToList();

        var snapshotMetric = currentSnapshot is not null &&
            string.Equals(currentSnapshot.SecId, secId, StringComparison.Ordinal)
                ? CreateLiquidityMetric(currentSnapshot)
                : null;

        return new OfzIssueLiquidityProfile
        {
            SecId = secId,
            ShortName = GetShortName(issue, secId),
            DisplayMarker = issue?.DisplayMarker ?? string.Empty,
            HistoricalMetrics = historicalMetrics,
            CurrentSnapshotMetric = snapshotMetric
        };
    }

    public static IReadOnlyList<OfzWeakLiquidityItem> GetWeakLiquidityRankings(
        IEnumerable<OfzLiquidityMetric> metrics,
        IEnumerable<OfzIssue> issues,
        int topCount = 50)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topCount);

        var issuesBySecId = issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue.SecId))
            .GroupBy(issue => issue.SecId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        return metrics
            .Where(metric => !string.IsNullOrWhiteSpace(metric.SecId))
            .Where(IsWeakLiquidityCandidate)
            .OrderByDescending(GetWeakLiquidityPriority)
            .ThenByDescending(metric => metric.LiquidityScore ?? -1)
            .ThenByDescending(metric => metric.Value ?? 0)
            .ThenByDescending(metric => metric.NumTrades ?? 0)
            .ThenBy(metric => metric.SecId, StringComparer.Ordinal)
            .Take(topCount)
            .Select((metric, index) =>
            {
                var issue = GetIssue(issuesBySecId, metric.SecId);

                return new OfzWeakLiquidityItem
                {
                    Rank = index + 1,
                    SecId = metric.SecId,
                    ShortName = GetShortName(issue, metric.SecId),
                    TradeDate = metric.TradeDate,
                    CouponType = issue?.CouponType ?? OfzCouponType.Unknown,
                    CouponTypeMarker = issue?.CouponTypeMarker ?? string.Empty,
                    Spread = metric.Spread,
                    SpreadSource = metric.SpreadSource,
                    LiquidityScore = metric.LiquidityScore,
                    LiquidityBucket = metric.LiquidityBucket,
                    Status = metric.Status,
                    Value = metric.Value,
                    NumTrades = metric.NumTrades,
                    IsSnapshot = metric.IsSnapshot,
                    IsProvisional = metric.IsProvisional
                };
            })
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
        OfzIssue? issue = null,
        OfzLiquiditySnapshot? currentSnapshot = null,
        IEnumerable<CbrKeyRate>? cbrKeyRates = null)
    {
        if (string.IsNullOrWhiteSpace(secId))
        {
            throw new ArgumentException("SECID is required.", nameof(secId));
        }

        var liquidityMetrics = CalculateLiquidityMetrics(trades)
            .Where(metric => string.Equals(metric.SecId, secId, StringComparison.Ordinal))
            .GroupBy(metric => (metric.SecId, TradeDate: metric.TradeDate.Date))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(metric => metric.LiquidityScore ?? -1)
                    .ThenByDescending(metric => metric.Value ?? 0)
                    .First());

        var points = trades
            .Where(trade => string.Equals(trade.SecId, secId, StringComparison.Ordinal))
            .OrderBy(trade => trade.TradeDate)
            .Select(trade =>
            {
                liquidityMetrics.TryGetValue((trade.SecId, trade.TradeDate.Date), out var liquidityMetric);

                return new OfzIssueDetailPoint
                {
                    SecId = trade.SecId,
                    TradeDate = trade.TradeDate.Date,
                    Value = trade.Value,
                    NumTrades = trade.NumTrades,
                    Price = trade.PreferredPrice,
                    Yield = trade.PreferredYield,
                    Bid = trade.Bid,
                    Offer = trade.Offer,
                    Spread = liquidityMetric?.Spread,
                    ZSpread = trade.ZSpread,
                    ZSpreadAtWeightedAveragePrice = trade.ZSpreadAtWeightedAveragePrice,
                    ImpliedFloatingRate = trade.ImpliedFloatingRate,
                    ImpliedInflation = trade.ImpliedInflation,
                    ImpliedCbrRate = trade.ImpliedCbrRate,
                    SpreadSource = liquidityMetric?.SpreadSource ?? OfzSpreadSource.Missing,
                    LiquidityScore = liquidityMetric?.LiquidityScore,
                    LiquidityBucket = liquidityMetric?.LiquidityBucket,
                    LiquidityStatus = liquidityMetric?.Status
                };
            })
            .ToList();

        var snapshotMetric = currentSnapshot is not null &&
            string.Equals(currentSnapshot.SecId, secId, StringComparison.Ordinal)
                ? CreateLiquidityMetric(currentSnapshot)
                : null;
        var classification = issue is null
            ? null
            : OfzIssueClassifier.Classify(issue, issue.ClassificationSource);
        var hasStoredClassification = HasStoredClassification(issue);
        var couponType = hasStoredClassification ? issue!.CouponType : classification?.CouponType ?? OfzCouponType.Unknown;
        var couponTypeMarker = hasStoredClassification
            ? issue!.CouponTypeMarker
            : classification?.CouponTypeMarker ?? OfzIssueClassifier.GetCouponTypeMarker(OfzCouponType.Unknown);

        return new OfzIssueDetail
        {
            SecId = secId,
            ShortName = string.IsNullOrWhiteSpace(issue?.ShortName) ? secId : issue.ShortName,
            DisplayMarker = issue?.DisplayMarker ?? string.Empty,
            CouponType = couponType,
            CouponTypeMarker = couponTypeMarker,
            ClassificationReliability = issue?.ClassificationReliability ?? classification?.Reliability ?? OfzClassificationReliability.Unknown,
            ClassificationSource = IsKnownClassificationSource(issue?.ClassificationSource)
                ? issue!.ClassificationSource!
                : classification?.Source ?? OfzIssueClassificationSources.Unknown,
            ClassificationEvidence = IsKnownClassificationEvidence(issue?.ClassificationEvidence)
                ? issue!.ClassificationEvidence
                : classification?.Evidence,
            ClassificationLoadedAt = issue?.ClassificationLoadedAt,
            ClassificationLimitations = classification?.Limitations ?? [],
            IsIndexedNominal = issue?.IsIndexedNominal ?? classification?.IsIndexedNominal,
            IsAmortizing = issue?.IsAmortizing ?? classification?.IsAmortizing,
            NominalCurrency = !string.IsNullOrWhiteSpace(issue?.NominalCurrency)
                ? issue.NominalCurrency
                : classification?.NominalCurrency ?? "Unknown",
            MatDate = issue?.MatDate,
            Points = points,
            LiquidityMetrics = liquidityMetrics.Values
                .OrderBy(metric => metric.TradeDate)
                .ToList(),
            CurrentLiquiditySnapshot = snapshotMetric,
            SpecialContext = OfzSpecialAnalyticsBuilder.Build(couponType, points, currentSnapshot, cbrKeyRates)
        };
    }

    private static bool HasStoredClassification(OfzIssue? issue)
    {
        return issue is not null &&
            (issue.NormalizedCouponType.HasValue ||
             issue.ClassificationReliability.HasValue ||
             !string.IsNullOrWhiteSpace(issue.NormalizedTypeMarker));
    }

    private static bool IsKnownClassificationSource(string? source)
    {
        return !string.IsNullOrWhiteSpace(source) &&
            !source.Equals(OfzIssueClassificationSources.Unknown, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKnownClassificationEvidence(string? evidence)
    {
        return !string.IsNullOrWhiteSpace(evidence) &&
            !evidence.Contains("source=unknown", StringComparison.OrdinalIgnoreCase);
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
        return BuildDurationYieldScatter(metrics, issues, [], tradeDate, maxPoints, minimumActivityScore);
    }

    public static IReadOnlyList<OfzDurationYieldScatterPoint> BuildDurationYieldScatter(
        IEnumerable<OfzActivityMetric> metrics,
        IEnumerable<OfzIssue> issues,
        IEnumerable<OfzLiquidityMetric> liquidityMetrics,
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
        var liquidityByKey = liquidityMetrics
            .Where(metric => !metric.IsSnapshot)
            .GroupBy(metric => (metric.SecId, TradeDate: metric.TradeDate.Date))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(metric => metric.LiquidityScore ?? -1)
                    .First());

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
                liquidityByKey.TryGetValue((metric.SecId, metric.TradeDate.Date), out var liquidityMetric);

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
                    Spread = liquidityMetric?.Spread,
                    SpreadSource = liquidityMetric?.SpreadSource ?? OfzSpreadSource.Missing,
                    LiquidityScore = liquidityMetric?.LiquidityScore,
                    LiquidityBucket = liquidityMetric?.LiquidityBucket,
                    LiquidityStatus = liquidityMetric?.Status,
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

    public static IReadOnlyList<OfzSpreadSignal> BuildSpreadSignals(
        IEnumerable<OfzLiquidityMetric> metrics,
        IEnumerable<OfzIssue> issues,
        int maxSignals = 6)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSignals);

        var issuesBySecId = issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue.SecId))
            .GroupBy(issue => issue.SecId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var orderedMetrics = metrics
            .Where(metric => !string.IsNullOrWhiteSpace(metric.SecId))
            .OrderByDescending(metric => metric.TradeDate)
            .ThenByDescending(metric => metric.LiquidityScore ?? -1)
            .ThenByDescending(metric => metric.Value ?? 0)
            .ToList();

        if (orderedMetrics.Count == 0)
        {
            return [];
        }

        List<OfzSpreadSignal> signals = [];
        AddMissingQuotesSignal(signals, orderedMetrics, issuesBySecId);
        AddActivityWithWideSpreadSignal(signals, orderedMetrics, issuesBySecId);
        AddWideSpreadSignal(signals, orderedMetrics, issuesBySecId);
        AddImprovingSpreadSignal(signals, orderedMetrics, issuesBySecId);
        AddZSpreadContextSignal(signals, orderedMetrics, issuesBySecId);

        return signals
            .GroupBy(signal => new { signal.Kind, signal.SecId, signal.TradeDate })
            .Select(group => group.OrderByDescending(signal => signal.Severity).First())
            .OrderByDescending(signal => signal.Severity)
            .ThenBy(signal => signal.Kind)
            .Take(maxSignals)
            .ToList();
    }

    public static IReadOnlyList<OfzActivityInsight> BuildLiquidityInsights(
        IEnumerable<OfzLiquidityMetric> metrics,
        IEnumerable<OfzIssue> issues,
        int maxInsights = 4)
    {
        return BuildSpreadSignals(metrics, issues, maxInsights)
            .Select(signal => new OfzActivityInsight
            {
                Kind = OfzActivityInsightKind.LiquiditySignal,
                Severity = signal.Severity,
                Title = GetSpreadSignalTitle(signal.Kind),
                Text = signal.Text,
                TradeDate = signal.TradeDate,
                SecId = signal.SecId,
                ShortName = signal.ShortName,
                CouponType = signal.CouponType,
                CouponTypeMarker = signal.CouponTypeMarker,
                Value = signal.Value,
                NumTrades = signal.NumTrades,
                Spread = signal.Spread,
                LiquidityScore = signal.LiquidityScore,
                ZSpread = signal.ZSpread,
                ZSpreadBp = signal.ZSpreadBp,
                GSpreadBp = signal.GSpreadBp,
                LiquidityBucket = signal.LiquidityBucket,
                LiquidityStatus = signal.LiquidityStatus
            })
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

    private static OfzLiquidityMetric CreateLiquidityMetric(OfzDailyTrade trade)
    {
        var (spread, spreadSource) = ResolveSpread(trade.Spread, trade.Bid, trade.Offer);
        var liquidityScore = GetLiquidityScore(spread);

        return new OfzLiquidityMetric
        {
            SecId = trade.SecId,
            TradeDate = trade.TradeDate.Date,
            Bid = NormalizePositive(trade.Bid),
            Offer = NormalizePositive(trade.Offer),
            Spread = spread,
            SpreadSource = spreadSource,
            Value = trade.Value,
            NumTrades = trade.NumTrades,
            ZSpread = trade.ZSpread,
            ZSpreadAtWeightedAveragePrice = trade.ZSpreadAtWeightedAveragePrice,
            LiquidityScore = liquidityScore,
            LiquidityBucket = GetLiquidityBucket(liquidityScore),
            Status = spreadSource == OfzSpreadSource.Missing
                ? OfzLiquidityMetricStatus.MissingQuotes
                : OfzLiquidityMetricStatus.Ready
        };
    }

    private static OfzLiquidityMetric CreateLiquidityMetric(OfzLiquiditySnapshot snapshot)
    {
        var (spread, spreadSource) = ResolveSpread(snapshot.Spread, snapshot.Bid, snapshot.Offer);
        var liquidityScore = GetLiquidityScore(spread);

        return new OfzLiquidityMetric
        {
            SecId = snapshot.SecId,
            TradeDate = snapshot.TradeDate.Date,
            Bid = NormalizePositive(snapshot.Bid),
            Offer = NormalizePositive(snapshot.Offer),
            Spread = spread,
            SpreadSource = spreadSource,
            BidDepthTotal = NormalizeNonNegative(snapshot.BidDepthTotal),
            OfferDepthTotal = NormalizeNonNegative(snapshot.OfferDepthTotal),
            Value = snapshot.ValueToday,
            NumTrades = snapshot.NumTrades,
            ZSpread = snapshot.ZSpread,
            ZSpreadAtWeightedAveragePrice = snapshot.ZSpreadAtWeightedAveragePrice,
            ZSpreadBp = snapshot.ZSpreadBp,
            GSpreadBp = snapshot.GSpreadBp,
            LiquidityScore = liquidityScore,
            LiquidityBucket = GetLiquidityBucket(liquidityScore),
            Status = spreadSource == OfzSpreadSource.Missing
                ? OfzLiquidityMetricStatus.MissingQuotes
                : OfzLiquidityMetricStatus.SnapshotOnly,
            IsSnapshot = true,
            IsProvisional = snapshot.IsProvisional,
            ObservedAt = snapshot.ObservedAt
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

    private static bool IsWeakLiquidityCandidate(OfzLiquidityMetric metric)
    {
        return metric.LiquidityBucket is OfzLiquidityBucket.Problem or OfzLiquidityBucket.Weak ||
            metric.LiquidityBucket == OfzLiquidityBucket.MissingData &&
            metric.IsSnapshot &&
            (metric.Value is > 0 || metric.NumTrades is > 0);
    }

    private static int GetWeakLiquidityPriority(OfzLiquidityMetric metric)
    {
        return metric.LiquidityBucket switch
        {
            OfzLiquidityBucket.Problem => 3,
            OfzLiquidityBucket.Weak => 2,
            OfzLiquidityBucket.MissingData => 1,
            _ => 0
        };
    }

    private static (double? Spread, OfzSpreadSource Source) ResolveSpread(
        double? providedSpread,
        double? bid,
        double? offer)
    {
        var normalizedBid = NormalizePositive(bid);
        var normalizedOffer = NormalizePositive(offer);

        if (providedSpread is > 0)
        {
            return (providedSpread.Value, OfzSpreadSource.Provided);
        }

        if (providedSpread is 0)
        {
            return normalizedBid.HasValue && normalizedOffer.HasValue
                ? (0, OfzSpreadSource.Provided)
                : (null, OfzSpreadSource.Missing);
        }

        if (normalizedBid.HasValue && normalizedOffer.HasValue)
        {
            var calculatedSpread = normalizedOffer.Value - normalizedBid.Value;
            if (calculatedSpread >= 0)
            {
                return (calculatedSpread, OfzSpreadSource.CalculatedFromBidOffer);
            }
        }

        return (null, OfzSpreadSource.Missing);
    }

    private static double? GetLiquidityScore(double? spread)
    {
        return spread.HasValue ? spread.Value * 100 : null;
    }

    private static OfzLiquidityBucket GetLiquidityBucket(double? liquidityScore)
    {
        if (!liquidityScore.HasValue)
        {
            return OfzLiquidityBucket.MissingData;
        }

        return liquidityScore.Value switch
        {
            <= GoodLiquidityScoreLimit => OfzLiquidityBucket.Good,
            <= NormalLiquidityScoreLimit => OfzLiquidityBucket.Normal,
            <= WeakLiquidityScoreLimit => OfzLiquidityBucket.Weak,
            _ => OfzLiquidityBucket.Problem
        };
    }

    private static double? NormalizePositive(double? value)
    {
        return value is > 0 ? value : null;
    }

    private static double? NormalizeNonNegative(double? value)
    {
        return value is >= 0 ? value : null;
    }

    private static void AddMissingQuotesSignal(
        ICollection<OfzSpreadSignal> signals,
        IReadOnlyList<OfzLiquidityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var metric = metrics
            .Where(metric => metric.Status == OfzLiquidityMetricStatus.MissingQuotes)
            .Where(metric => metric.IsSnapshot)
            .Where(metric => metric.Value is > 0 || metric.NumTrades is > 0)
            .OrderByDescending(metric => metric.Value ?? 0)
            .ThenByDescending(metric => metric.NumTrades ?? 0)
            .FirstOrDefault();

        if (metric is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, metric.SecId);
        signals.Add(CreateSpreadSignal(
            OfzSpreadSignalKind.MissingQuotesOnActiveDay,
            severity: 6,
            metric,
            issue,
            $"{GetShortName(issue, metric.SecId)}: нет bid/offer при обороте {metric.Value:N0} RUB и {metric.NumTrades:N0} сделках на {metric.TradeDate:dd.MM.yyyy}."));
    }

    private static void AddActivityWithWideSpreadSignal(
        ICollection<OfzSpreadSignal> signals,
        IReadOnlyList<OfzLiquidityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var metric = metrics
            .Where(metric => metric.LiquidityBucket is OfzLiquidityBucket.Problem or OfzLiquidityBucket.Weak)
            .Where(metric => metric.Value >= 1_000_000_000 || metric.NumTrades >= 1_000)
            .OrderByDescending(metric => metric.LiquidityScore ?? -1)
            .ThenByDescending(metric => metric.Value ?? 0)
            .FirstOrDefault();

        if (metric is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, metric.SecId);
        signals.Add(CreateSpreadSignal(
            OfzSpreadSignalKind.ActivityWithWideSpread,
            severity: 5,
            metric,
            issue,
            $"{GetShortName(issue, metric.SecId)}: активная торговля при spread {metric.Spread:N3}; liquidity score {metric.LiquidityScore:N2}, оборот {metric.Value:N0} RUB на {metric.TradeDate:dd.MM.yyyy}."));
    }

    private static void AddWideSpreadSignal(
        ICollection<OfzSpreadSignal> signals,
        IReadOnlyList<OfzLiquidityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var metric = metrics
            .Where(metric => metric.LiquidityBucket is OfzLiquidityBucket.Problem or OfzLiquidityBucket.Weak)
            .OrderByDescending(metric => metric.LiquidityScore ?? -1)
            .ThenByDescending(metric => metric.Value ?? 0)
            .FirstOrDefault();

        if (metric is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, metric.SecId);
        signals.Add(CreateSpreadSignal(
            OfzSpreadSignalKind.WideSpread,
            severity: metric.LiquidityBucket == OfzLiquidityBucket.Problem ? 4 : 3,
            metric,
            issue,
            $"{GetShortName(issue, metric.SecId)}: wide spread {metric.Spread:N3}; liquidity score {metric.LiquidityScore:N2}, источник {metric.SpreadSource} на {metric.TradeDate:dd.MM.yyyy}."));
    }

    private static void AddImprovingSpreadSignal(
        ICollection<OfzSpreadSignal> signals,
        IReadOnlyList<OfzLiquidityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var candidate = metrics
            .Where(metric => metric.Spread is > 0)
            .GroupBy(metric => metric.SecId, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(metric => metric.TradeDate)
                .Take(2)
                .OrderBy(metric => metric.TradeDate)
                .ToArray())
            .Where(pair => pair.Length == 2 && pair[1].Spread < pair[0].Spread * 0.75)
            .OrderByDescending(pair => pair[0].Spread - pair[1].Spread)
            .FirstOrDefault();

        if (candidate is null)
        {
            return;
        }

        var latest = candidate[1];
        var previous = candidate[0];
        var issue = GetIssue(issuesBySecId, latest.SecId);
        signals.Add(CreateSpreadSignal(
            OfzSpreadSignalKind.ImprovingSpread,
            severity: 2,
            latest,
            issue,
            $"{GetShortName(issue, latest.SecId)}: spread снизился с {previous.Spread:N3} до {latest.Spread:N3} между {previous.TradeDate:dd.MM.yyyy} и {latest.TradeDate:dd.MM.yyyy}."));
    }

    private static void AddZSpreadContextSignal(
        ICollection<OfzSpreadSignal> signals,
        IReadOnlyList<OfzLiquidityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var metric = metrics
            .Where(metric => metric.ZSpreadBp.HasValue || metric.ZSpread.HasValue)
            .OrderByDescending(metric => Math.Abs(metric.ZSpreadBp ?? metric.ZSpread ?? 0))
            .FirstOrDefault();

        if (metric is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, metric.SecId);
        signals.Add(CreateSpreadSignal(
            OfzSpreadSignalKind.ZSpreadContext,
            severity: 1,
            metric,
            issue,
            $"{GetShortName(issue, metric.SecId)}: Z-spread context {FormatNullable(metric.ZSpreadBp ?? metric.ZSpread, "N2")} на {metric.TradeDate:dd.MM.yyyy}; spread {FormatNullable(metric.Spread, "N3")}."));
    }

    private static OfzSpreadSignal CreateSpreadSignal(
        OfzSpreadSignalKind kind,
        int severity,
        OfzLiquidityMetric metric,
        OfzIssue? issue,
        string text)
    {
        return new OfzSpreadSignal
        {
            Kind = kind,
            Severity = severity,
            SecId = metric.SecId,
            ShortName = GetShortName(issue, metric.SecId),
            TradeDate = metric.TradeDate,
            CouponType = issue?.CouponType ?? OfzCouponType.Unknown,
            CouponTypeMarker = issue?.CouponTypeMarker ?? string.Empty,
            Value = metric.Value,
            NumTrades = metric.NumTrades,
            Spread = metric.Spread,
            LiquidityScore = metric.LiquidityScore,
            LiquidityBucket = metric.LiquidityBucket,
            LiquidityStatus = metric.Status,
            ZSpread = metric.ZSpread,
            ZSpreadBp = metric.ZSpreadBp,
            GSpreadBp = metric.GSpreadBp,
            Text = text
        };
    }

    private static string GetSpreadSignalTitle(OfzSpreadSignalKind kind)
    {
        return kind switch
        {
            OfzSpreadSignalKind.WideSpread => "Широкий spread",
            OfzSpreadSignalKind.ActivityWithWideSpread => "Активность при широком spread",
            OfzSpreadSignalKind.MissingQuotesOnActiveDay => "Нет котировок в активный день",
            OfzSpreadSignalKind.ImprovingSpread => "Сужение spread",
            OfzSpreadSignalKind.DepthImbalance => "Дисбаланс глубины",
            OfzSpreadSignalKind.SpreadOutlier => "Spread outlier",
            OfzSpreadSignalKind.ZSpreadContext => "Z-spread context",
            _ => kind.ToString()
        };
    }

    private static string FormatNullable(double? value, string format)
    {
        return value.HasValue
            ? value.Value.ToString(format)
            : "n/a";
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
