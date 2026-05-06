namespace CurveAnalyzer.Core;

public sealed class OfzMarketSummaryInput
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public OfzCouponType? CouponTypeFilter { get; init; }
    public OfzSummarySignalScope SignalScope { get; init; } = OfzSummarySignalScope.AllDays;
    public IEnumerable<OfzIssue> Issues { get; init; } = [];
    public IEnumerable<OfzDailyTrade> Trades { get; init; } = [];
    public IEnumerable<OfzActivityMetric> ActivityMetrics { get; init; } = [];
    public IEnumerable<OfzLiquidityMetric> LiquidityMetrics { get; init; } = [];
}

public sealed class OfzMarketSummaryOptions
{
    public int MaxFindings { get; init; } = 7;
    public int MaxTopIssuesPerSegment { get; init; } = 5;
    public int MinimumRepeatedIssueDates { get; init; } = 2;
    public double MinimumYieldMoveAbs { get; init; } = 0.1;
    public double SegmentConcentrationShare { get; init; } = 0.5;
    public DateTime? GeneratedAt { get; init; }
}

public static class OfzMarketSummaryBuilder
{
    public static OfzMarketSummary Build(
        OfzMarketSummaryInput input,
        OfzMarketSummaryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        options ??= new OfzMarketSummaryOptions();
        Validate(input, options);

        var startDate = input.StartDate.Date;
        var endDate = input.EndDate.Date;
        var issuesBySecId = BuildIssueLookup(input.Issues);

        var trades = input.Trades
            .Where(trade => IsInRange(trade.TradeDate, startDate, endDate))
            .Where(trade => MatchesFilter(trade.SecId, issuesBySecId, input.CouponTypeFilter))
            .ToList();

        var metrics = input.ActivityMetrics
            .Where(metric => IsInRange(metric.TradeDate, startDate, endDate))
            .Where(metric => MatchesFilter(metric.SecId, issuesBySecId, input.CouponTypeFilter))
            .ToList();

        var liquidityMetrics = input.LiquidityMetrics
            .Where(metric => IsInRange(metric.TradeDate, startDate, endDate))
            .Where(metric => MatchesFilter(metric.SecId, issuesBySecId, input.CouponTypeFilter))
            .ToList();

        var signalMetrics = ApplySignalScope(metrics, input.SignalScope).ToList();
        var signalLiquidityMetrics = ApplySignalScope(liquidityMetrics, input.SignalScope).ToList();
        var signalStartDate = GetMinDate(signalMetrics, signalLiquidityMetrics, startDate);
        var signalEndDate = GetMaxDate(signalMetrics, signalLiquidityMetrics, endDate);
        var limitations = BuildMarketLimitations(trades, metrics, liquidityMetrics, startDate, endDate);
        var segments = BuildSegments(metrics, liquidityMetrics, issuesBySecId, options);
        var findings = BuildFindings(
                signalMetrics,
                signalLiquidityMetrics,
                segments,
                limitations,
                issuesBySecId,
                options)
            .OrderByDescending(finding => finding.Priority)
            .ThenBy(finding => finding.Id, StringComparer.Ordinal)
            .Take(options.MaxFindings)
            .ToList();

        return new OfzMarketSummary
        {
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = options.GeneratedAt ?? DateTime.UtcNow,
            CouponTypeFilter = input.CouponTypeFilter,
            CouponTypeMarker = input.CouponTypeFilter.HasValue
                ? GetCouponTypeMarker(input.CouponTypeFilter.Value)
                : null,
            SignalScope = input.SignalScope,
            InsightStartDate = signalStartDate,
            InsightEndDate = signalEndDate,
            Findings = findings,
            Segments = segments,
            Limitations = limitations,
            SourceCounts = BuildSourceCounts(trades, metrics, liquidityMetrics, issuesBySecId, input.CouponTypeFilter)
        };
    }

    private static IReadOnlyList<OfzSummaryFinding> BuildFindings(
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyCollection<OfzSegmentSummary> segments,
        IReadOnlyList<OfzDataLimitation> marketLimitations,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzMarketSummaryOptions options)
    {
        List<OfzSummaryFinding> findings = [];

        AddMarketActivityFinding(findings, metrics, issuesBySecId);
        AddSegmentConcentrationFinding(findings, segments, options.SegmentConcentrationShare);
        AddRepeatedIssueFinding(findings, metrics, issuesBySecId, options);
        AddYieldMoveFinding(findings, metrics, issuesBySecId, options);
        AddWeakLiquidityFinding(findings, liquidityMetrics, issuesBySecId);
        AddDataQualityFinding(findings, metrics, liquidityMetrics, marketLimitations);

        return findings;
    }

    private static void AddMarketActivityFinding(
        ICollection<OfzSummaryFinding> findings,
        IEnumerable<OfzActivityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var activeDate = metrics
            .Where(metric => metric.Value is > 0 || metric.NumTrades is > 0)
            .GroupBy(metric => metric.TradeDate.Date)
            .Select(group =>
            {
                var rankableScores = group.Where(metric => metric.IsRankable).Select(metric => metric.ActivityScore!.Value).ToArray();

                return new
                {
                    TradeDate = group.Key,
                    TotalValue = group.Sum(metric => metric.Value ?? 0),
                    TotalNumTrades = group.Sum(metric => metric.NumTrades ?? 0),
                    ActiveIssueCount = group.Count(metric => metric.Value is > 0 || metric.NumTrades is > 0),
                    RankableIssueCount = rankableScores.Length,
                    MedianActivityScore = Median(rankableScores),
                    MaxActivityScore = rankableScores.Length == 0 ? (double?)null : rankableScores.Max()
                };
            })
            .OrderByDescending(item => item.TotalValue)
            .ThenByDescending(item => item.TotalNumTrades)
            .FirstOrDefault();

        if (activeDate is null)
        {
            return;
        }

        var issueCount = metrics
            .Select(metric => metric.SecId)
            .Where(secId => !string.IsNullOrWhiteSpace(secId))
            .Distinct(StringComparer.Ordinal)
            .Count();

        findings.Add(new OfzSummaryFinding
        {
            Id = $"market-active-date-{activeDate.TradeDate:yyyy-MM-dd}",
            Kind = OfzSummaryFindingKind.MarketActivity,
            Priority = 900 + Math.Min(activeDate.ActiveIssueCount, 99),
            Scope = OfzSummaryScope.Date,
            Title = "Самый активный день периода",
            Text = $"{activeDate.TradeDate:dd.MM.yyyy}: оборот {FormatMoney(activeDate.TotalValue)}, сделок {activeDate.TotalNumTrades:N0}, активных выпусков {activeDate.ActiveIssueCount}.",
            Evidence = new OfzFindingEvidence
            {
                TradeDate = activeDate.TradeDate,
                IssueCount = issueCount,
                ActiveIssueCount = activeDate.ActiveIssueCount,
                RankableIssueCount = activeDate.RankableIssueCount,
                TotalValue = activeDate.TotalValue,
                TotalNumTrades = activeDate.TotalNumTrades,
                MedianActivityScore = activeDate.MedianActivityScore,
                MaxActivityScore = activeDate.MaxActivityScore
            },
            DrillDown = new OfzSummaryDrillDown
            {
                Target = OfzSummaryDrillDownTarget.HeatmapDate,
                TradeDate = activeDate.TradeDate
            }
        });
    }

    private static void AddSegmentConcentrationFinding(
        ICollection<OfzSummaryFinding> findings,
        IReadOnlyCollection<OfzSegmentSummary> segments,
        double minimumShare)
    {
        var totalValue = segments.Sum(segment => segment.TotalValue);
        if (totalValue <= 0)
        {
            return;
        }

        var topSegment = segments
            .Where(segment => segment.TotalValue > 0)
            .OrderByDescending(segment => segment.TotalValue)
            .ThenBy(segment => segment.CouponType)
            .FirstOrDefault();

        if (topSegment is null)
        {
            return;
        }

        var share = topSegment.TotalValue / totalValue;
        if (share < minimumShare)
        {
            return;
        }

        findings.Add(new OfzSummaryFinding
        {
            Id = $"segment-concentration-{topSegment.CouponType.ToString().ToLowerInvariant()}",
            Kind = OfzSummaryFindingKind.SegmentConcentration,
            Priority = 760 + (int)Math.Round(share * 100),
            Scope = OfzSummaryScope.Segment,
            Title = "Концентрация оборота в сегменте",
            Text = $"{topSegment.CouponTypeMarker}: {share:P0} оборота выбранного набора, активных выпусков {topSegment.ActiveIssueCount}.",
            Evidence = new OfzFindingEvidence
            {
                CouponType = topSegment.CouponType,
                CouponTypeMarker = topSegment.CouponTypeMarker,
                IssueCount = topSegment.IssueCount,
                ActiveIssueCount = topSegment.ActiveIssueCount,
                RankableIssueCount = topSegment.RankableIssueCount,
                TotalValue = topSegment.TotalValue,
                TotalNumTrades = topSegment.TotalNumTrades,
                MedianActivityScore = topSegment.MedianActivityScore,
                MaxActivityScore = topSegment.MaxActivityScore
            },
            Limitations = topSegment.Limitations,
            DrillDown = new OfzSummaryDrillDown
            {
                Target = OfzSummaryDrillDownTarget.SegmentDetail,
                CouponType = topSegment.CouponType
            }
        });
    }

    private static void AddRepeatedIssueFinding(
        ICollection<OfzSummaryFinding> findings,
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzMarketSummaryOptions options)
    {
        var repeated = metrics
            .Where(metric => metric.IsRankable)
            .GroupBy(metric => metric.SecId, StringComparer.Ordinal)
            .Select(group => new
            {
                SecId = group.Key,
                Dates = group.Select(metric => metric.TradeDate.Date).Distinct().Count(),
                MaxMetric = group
                    .OrderByDescending(metric => metric.ActivityScore)
                    .ThenByDescending(metric => metric.Value)
                    .First()
            })
            .Where(item => item.Dates >= options.MinimumRepeatedIssueDates)
            .OrderByDescending(item => item.Dates)
            .ThenByDescending(item => item.MaxMetric.ActivityScore)
            .FirstOrDefault();

        if (repeated is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, repeated.SecId);
        var evidence = CreateEvidence(repeated.MaxMetric, null, issue);
        findings.Add(new OfzSummaryFinding
        {
            Id = $"repeated-issue-{NormalizeId(repeated.SecId)}",
            Kind = OfzSummaryFindingKind.RepeatedIssue,
            Priority = 820 + repeated.Dates,
            Scope = OfzSummaryScope.Issue,
            Title = "Повторяющаяся активность выпуска",
            Text = $"{GetShortName(issue, repeated.SecId)} появлялся в активных днях {repeated.Dates} раза; максимум score {repeated.MaxMetric.ActivityScore:N2}.",
            Evidence = new OfzFindingEvidence
            {
                TradeDate = evidence.TradeDate,
                SecId = evidence.SecId,
                ShortName = evidence.ShortName,
                CouponType = evidence.CouponType,
                CouponTypeMarker = evidence.CouponTypeMarker,
                ActiveIssueCount = repeated.Dates,
                Value = evidence.Value,
                NumTrades = evidence.NumTrades,
                ActivityScore = evidence.ActivityScore,
                YieldMove = evidence.YieldMove,
                YieldValue = evidence.YieldValue,
                Duration = evidence.Duration
            },
            DrillDown = new OfzSummaryDrillDown
            {
                Target = OfzSummaryDrillDownTarget.IssueDetail,
                SecId = repeated.SecId,
                TradeDate = repeated.MaxMetric.TradeDate.Date
            }
        });
    }

    private static void AddYieldMoveFinding(
        ICollection<OfzSummaryFinding> findings,
        IEnumerable<OfzActivityMetric> metrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzMarketSummaryOptions options)
    {
        var metric = metrics
            .Where(metric => Math.Abs(metric.YieldMove ?? 0) >= options.MinimumYieldMoveAbs)
            .OrderByDescending(metric => Math.Abs(metric.YieldMove ?? 0))
            .ThenByDescending(metric => metric.ActivityScore ?? 0)
            .FirstOrDefault();

        if (metric is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, metric.SecId);
        findings.Add(new OfzSummaryFinding
        {
            Id = $"yield-move-{NormalizeId(metric.SecId)}-{metric.TradeDate:yyyy-MM-dd}",
            Kind = OfzSummaryFindingKind.YieldMove,
            Priority = 700 + (int)Math.Round(Math.Abs(metric.YieldMove ?? 0) * 100),
            Scope = OfzSummaryScope.Issue,
            Title = "Заметное движение доходности",
            Text = $"{GetShortName(issue, metric.SecId)}: yield delta {metric.YieldMove:N2} п.п., score {FormatNullable(metric.ActivityScore, "N2")} на {metric.TradeDate:dd.MM.yyyy}.",
            Evidence = CreateEvidence(metric, null, issue),
            DrillDown = new OfzSummaryDrillDown
            {
                Target = OfzSummaryDrillDownTarget.IssueDetail,
                SecId = metric.SecId,
                TradeDate = metric.TradeDate.Date
            }
        });
    }

    private static void AddWeakLiquidityFinding(
        ICollection<OfzSummaryFinding> findings,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId)
    {
        var metric = liquidityMetrics
            .Where(IsWeakLiquidityCandidate)
            .OrderByDescending(GetWeakLiquidityPriority)
            .ThenByDescending(metric => metric.LiquidityScore ?? -1)
            .ThenByDescending(metric => metric.Value ?? 0)
            .FirstOrDefault();

        if (metric is null)
        {
            return;
        }

        var issue = GetIssue(issuesBySecId, metric.SecId);
        var limitations = BuildLiquidityLimitations(metric, issue);

        findings.Add(new OfzSummaryFinding
        {
            Id = $"weak-liquidity-{NormalizeId(metric.SecId)}-{metric.TradeDate:yyyy-MM-dd}",
            Kind = OfzSummaryFindingKind.WeakLiquidity,
            Priority = 680 + GetWeakLiquidityPriority(metric) * 20,
            Scope = OfzSummaryScope.Liquidity,
            Title = "Слабая ликвидность",
            Text = $"{GetShortName(issue, metric.SecId)}: bucket {metric.LiquidityBucket}, score {FormatNullable(metric.LiquidityScore, "N2")}, spread {FormatNullable(metric.Spread, "N3")} на {metric.TradeDate:dd.MM.yyyy}.",
            Evidence = CreateEvidence(null, metric, issue),
            Limitations = limitations,
            DrillDown = new OfzSummaryDrillDown
            {
                Target = OfzSummaryDrillDownTarget.WeakLiquidityTable,
                SecId = metric.SecId,
                TradeDate = metric.TradeDate.Date
            }
        });
    }

    private static void AddDataQualityFinding(
        ICollection<OfzSummaryFinding> findings,
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyList<OfzDataLimitation> marketLimitations)
    {
        var missingBaselineCount = metrics.Count(metric => metric.Status is OfzActivityMetricStatus.InsufficientBaseline or OfzActivityMetricStatus.MissingBaseline);
        var missingSpreadCount = liquidityMetrics.Count(metric => metric.SpreadSource == OfzSpreadSource.Missing);
        var snapshotCount = liquidityMetrics.Count(metric => metric.IsSnapshot);
        var provisionalCount = liquidityMetrics.Count(metric => metric.IsProvisional);
        var limitations = marketLimitations
            .Where(limitation => limitation.Kind is not OfzDataLimitationKind.NoData)
            .ToList();

        if (missingBaselineCount == 0 && missingSpreadCount == 0 && snapshotCount == 0 && provisionalCount == 0 && limitations.Count == 0)
        {
            return;
        }

        findings.Add(new OfzSummaryFinding
        {
            Id = "data-quality-limitations",
            Kind = OfzSummaryFindingKind.DataQuality,
            Priority = 500 + missingSpreadCount + snapshotCount + provisionalCount + missingBaselineCount,
            Scope = OfzSummaryScope.DataQuality,
            Title = "Ограничения данных",
            Text = $"Ограничения расчета: missing spread {missingSpreadCount}, snapshot-only {snapshotCount}, provisional {provisionalCount}, baseline gaps {missingBaselineCount}.",
            Evidence = new OfzFindingEvidence
            {
                IssueCount = metrics.Select(metric => metric.SecId).Distinct(StringComparer.Ordinal).Count(),
                RankableIssueCount = metrics.Count(metric => metric.IsRankable),
                ActiveIssueCount = metrics.Count(metric => metric.Value is > 0 || metric.NumTrades is > 0),
                TotalValue = metrics.Sum(metric => metric.Value ?? 0),
                TotalNumTrades = metrics.Sum(metric => metric.NumTrades ?? 0),
                IsSnapshot = snapshotCount > 0,
                IsProvisional = provisionalCount > 0
            },
            Limitations = limitations,
            DrillDown = new OfzSummaryDrillDown
            {
                Target = OfzSummaryDrillDownTarget.ActivityTable
            }
        });
    }

    private static IReadOnlyList<OfzSegmentSummary> BuildSegments(
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzMarketSummaryOptions options)
    {
        var secIds = metrics
            .Select(metric => metric.SecId)
            .Concat(liquidityMetrics.Select(metric => metric.SecId))
            .Where(secId => !string.IsNullOrWhiteSpace(secId))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return secIds
            .GroupBy(secId => GetIssue(issuesBySecId, secId)?.CouponType ?? OfzCouponType.Unknown)
            .OrderBy(group => group.Key)
            .Select(group => BuildSegment(group.Key, group, metrics, liquidityMetrics, issuesBySecId, options))
            .ToList();
    }

    private static OfzSegmentSummary BuildSegment(
        OfzCouponType couponType,
        IEnumerable<string> secIds,
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzMarketSummaryOptions options)
    {
        var secIdSet = secIds.ToHashSet(StringComparer.Ordinal);
        var segmentMetrics = metrics.Where(metric => secIdSet.Contains(metric.SecId)).ToList();
        var segmentLiquidityMetrics = liquidityMetrics.Where(metric => secIdSet.Contains(metric.SecId)).ToList();
        var activeIssueCount = segmentMetrics
            .Where(metric => metric.Value is > 0 || metric.NumTrades is > 0)
            .Select(metric => metric.SecId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var rankableScores = segmentMetrics
            .Where(metric => metric.IsRankable)
            .Select(metric => metric.ActivityScore!.Value)
            .ToArray();
        var topIssues = BuildTopIssues(segmentMetrics, segmentLiquidityMetrics, issuesBySecId, options.MaxTopIssuesPerSegment);
        var limitations = BuildSegmentLimitations(couponType, segmentLiquidityMetrics);

        return new OfzSegmentSummary
        {
            CouponType = couponType,
            CouponTypeMarker = GetCouponTypeMarker(couponType),
            IssueCount = secIdSet.Count,
            ActiveIssueCount = activeIssueCount,
            RankableIssueCount = rankableScores.Length,
            TotalValue = segmentMetrics.Sum(metric => metric.Value ?? 0),
            TotalNumTrades = segmentMetrics.Sum(metric => metric.NumTrades ?? 0),
            MedianActivityScore = Median(rankableScores),
            MaxActivityScore = rankableScores.Length == 0 ? null : rankableScores.Max(),
            WeakLiquidityCount = segmentLiquidityMetrics.Count(IsWeakLiquidityCandidate),
            MissingQuotesCount = segmentLiquidityMetrics.Count(metric => metric.Status == OfzLiquidityMetricStatus.MissingQuotes),
            SnapshotOnlyCount = segmentLiquidityMetrics.Count(metric => metric.IsSnapshot),
            TopIssues = topIssues,
            Limitations = limitations
        };
    }

    private static IReadOnlyList<OfzIssueFocus> BuildTopIssues(
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        int topCount)
    {
        var liquidityByKey = liquidityMetrics
            .GroupBy(metric => (metric.SecId, TradeDate: metric.TradeDate.Date))
            .ToDictionary(group => group.Key, group => group.OrderByDescending(GetWeakLiquidityPriority).First());

        return metrics
            .Where(metric => metric.Value is > 0 || metric.ActivityScore is > 0 || metric.NumTrades is > 0)
            .GroupBy(metric => metric.SecId, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(metric => metric.Value ?? 0)
                .ThenByDescending(metric => metric.ActivityScore ?? 0)
                .First())
            .OrderByDescending(metric => metric.Value ?? 0)
            .ThenByDescending(metric => metric.ActivityScore ?? 0)
            .ThenBy(metric => metric.SecId, StringComparer.Ordinal)
            .Take(topCount)
            .Select(metric =>
            {
                var issue = GetIssue(issuesBySecId, metric.SecId);
                liquidityByKey.TryGetValue((metric.SecId, metric.TradeDate.Date), out var liquidityMetric);

                return new OfzIssueFocus
                {
                    SecId = metric.SecId,
                    ShortName = GetShortName(issue, metric.SecId),
                    CouponTypeMarker = issue?.CouponTypeMarker,
                    DisplayMarker = issue?.DisplayMarker,
                    TradeDate = metric.TradeDate.Date,
                    Value = metric.Value,
                    NumTrades = metric.NumTrades,
                    ActivityScore = metric.ActivityScore,
                    YieldMove = metric.YieldMove,
                    Duration = metric.Duration,
                    Spread = liquidityMetric?.Spread,
                    LiquidityScore = liquidityMetric?.LiquidityScore,
                    LiquidityBucket = liquidityMetric?.LiquidityBucket,
                    LiquidityStatus = liquidityMetric?.Status,
                    IsSnapshot = liquidityMetric?.IsSnapshot ?? false,
                    Reason = metric.ActivityScore > 0 && metric.ActivityScore >= (metric.Value ?? 0)
                        ? OfzIssueFocusReason.TopScore
                        : OfzIssueFocusReason.TopValue
                };
            })
            .ToList();
    }

    private static IReadOnlyList<OfzDataLimitation> BuildMarketLimitations(
        IReadOnlyCollection<OfzDailyTrade> trades,
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        DateTime startDate,
        DateTime endDate)
    {
        List<OfzDataLimitation> limitations = [];

        if (trades.Count == 0 && metrics.Count == 0 && liquidityMetrics.Count == 0)
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.NoData,
                Scope = OfzSummaryScope.Market,
                Text = "В выбранном периоде нет trade, activity и liquidity данных."
            });
            return limitations;
        }

        if ((endDate - startDate).TotalDays < 5)
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.ShortRange,
                Scope = OfzSummaryScope.Market,
                Text = "Короткий период ограничивает сравнение с устойчивым baseline."
            });
        }

        if (metrics.Any(metric => metric.Status is OfzActivityMetricStatus.InsufficientBaseline or OfzActivityMetricStatus.MissingBaseline))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.InsufficientBaseline,
                Scope = OfzSummaryScope.Market,
                Text = "Часть activity metrics не имеет достаточного baseline."
            });
        }

        if (liquidityMetrics.Count == 0)
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.MissingSpread,
                Scope = OfzSummaryScope.Liquidity,
                Text = "Liquidity metrics отсутствуют; spread и bucket не учитываются."
            });
        }
        else if (liquidityMetrics.Any(metric => metric.SpreadSource == OfzSpreadSource.Missing))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.MissingSpread,
                Scope = OfzSummaryScope.Spread,
                Text = "Для части выпусков spread отсутствует и не заменяется нулем."
            });
        }

        if (liquidityMetrics.Any(metric => metric.IsSnapshot))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.SnapshotOnly,
                Scope = OfzSummaryScope.Liquidity,
                Text = "Часть liquidity данных основана на current snapshot."
            });
        }

        if (liquidityMetrics.Any(metric => metric.IsProvisional))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.Provisional,
                Scope = OfzSummaryScope.Liquidity,
                Text = "Часть текущих liquidity данных предварительная."
            });
        }

        return limitations;
    }

    private static IReadOnlyList<OfzDataLimitation> BuildSegmentLimitations(
        OfzCouponType couponType,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics)
    {
        List<OfzDataLimitation> limitations = [];

        if (couponType == OfzCouponType.Currency)
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.CurrencyMixed,
                Scope = OfzSummaryScope.Segment,
                CouponType = couponType,
                Text = "Валютный сегмент не смешивается с рублевыми ОФЗ; номиналы и обороты могут быть в другой валюте."
            });
        }

        if (liquidityMetrics.Any(metric => metric.Status == OfzLiquidityMetricStatus.MissingQuotes))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.MissingQuotes,
                Scope = OfzSummaryScope.Segment,
                CouponType = couponType,
                Text = "В сегменте есть выпуски без котировок."
            });
        }

        if (liquidityMetrics.Any(metric => metric.IsSnapshot))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.SnapshotOnly,
                Scope = OfzSummaryScope.Segment,
                CouponType = couponType,
                Text = "В сегменте есть snapshot-only liquidity observations."
            });
        }

        return limitations;
    }

    private static IReadOnlyList<OfzDataLimitation> BuildLiquidityLimitations(
        OfzLiquidityMetric metric,
        OfzIssue? issue)
    {
        List<OfzDataLimitation> limitations = [];

        if (metric.SpreadSource == OfzSpreadSource.Missing)
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.MissingSpread,
                Scope = OfzSummaryScope.Spread,
                SecId = metric.SecId,
                TradeDate = metric.TradeDate.Date,
                CouponType = issue?.CouponType,
                Text = "Spread отсутствует и остается null."
            });
        }

        if (metric.Status == OfzLiquidityMetricStatus.MissingQuotes)
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.MissingQuotes,
                Scope = OfzSummaryScope.Liquidity,
                SecId = metric.SecId,
                TradeDate = metric.TradeDate.Date,
                CouponType = issue?.CouponType,
                Text = "Для выпуска нет котировок на дату наблюдения."
            });
        }

        if (metric.IsSnapshot)
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.SnapshotOnly,
                Scope = OfzSummaryScope.Liquidity,
                SecId = metric.SecId,
                TradeDate = metric.TradeDate.Date,
                CouponType = issue?.CouponType,
                Text = "Liquidity evidence построен по current snapshot."
            });
        }

        if (metric.IsProvisional)
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.Provisional,
                Scope = OfzSummaryScope.Liquidity,
                SecId = metric.SecId,
                TradeDate = metric.TradeDate.Date,
                CouponType = issue?.CouponType,
                Text = "Liquidity evidence предварительный для текущей даты."
            });
        }

        return limitations;
    }

    private static OfzSummarySourceCounts BuildSourceCounts(
        IReadOnlyCollection<OfzDailyTrade> trades,
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzCouponType? couponTypeFilter)
    {
        var dates = trades.Select(trade => trade.TradeDate.Date)
            .Concat(metrics.Select(metric => metric.TradeDate.Date))
            .Concat(liquidityMetrics.Select(metric => metric.TradeDate.Date))
            .Distinct()
            .Count();

        return new OfzSummarySourceCounts
        {
            Issues = issuesBySecId.Values.Count(issue => !couponTypeFilter.HasValue || issue.CouponType == couponTypeFilter.Value),
            Trades = trades.Count,
            ActivityMetrics = metrics.Count,
            LiquidityMetrics = liquidityMetrics.Count(metric => !metric.IsSnapshot),
            SnapshotLiquidityMetrics = liquidityMetrics.Count(metric => metric.IsSnapshot),
            Dates = dates
        };
    }

    private static OfzFindingEvidence CreateEvidence(
        OfzActivityMetric? activityMetric,
        OfzLiquidityMetric? liquidityMetric,
        OfzIssue? issue)
    {
        return new OfzFindingEvidence
        {
            TradeDate = activityMetric?.TradeDate.Date ?? liquidityMetric?.TradeDate.Date,
            SecId = activityMetric?.SecId ?? liquidityMetric?.SecId,
            ShortName = GetShortName(issue, activityMetric?.SecId ?? liquidityMetric?.SecId ?? string.Empty),
            CouponType = issue?.CouponType,
            CouponTypeMarker = issue?.CouponTypeMarker,
            Value = activityMetric?.Value ?? liquidityMetric?.Value,
            NumTrades = activityMetric?.NumTrades ?? liquidityMetric?.NumTrades,
            ActivityScore = activityMetric?.ActivityScore,
            YieldMove = activityMetric?.YieldMove,
            YieldValue = activityMetric?.YieldValue,
            Duration = activityMetric?.Duration,
            Spread = liquidityMetric?.Spread,
            SpreadSource = liquidityMetric?.SpreadSource,
            LiquidityScore = liquidityMetric?.LiquidityScore,
            LiquidityBucket = liquidityMetric?.LiquidityBucket,
            LiquidityStatus = liquidityMetric?.Status,
            ZSpread = liquidityMetric?.ZSpread,
            ZSpreadBp = liquidityMetric?.ZSpreadBp,
            GSpreadBp = liquidityMetric?.GSpreadBp,
            IsSnapshot = liquidityMetric?.IsSnapshot ?? false,
            IsProvisional = liquidityMetric?.IsProvisional ?? false
        };
    }

    private static IEnumerable<T> ApplySignalScope<T>(
        IEnumerable<T> items,
        OfzSummarySignalScope signalScope)
        where T : class
    {
        if (signalScope == OfzSummarySignalScope.AllDays)
        {
            return items;
        }

        var list = items.ToList();
        var maxDate = list.Select(GetTradeDate).Where(date => date.HasValue).Max();
        return maxDate.HasValue
            ? list.Where(item => GetTradeDate(item) == maxDate.Value)
            : list;
    }

    private static DateTime? GetTradeDate<T>(T item)
    {
        return item switch
        {
            OfzActivityMetric metric => metric.TradeDate.Date,
            OfzLiquidityMetric metric => metric.TradeDate.Date,
            _ => null
        };
    }

    private static DateTime GetMinDate(
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        DateTime fallback)
    {
        return metrics.Select(metric => metric.TradeDate.Date)
            .Concat(liquidityMetrics.Select(metric => metric.TradeDate.Date))
            .DefaultIfEmpty(fallback)
            .Min();
    }

    private static DateTime GetMaxDate(
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        DateTime fallback)
    {
        return metrics.Select(metric => metric.TradeDate.Date)
            .Concat(liquidityMetrics.Select(metric => metric.TradeDate.Date))
            .DefaultIfEmpty(fallback)
            .Max();
    }

    private static bool IsWeakLiquidityCandidate(OfzLiquidityMetric metric)
    {
        return metric.LiquidityBucket is OfzLiquidityBucket.Problem or OfzLiquidityBucket.Weak ||
            metric.Status is OfzLiquidityMetricStatus.MissingQuotes or OfzLiquidityMetricStatus.NoData ||
            metric.SpreadSource == OfzSpreadSource.Missing;
    }

    private static int GetWeakLiquidityPriority(OfzLiquidityMetric metric)
    {
        return metric.LiquidityBucket switch
        {
            OfzLiquidityBucket.Problem => 4,
            OfzLiquidityBucket.Weak => 3,
            OfzLiquidityBucket.MissingData => 2,
            _ => metric.Status == OfzLiquidityMetricStatus.MissingQuotes ? 2 : 1
        };
    }

    private static bool IsInRange(DateTime date, DateTime startDate, DateTime endDate)
    {
        var normalized = date.Date;
        return normalized >= startDate && normalized <= endDate;
    }

    private static bool MatchesFilter(
        string secId,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzCouponType? couponTypeFilter)
    {
        if (!couponTypeFilter.HasValue)
        {
            return true;
        }

        return GetIssue(issuesBySecId, secId)?.CouponType == couponTypeFilter.Value;
    }

    private static Dictionary<string, OfzIssue> BuildIssueLookup(IEnumerable<OfzIssue> issues)
    {
        return issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue.SecId))
            .GroupBy(issue => issue.SecId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    private static OfzIssue? GetIssue(
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        string secId)
    {
        return issuesBySecId.TryGetValue(secId, out var issue) ? issue : null;
    }

    private static string GetShortName(OfzIssue? issue, string secId)
    {
        return !string.IsNullOrWhiteSpace(issue?.ShortName) ? issue.ShortName : secId;
    }

    private static string GetCouponTypeMarker(OfzCouponType couponType)
    {
        return couponType switch
        {
            OfzCouponType.Fixed => "ОФЗ-ПД",
            OfzCouponType.Floating => "ОФЗ-ПК",
            OfzCouponType.InflationLinked => "ОФЗ-ИН",
            OfzCouponType.Amortized => "ОФЗ-АД",
            OfzCouponType.Currency => "Валютная",
            _ => "Тип n/a"
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

    private static string FormatMoney(double value)
    {
        return value >= 1_000_000_000
            ? $"{value / 1_000_000_000:N2} млрд"
            : $"{value / 1_000_000:N2} млн";
    }

    private static string FormatNullable(double? value, string format)
    {
        return value.HasValue ? value.Value.ToString(format) : "n/a";
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToLowerInvariant();
    }

    private static void Validate(OfzMarketSummaryInput input, OfzMarketSummaryOptions options)
    {
        if (input.StartDate.Date > input.EndDate.Date)
        {
            throw new ArgumentException("StartDate must be less than or equal to EndDate.", nameof(input));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxFindings);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxTopIssuesPerSegment);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MinimumRepeatedIssueDates, 2);
        ArgumentOutOfRangeException.ThrowIfNegative(options.MinimumYieldMoveAbs);
        ArgumentOutOfRangeException.ThrowIfNegative(options.SegmentConcentrationShare);
    }
}
