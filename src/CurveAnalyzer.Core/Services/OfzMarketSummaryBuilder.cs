namespace CurveAnalyzer.Core;

public sealed class OfzMarketSummaryInput
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? InsightStartDate { get; init; }
    public DateTime? InsightEndDate { get; init; }
    public OfzCouponType? CouponTypeFilter { get; init; }
    public OfzSummarySignalScope SignalScope { get; init; } = OfzSummarySignalScope.AllDays;
    public IEnumerable<OfzIssue> Issues { get; init; } = [];
    public IEnumerable<OfzDailyTrade> Trades { get; init; } = [];
    public IEnumerable<OfzActivityMetric> ActivityMetrics { get; init; } = [];
    public IEnumerable<OfzLiquidityMetric> LiquidityMetrics { get; init; } = [];
    public IEnumerable<CbrKeyRate> CbrKeyRates { get; init; } = [];
    public IEnumerable<OfzMarketIndexPoint> IndexPoints { get; init; } = [];
    public IEnumerable<OfzCashflowEvent> CashflowEvents { get; init; } = [];
    public bool CashflowDataLoaded { get; init; }
}

public sealed class OfzMarketSummaryOptions
{
    public int MaxFindings { get; init; } = 7;
    public int MaxTopIssuesPerSegment { get; init; } = 5;
    public int MinimumRepeatedIssueDates { get; init; } = 2;
    public double MinimumYieldMoveAbs { get; init; } = 0.1;
    public double SegmentConcentrationShare { get; init; } = 0.5;
    public OfzMarketBreadthOptions Breadth { get; init; } = new();
    public OfzIndexContextOptions IndexContext { get; init; } = new();
    public OfzCashflowContextOptions Cashflow { get; init; } = new();
    public OfzSeasonalityContextOptions Seasonality { get; init; } = new();
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
        var outputStartDate = input.InsightStartDate?.Date ?? startDate;
        var outputEndDate = input.InsightEndDate?.Date ?? endDate;
        (outputStartDate, outputEndDate) = NormalizeOutputRange(startDate, endDate, outputStartDate, outputEndDate);
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

        var breadthDays = BuildBreadthDays(trades, metrics, liquidityMetrics, issuesBySecId, options.Breadth);
        var activityDates = trades.Select(trade => trade.TradeDate.Date)
            .Concat(metrics.Select(metric => metric.TradeDate.Date))
            .Distinct();
        var indexContext = OfzIndexContextBuilder.Build(
            input.IndexPoints,
            startDate,
            endDate,
            activityDates,
            options.IndexContext);

        var outputTrades = ApplyDateRange(trades, outputStartDate, outputEndDate).ToList();
        var outputMetrics = ApplyDateRange(metrics, outputStartDate, outputEndDate).ToList();
        var outputLiquidityMetrics = ApplyDateRange(liquidityMetrics, outputStartDate, outputEndDate).ToList();
        var outputBreadthDays = ApplyDateRange(breadthDays, outputStartDate, outputEndDate).ToList();
        var outputIndexContextDays = ApplyDateRange(indexContext.Days, outputStartDate, outputEndDate).ToList();
        var seasonalityContext = OfzSeasonalityContextBuilder.Build(
            metrics,
            CreateSeasonalityOptions(options.Seasonality, startDate, endDate, outputStartDate, outputEndDate, input.SignalScope));

        var signalTrades = ApplySignalScope(outputTrades, input.SignalScope).ToList();
        var signalMetrics = ApplySignalScope(outputMetrics, input.SignalScope).ToList();
        var signalLiquidityMetrics = ApplySignalScope(outputLiquidityMetrics, input.SignalScope).ToList();
        var signalBreadthDays = ApplySignalScope(outputBreadthDays, input.SignalScope).ToList();
        var signalIndexContextDays = ApplySignalScope(outputIndexContextDays, input.SignalScope).ToList();
        var signalStartDate = GetMinDate(signalMetrics, signalLiquidityMetrics, signalBreadthDays, signalIndexContextDays, outputStartDate);
        var signalEndDate = GetMaxDate(signalMetrics, signalLiquidityMetrics, signalBreadthDays, signalIndexContextDays, outputEndDate);
        var specialMetrics = BuildSpecialMetrics(signalTrades, input.CouponTypeFilter, input.CbrKeyRates);
        var cashflowEvents = input.CashflowEvents.ToList();
        var shouldBuildCashflowContext = input.CashflowDataLoaded || cashflowEvents.Count > 0;
        var cashflowContext = OfzCashflowContextBuilder.Build(
            cashflowEvents,
            shouldBuildCashflowContext
                ? issuesBySecId.Values.Where(issue => !input.CouponTypeFilter.HasValue || issue.CouponType == input.CouponTypeFilter.Value)
                : [],
            signalMetrics,
            signalStartDate,
            signalEndDate,
            options.Cashflow);
        var limitations = BuildMarketLimitations(signalTrades, signalMetrics, signalLiquidityMetrics, signalStartDate, signalEndDate)
            .Concat(BuildSpecialMetricLimitations(specialMetrics, input.CouponTypeFilter))
            .Concat(BuildIndexLimitations(signalIndexContextDays, indexContext.Limitations))
            .Concat(cashflowContext.Limitations)
            .Concat(seasonalityContext.Limitations)
            .ToList();
        var segments = BuildSegments(signalMetrics, signalLiquidityMetrics, issuesBySecId, options);
        var findings = BuildFindings(
                signalMetrics,
                signalLiquidityMetrics,
                signalBreadthDays,
                signalIndexContextDays,
                cashflowContext,
                seasonalityContext,
                specialMetrics,
                segments,
                limitations,
                issuesBySecId,
                input.CouponTypeFilter,
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
            BreadthDays = signalBreadthDays,
            IndexContextDays = signalIndexContextDays,
            IndexSegments = indexContext.Segments,
            CashflowContext = cashflowContext,
            SeasonalityContext = seasonalityContext,
            SpecialMetrics = specialMetrics,
            Limitations = limitations,
            SourceCounts = BuildSourceCounts(signalTrades, signalMetrics, signalLiquidityMetrics, signalIndexContextDays, cashflowContext, seasonalityContext, specialMetrics, issuesBySecId, input.CouponTypeFilter)
        };
    }

    private static IReadOnlyList<OfzSummaryFinding> BuildFindings(
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyCollection<MarketBreadthDay> breadthDays,
        IReadOnlyCollection<OfzIndexContextDay> indexContextDays,
        OfzCashflowContext cashflowContext,
        OfzSeasonalityContext seasonalityContext,
        IReadOnlyCollection<OfzSpecialSummaryMetric> specialMetrics,
        IReadOnlyCollection<OfzSegmentSummary> segments,
        IReadOnlyList<OfzDataLimitation> marketLimitations,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzCouponType? couponTypeFilter,
        OfzMarketSummaryOptions options)
    {
        List<OfzSummaryFinding> findings = [];

        AddMarketActivityFinding(findings, metrics, issuesBySecId);
        AddSegmentConcentrationFinding(findings, segments, options.SegmentConcentrationShare);
        AddRepeatedIssueFinding(findings, metrics, issuesBySecId, options);
        AddYieldMoveFinding(findings, metrics, issuesBySecId, options);
        AddMarketBreadthFinding(findings, breadthDays, options.Breadth);
        AddTurnoverConcentrationFinding(findings, breadthDays);
        AddTypeShareFinding(findings, breadthDays, couponTypeFilter, options.Breadth);
        AddIndexContextFindings(findings, metrics, breadthDays, indexContextDays);
        AddCashflowFindings(findings, cashflowContext);
        AddSeasonalityFindings(findings, seasonalityContext);
        AddWeakLiquidityFinding(findings, liquidityMetrics, issuesBySecId);
        AddSpecialMetricFinding(findings, specialMetrics, couponTypeFilter);
        AddDataQualityFinding(findings, metrics, liquidityMetrics, marketLimitations);

        return findings;
    }

    private static OfzSeasonalityContextOptions CreateSeasonalityOptions(
        OfzSeasonalityContextOptions source,
        DateTime startDate,
        DateTime endDate,
        DateTime outputStartDate,
        DateTime outputEndDate,
        OfzSummarySignalScope signalScope)
    {
        return new OfzSeasonalityContextOptions
        {
            StartDate = startDate,
            EndDate = endDate,
            InsightStartDate = outputStartDate,
            InsightEndDate = outputEndDate,
            SignalScope = signalScope,
            MinWeekdayBaselineObservations = source.MinWeekdayBaselineObservations,
            MinMonthBaselineObservations = source.MinMonthBaselineObservations,
            HighActivityRatioThreshold = source.HighActivityRatioThreshold,
            LowActivityRatioThreshold = source.LowActivityRatioThreshold,
            UnchangedYieldMoveThreshold = source.UnchangedYieldMoveThreshold
        };
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
        var hasMarketEvidence = metrics.Count > 0 || liquidityMetrics.Count > 0;
        var limitations = marketLimitations
            .Where(limitation => limitation.Kind is not OfzDataLimitationKind.NoData)
            .Where(limitation => hasMarketEvidence || limitation.Kind is not OfzDataLimitationKind.NoIndexData)
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

    private static void AddMarketBreadthFinding(
        ICollection<OfzSummaryFinding> findings,
        IReadOnlyCollection<MarketBreadthDay> breadthDays,
        OfzMarketBreadthOptions options)
    {
        var day = breadthDays
            .Where(item => item.ComparableIssueCount >= options.MinimumComparableIssuesForBroadMove)
            .Where(item => item.Direction.DominantDirection is OfzDominantYieldDirection.Up or OfzDominantYieldDirection.Down)
            .Where(item => item.Direction.DominantDirectionShare >= options.BroadMoveShare)
            .OrderByDescending(item => item.Direction.DominantDirectionShare)
            .ThenByDescending(item => item.ComparableIssueCount)
            .ThenByDescending(item => item.TotalValue ?? 0)
            .FirstOrDefault();

        if (day is null)
        {
            return;
        }

        var directionLabel = day.Direction.DominantDirection == OfzDominantYieldDirection.Up
            ? "рост доходности"
            : "снижение доходности";
        var directionCount = day.Direction.DominantDirection == OfzDominantYieldDirection.Up
            ? day.Direction.YieldUpCount
            : day.Direction.YieldDownCount;

        findings.Add(new OfzSummaryFinding
        {
            Id = $"market-breadth-{day.TradeDate:yyyy-MM-dd}",
            Kind = OfzSummaryFindingKind.MarketBreadth,
            Priority = 740 + (int)Math.Round((day.Direction.DominantDirectionShare ?? 0) * 100),
            Scope = OfzSummaryScope.MarketBreadth,
            Title = "Широкое движение доходности",
            Text = $"{day.TradeDate:dd.MM.yyyy}: {directionLabel} у {directionCount} из {day.ComparableIssueCount} сравнимых выпусков; активных {day.ActiveIssueCount}.",
            Evidence = CreateEvidence(day),
            Limitations = day.Limitations,
            DrillDown = new OfzSummaryDrillDown
            {
                Target = OfzSummaryDrillDownTarget.MarketBreadthDay,
                TradeDate = day.TradeDate
            }
        });
    }

    private static void AddTurnoverConcentrationFinding(
        ICollection<OfzSummaryFinding> findings,
        IReadOnlyCollection<MarketBreadthDay> breadthDays)
    {
        var day = breadthDays
            .Where(item => item.Concentration.IsHighConcentration)
            .OrderByDescending(item => item.Concentration.Top5Share ?? 0)
            .ThenByDescending(item => item.Concentration.Top10Share ?? 0)
            .ThenByDescending(item => item.TotalValue ?? 0)
            .FirstOrDefault();

        if (day is null)
        {
            return;
        }

        findings.Add(new OfzSummaryFinding
        {
            Id = $"turnover-concentration-{day.TradeDate:yyyy-MM-dd}",
            Kind = OfzSummaryFindingKind.TurnoverConcentration,
            Priority = 720 + (int)Math.Round((day.Concentration.Top5Share ?? 0) * 100),
            Scope = OfzSummaryScope.MarketBreadth,
            Title = "Концентрация оборота",
            Text = $"{day.TradeDate:dd.MM.yyyy}: top-5 выпусков дали {FormatNullablePercent(day.Concentration.Top5Share)} оборота дня; база {day.Concentration.IssueBaseCount} выпусков.",
            Evidence = CreateEvidence(day),
            Limitations = day.Limitations,
            DrillDown = new OfzSummaryDrillDown
            {
                Target = OfzSummaryDrillDownTarget.MarketBreadthDay,
                TradeDate = day.TradeDate
            }
        });
    }

    private static void AddTypeShareFinding(
        ICollection<OfzSummaryFinding> findings,
        IReadOnlyCollection<MarketBreadthDay> breadthDays,
        OfzCouponType? couponTypeFilter,
        OfzMarketBreadthOptions options)
    {
        if (couponTypeFilter.HasValue)
        {
            return;
        }

        var aggregateTotal = breadthDays
            .SelectMany(day => day.TypeShares)
            .Sum(share => share.TotalValue ?? 0);
        if (aggregateTotal <= 0)
        {
            return;
        }

        var topType = breadthDays
            .SelectMany(day => day.TypeShares)
            .GroupBy(share => share.CouponType)
            .Select(group => new
            {
                CouponType = group.Key,
                CouponTypeMarker = GetCouponTypeMarker(group.Key),
                IssueCount = group.Sum(share => share.IssueCount),
                ActiveIssueCount = group.Sum(share => share.ActiveIssueCount),
                TotalValue = group.Sum(share => share.TotalValue ?? 0),
                NumTrades = group.Sum(share => share.NumTrades ?? 0),
                MissingTypeCount = group.Sum(share => share.MissingTypeCount)
            })
            .OrderByDescending(item => item.TotalValue)
            .ThenBy(item => item.CouponType)
            .FirstOrDefault();

        if (topType is null)
        {
            return;
        }

        var valueShare = topType.TotalValue / aggregateTotal;
        if (valueShare < options.TypeDominanceShare)
        {
            return;
        }

        findings.Add(new OfzSummaryFinding
        {
            Id = $"type-share-{topType.CouponType.ToString().ToLowerInvariant()}",
            Kind = OfzSummaryFindingKind.TypeShare,
            Priority = 700 + (int)Math.Round(valueShare * 100),
            Scope = OfzSummaryScope.MarketBreadth,
            Title = "Вклад типа ОФЗ в оборот",
            Text = $"{topType.CouponTypeMarker}: {valueShare:P0} оборота breadth-набора; активных наблюдений {topType.ActiveIssueCount}.",
            Evidence = new OfzFindingEvidence
            {
                CouponType = topType.CouponType,
                CouponTypeMarker = topType.CouponTypeMarker,
                IssueCount = topType.IssueCount,
                ActiveIssueCount = topType.ActiveIssueCount,
                TotalValue = topType.TotalValue,
                TotalNumTrades = topType.NumTrades,
                ValueShare = valueShare,
                MissingTypeCount = topType.MissingTypeCount
            },
            DrillDown = new OfzSummaryDrillDown
            {
                Target = OfzSummaryDrillDownTarget.SegmentDetail,
                CouponType = topType.CouponType
            }
        });
    }

    private static void AddIndexContextFindings(
        ICollection<OfzSummaryFinding> findings,
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<MarketBreadthDay> breadthDays,
        IReadOnlyCollection<OfzIndexContextDay> indexContextDays)
    {
        var activeDate = metrics
            .Where(metric => metric.Value is > 0 || metric.NumTrades is > 0)
            .GroupBy(metric => metric.TradeDate.Date)
            .Select(group => new
            {
                TradeDate = group.Key,
                TotalValue = group.Sum(metric => metric.Value ?? 0),
                TotalNumTrades = group.Sum(metric => metric.NumTrades ?? 0),
                ActiveIssueCount = group.Count(metric => metric.Value is > 0 || metric.NumTrades is > 0),
                MaxActivityScore = group
                    .Where(metric => metric.ActivityScore.HasValue)
                    .Select(metric => metric.ActivityScore!.Value)
                    .DefaultIfEmpty()
                    .Max()
            })
            .OrderByDescending(item => item.TotalValue)
            .ThenByDescending(item => item.TotalNumTrades)
            .FirstOrDefault();

        if (activeDate is not null)
        {
            var day = indexContextDays.FirstOrDefault(item => item.TradeDate.Date == activeDate.TradeDate);
            var point = day?.PriceIndexPoint ?? day?.TotalReturnIndexPoint;
            if (point is not null)
            {
                var meaningfulText = point.IsMeaningful
                    ? $"совпал с движением {point.SecId}: {FormatSignedPercent(point.DailyChangePercent)}, yield Δ {FormatSignedPoints(point.YieldChange)}"
                    : $"был локальным относительно спокойного {point.SecId}: {FormatSignedPercent(point.DailyChangePercent)}";

                findings.Add(new OfzSummaryFinding
                {
                    Id = point.IsMeaningful
                        ? $"activity-with-index-{point.SecId.ToLowerInvariant()}-{activeDate.TradeDate:yyyy-MM-dd}"
                        : $"activity-without-index-{point.SecId.ToLowerInvariant()}-{activeDate.TradeDate:yyyy-MM-dd}",
                    Kind = point.IsMeaningful
                        ? OfzSummaryFindingKind.ActivityWithIndexMove
                        : OfzSummaryFindingKind.ActivityWithoutIndexMove,
                    Priority = point.IsMeaningful ? 735 : 690,
                    Scope = OfzSummaryScope.IndexContext,
                    Title = point.IsMeaningful
                        ? "Активность на фоне индекса"
                        : "Локальная активность относительно индекса",
                    Text = $"{activeDate.TradeDate:dd.MM.yyyy}: активный день {meaningfulText}.",
                    Evidence = CreateIndexEvidence(point, activeDate.TotalValue, activeDate.TotalNumTrades, activeDate.ActiveIssueCount),
                    Limitations = day!.Limitations,
                    DrillDown = new OfzSummaryDrillDown
                    {
                        Target = OfzSummaryDrillDownTarget.IndexContextDay,
                        TradeDate = activeDate.TradeDate
                    }
                });
            }
            else if (day is not null && day.Limitations.Count > 0)
            {
                findings.Add(CreateIndexDataLimitationFinding(day));
            }
        }

        var segmentPoint = indexContextDays
            .SelectMany(day => day.SegmentPoints)
            .Where(point => point.IsMeaningful)
            .OrderByDescending(point => Math.Abs(point.DailyChangePercent ?? 0))
            .ThenBy(point => point.SecId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (segmentPoint is not null)
        {
            findings.Add(new OfzSummaryFinding
            {
                Id = $"segment-index-move-{segmentPoint.SecId.ToLowerInvariant()}-{segmentPoint.TradeDate:yyyy-MM-dd}",
                Kind = OfzSummaryFindingKind.SegmentIndexMove,
                Priority = 680,
                Scope = OfzSummaryScope.IndexContext,
                Title = "Движение duration-сегмента",
                Text = $"{segmentPoint.SecId}: {FormatSignedPercent(segmentPoint.DailyChangePercent)} за {segmentPoint.TradeDate:dd.MM.yyyy}; yield Δ {FormatSignedPoints(segmentPoint.YieldChange)}.",
                Evidence = CreateIndexEvidence(segmentPoint, null, null, null),
                Limitations = segmentPoint.Limitations,
                DrillDown = new OfzSummaryDrillDown
                {
                    Target = OfzSummaryDrillDownTarget.IndexContextDay,
                    TradeDate = segmentPoint.TradeDate
                }
            });
        }

        var limitationDay = indexContextDays.FirstOrDefault(day => day.Points.Count == 0 && day.Limitations.Count > 0);
        if (limitationDay is not null)
        {
            findings.Add(CreateIndexDataLimitationFinding(limitationDay));
        }
    }

    private static OfzSummaryFinding CreateIndexDataLimitationFinding(OfzIndexContextDay day)
    {
        return new OfzSummaryFinding
        {
            Id = $"index-data-limitation-{day.TradeDate:yyyy-MM-dd}",
            Kind = OfzSummaryFindingKind.IndexDataLimitation,
            Priority = 500,
            Scope = OfzSummaryScope.DataQuality,
            Title = "Индексный фон неполный",
            Text = $"{day.TradeDate:dd.MM.yyyy}: индексный контекст неполный, значения не заменяются нулями.",
            Evidence = new OfzFindingEvidence
            {
                TradeDate = day.TradeDate,
                IsProvisional = day.IsProvisional
            },
            Limitations = day.Limitations,
            DrillDown = new OfzSummaryDrillDown
            {
                Target = OfzSummaryDrillDownTarget.IndexContextDay,
                TradeDate = day.TradeDate
            }
        };
    }

    private static OfzFindingEvidence CreateIndexEvidence(
        OfzIndexContextPoint point,
        double? totalValue,
        int? totalNumTrades,
        int? activeIssueCount)
    {
        return new OfzFindingEvidence
        {
            TradeDate = point.TradeDate,
            ActiveIssueCount = activeIssueCount,
            TotalValue = totalValue,
            TotalNumTrades = totalNumTrades,
            IndexSecId = point.SecId,
            IndexClose = point.Close,
            IndexDailyChange = point.DailyChange,
            IndexDailyChangePercent = point.DailyChangePercent,
            IndexYield = point.Yield,
            IndexYieldChange = point.YieldChange,
            IndexDuration = point.Duration,
            IndexPreviousTradeDate = point.PreviousTradeDate,
            IndexDirection = point.Direction,
            IndexMoveIsMeaningful = point.IsMeaningful,
            IsSnapshot = point.SourceKind == OfzMarketIndexSourceKind.Snapshot,
            IsProvisional = point.IsProvisional
        };
    }

    private static void AddCashflowFindings(
        ICollection<OfzSummaryFinding> findings,
        OfzCashflowContext cashflowContext)
    {
        var link = cashflowContext.ActivityLinks
            .OrderBy(item => Math.Abs(item.DaysToEvent))
            .ThenByDescending(item => item.Value ?? 0)
            .ThenByDescending(item => item.NumTrades ?? 0)
            .FirstOrDefault();
        if (link is not null)
        {
            findings.Add(new OfzSummaryFinding
            {
                Id = $"cashflow-near-{NormalizeId(link.Event.EventType.ToString())}-{NormalizeId(link.SecId)}-{link.TradeDate:yyyy-MM-dd}-{link.Event.EventDate:yyyy-MM-dd}",
                Kind = OfzSummaryFindingKind.ActivityNearCashflowEvent,
                Priority = 720 + Math.Max(0, cashflowContext.EventWindowDays - Math.Abs(link.DaysToEvent)),
                Scope = OfzSummaryScope.Cashflow,
                Title = "Активность рядом с событием выпуска",
                Text = $"{link.ShortName}: активность {link.TradeDate:dd.MM.yyyy} рядом с {FormatCashflowEventType(link.Event.EventType)} {link.Event.EventDate:dd.MM.yyyy} ({FormatDaysToEvent(link.DaysToEvent)}).",
                Evidence = CreateCashflowEvidence(link),
                Limitations = link.Event.Limitations,
                DrillDown = new OfzSummaryDrillDown
                {
                    Target = OfzSummaryDrillDownTarget.CashflowEvent,
                    SecId = link.SecId,
                    TradeDate = link.Event.EventDate
                }
            });
        }

        var upcoming = cashflowContext.UpcomingEvents.FirstOrDefault();
        if (upcoming is not null)
        {
            findings.Add(new OfzSummaryFinding
            {
                Id = $"upcoming-cashflow-{NormalizeId(upcoming.EventType.ToString())}-{NormalizeId(upcoming.SecId)}-{upcoming.EventDate:yyyy-MM-dd}",
                Kind = OfzSummaryFindingKind.UpcomingCashflowEvent,
                Priority = 520,
                Scope = OfzSummaryScope.Cashflow,
                Title = "Ближайшее событие выпуска",
                Text = $"{upcoming.ShortName ?? upcoming.SecId}: {FormatCashflowEventType(upcoming.EventType)} {upcoming.EventDate:dd.MM.yyyy}.",
                Evidence = CreateCashflowEvidence(upcoming, null, null, null, null),
                Limitations = upcoming.Limitations,
                DrillDown = new OfzSummaryDrillDown
                {
                    Target = OfzSummaryDrillDownTarget.CashflowEvent,
                    SecId = upcoming.SecId,
                    TradeDate = upcoming.EventDate
                }
            });
        }

        if (!cashflowContext.HasEvents && cashflowContext.Limitations.Count > 0)
        {
            findings.Add(new OfzSummaryFinding
            {
                Id = "cashflow-data-limitation",
                Kind = OfzSummaryFindingKind.CashflowDataLimitation,
                Priority = 480,
                Scope = OfzSummaryScope.DataQuality,
                Title = "Календарь событий неполный",
                Text = "Cashflow-календарь недоступен или пуст; суммы и события не заменяются нулями.",
                Limitations = cashflowContext.Limitations
            });
        }
    }

    private static void AddSeasonalityFindings(
        ICollection<OfzSummaryFinding> findings,
        OfzSeasonalityContext seasonalityContext)
    {
        foreach (var finding in seasonalityContext.Findings
            .OrderByDescending(item => GetSeasonalityPriority(item))
            .ThenBy(item => item.TradeDate)
            .ThenBy(item => item.BucketKind)
            .Take(3))
        {
            var directionText = finding.Kind == OfzSeasonalityFindingKind.HighSeasonalActivity
                ? "выше"
                : "ниже";
            var title = finding.Kind == OfzSeasonalityFindingKind.HighSeasonalActivity
                ? "Активность выше сезонной базы"
                : "Активность ниже сезонной базы";

            findings.Add(new OfzSummaryFinding
            {
                Id = $"seasonality-{NormalizeId(finding.Kind.ToString())}-{NormalizeId(finding.BucketKind.ToString())}-{NormalizeId(finding.BucketKey)}-{finding.TradeDate:yyyy-MM-dd}",
                Kind = OfzSummaryFindingKind.SeasonalityActivity,
                Priority = GetSeasonalityPriority(finding),
                Scope = OfzSummaryScope.Seasonality,
                Title = title,
                Text = $"{finding.TradeDate:dd.MM.yyyy}: оборот {directionText} сезонной базы {finding.BucketLabel}, ratio {FormatRatio(finding.ValueRatio)}.",
                Evidence = CreateSeasonalityEvidence(finding),
                Limitations = finding.Limitations,
                DrillDown = new OfzSummaryDrillDown
                {
                    Target = OfzSummaryDrillDownTarget.HeatmapDate,
                    TradeDate = finding.TradeDate
                }
            });
        }

    }

    private static OfzFindingEvidence CreateSeasonalityEvidence(OfzSeasonalityFinding finding)
    {
        return new OfzFindingEvidence
        {
            TradeDate = finding.TradeDate,
            TotalValue = finding.ActualValue,
            TotalNumTrades = finding.ActualNumTrades,
            SeasonalityFindingKind = finding.Kind,
            SeasonalityBucketKind = finding.BucketKind,
            SeasonalityBucketKey = finding.BucketKey,
            SeasonalityBucketLabel = finding.BucketLabel,
            SeasonalityActualValue = finding.ActualValue,
            SeasonalityBaselineMedianValue = finding.BaselineMedianValue,
            SeasonalityValueRatio = finding.ValueRatio,
            SeasonalityActualNumTrades = finding.ActualNumTrades,
            SeasonalityBaselineMedianNumTrades = finding.BaselineMedianNumTrades,
            SeasonalityBaselineObservationCount = finding.BaselineObservationCount
        };
    }

    private static int GetSeasonalityPriority(OfzSeasonalityFinding finding)
    {
        var ratioDistance = finding.ValueRatio.HasValue
            ? Math.Abs(finding.ValueRatio.Value - 1)
            : 0;

        return 650 + Math.Min(80, (int)Math.Round(ratioDistance * 20));
    }

    private static OfzFindingEvidence CreateCashflowEvidence(OfzCashflowActivityLink link)
    {
        return CreateCashflowEvidence(
            link.Event,
            link.TradeDate,
            link.Value,
            link.NumTrades,
            link.ActivityScore,
            link.YieldMove,
            link.DaysToEvent);
    }

    private static OfzFindingEvidence CreateCashflowEvidence(
        OfzCashflowEvent cashflowEvent,
        DateTime? tradeDate,
        double? value,
        int? numTrades,
        double? activityScore,
        double? yieldMove = null,
        int? daysToEvent = null)
    {
        return new OfzFindingEvidence
        {
            TradeDate = tradeDate,
            SecId = cashflowEvent.SecId,
            ShortName = cashflowEvent.ShortName ?? cashflowEvent.SecId,
            Value = value,
            NumTrades = numTrades,
            ActivityScore = activityScore,
            YieldMove = yieldMove,
            CashflowEventType = cashflowEvent.EventType,
            CashflowEventDate = cashflowEvent.EventDate,
            CashflowDaysToEvent = daysToEvent,
            CashflowValue = cashflowEvent.Value,
            CashflowValueRub = cashflowEvent.ValueRub,
            CashflowValuePercent = cashflowEvent.ValuePercent,
            CashflowSourceKind = cashflowEvent.SourceKind,
            IsSnapshot = cashflowEvent.SourceKind != OfzCashflowSourceKind.Schedule,
            IsProvisional = cashflowEvent.IsProvisional
        };
    }

    private static void AddSpecialMetricFinding(
        ICollection<OfzSummaryFinding> findings,
        IReadOnlyCollection<OfzSpecialSummaryMetric> specialMetrics,
        OfzCouponType? couponTypeFilter)
    {
        if (couponTypeFilter == OfzCouponType.Floating)
        {
            var floatingRate = FindSpecialMetric(specialMetrics, OfzSpecialMetricKind.ImpliedFloatingRate);
            var cbrRate = FindSpecialMetric(specialMetrics, OfzSpecialMetricKind.ImpliedCbrRate);
            var spread = FindSpecialMetric(specialMetrics, OfzSpecialMetricKind.ImpliedFloatingRateSpread);
            var observedAt = MaxObservedAt(floatingRate, cbrRate, spread);

            if (floatingRate?.HasValue != true && cbrRate?.HasValue != true && spread?.HasValue != true)
            {
                return;
            }

            findings.Add(new OfzSummaryFinding
            {
                Id = "special-floating-metrics",
                Kind = OfzSummaryFindingKind.SpecialMetric,
                Priority = 690,
                Scope = OfzSummaryScope.SpecialMetric,
                Title = "Спецметрики ОФЗ-ПК",
                Text = $"ОФЗ-ПК: ожидаемая ставка купона {FormatSpecialPercent(floatingRate?.Value)}, ключевая ставка {FormatSpecialPercent(cbrRate?.Value)}, спред {FormatSpecialPoints(spread?.Value)} на {FormatSpecialDate(observedAt)}.",
                Evidence = new OfzFindingEvidence
                {
                    TradeDate = observedAt,
                    CouponType = OfzCouponType.Floating,
                    CouponTypeMarker = GetCouponTypeMarker(OfzCouponType.Floating),
                    SpecialMetricKind = OfzSpecialMetricKind.ImpliedFloatingRate,
                    SpecialMetricCode = floatingRate?.Code,
                    SpecialMetricCount = specialMetrics.Count(metric => metric.HasValue),
                    ImpliedFloatingRate = floatingRate?.Value,
                    ImpliedCbrRate = cbrRate?.Value,
                    ImpliedFloatingRateSpread = spread?.Value,
                    SpecialMetricAvailability = GetWorstSpecialAvailability(specialMetrics)
                },
                Limitations = BuildSpecialMetricLimitations(specialMetrics, couponTypeFilter),
                DrillDown = new OfzSummaryDrillDown
                {
                    Target = OfzSummaryDrillDownTarget.SegmentDetail,
                    CouponType = OfzCouponType.Floating
                }
            });
            return;
        }

        if (couponTypeFilter == OfzCouponType.InflationLinked)
        {
            var inflation = FindSpecialMetric(specialMetrics, OfzSpecialMetricKind.ImpliedInflation);
            if (inflation?.HasValue != true)
            {
                return;
            }

            findings.Add(new OfzSummaryFinding
            {
                Id = "special-inflation-metrics",
                Kind = OfzSummaryFindingKind.SpecialMetric,
                Priority = 690,
                Scope = OfzSummaryScope.SpecialMetric,
                Title = "Спецметрика ОФЗ-ИН",
                Text = $"ОФЗ-ИН: ожидаемая инфляция {FormatSpecialPercent(inflation.Value)} на {FormatSpecialDate(inflation.ObservedAt)}.",
                Evidence = new OfzFindingEvidence
                {
                    TradeDate = inflation.ObservedAt,
                    CouponType = OfzCouponType.InflationLinked,
                    CouponTypeMarker = GetCouponTypeMarker(OfzCouponType.InflationLinked),
                    SpecialMetricKind = OfzSpecialMetricKind.ImpliedInflation,
                    SpecialMetricCode = inflation.Code,
                    SpecialMetricCount = specialMetrics.Count(metric => metric.HasValue),
                    ImpliedInflation = inflation.Value,
                    SpecialMetricAvailability = GetWorstSpecialAvailability(specialMetrics)
                },
                Limitations = BuildSpecialMetricLimitations(specialMetrics, couponTypeFilter),
                DrillDown = new OfzSummaryDrillDown
                {
                    Target = OfzSummaryDrillDownTarget.SegmentDetail,
                    CouponType = OfzCouponType.InflationLinked
                }
            });
        }
    }

    private static IReadOnlyList<OfzSpecialSummaryMetric> BuildSpecialMetrics(
        IReadOnlyCollection<OfzDailyTrade> trades,
        OfzCouponType? couponTypeFilter,
        IEnumerable<CbrKeyRate>? cbrKeyRates)
    {
        var cbrKeyRateLookup = CbrKeyRateLookup.Create(cbrKeyRates);

        if (couponTypeFilter == OfzCouponType.Floating)
        {
            return
            [
                BuildSpecialMetric(
                    OfzSpecialMetricKind.ImpliedFloatingRate,
                    "implied_floating_rate",
                    "Ожидаемая ставка купона",
                    "%",
                    trades,
                    trade => FromSpecialHistory(trade.TradeDate, trade.ImpliedFloatingRate, OfzSpecialMetricSource.History)),
                BuildSpecialMetric(
                    OfzSpecialMetricKind.ImpliedCbrRate,
                    "implied_cbr_rate",
                    "Ключевая ставка",
                    "%",
                    trades,
                    trade => GetSpecialCbrObservation(trade, cbrKeyRateLookup)),
                BuildSpecialMetric(
                    OfzSpecialMetricKind.ImpliedFloatingRateSpread,
                    "implied_floating_rate_spread",
                    "Спред к ключевой ставке",
                    "п.п.",
                    trades,
                    trade =>
                    {
                        var cbrRate = GetSpecialCbrRateValue(trade, cbrKeyRateLookup);
                        return trade.ImpliedFloatingRate.HasValue &&
                            double.IsFinite(trade.ImpliedFloatingRate.Value) &&
                            cbrRate.HasValue &&
                            double.IsFinite(cbrRate.Value)
                                ? new SpecialSummaryObservation(
                                    trade.TradeDate.Date,
                                    trade.ImpliedFloatingRate.Value - cbrRate.Value,
                                    OfzSpecialMetricSource.Derived)
                                : null;
                    })
            ];
        }

        if (couponTypeFilter == OfzCouponType.InflationLinked)
        {
            return
            [
                BuildSpecialMetric(
                    OfzSpecialMetricKind.ImpliedInflation,
                    "implied_inflation",
                    "Ожидаемая инфляция",
                    "%",
                    trades,
                    trade => FromSpecialHistory(trade.TradeDate, trade.ImpliedInflation, OfzSpecialMetricSource.History))
            ];
        }

        return [];
    }

    private static OfzSpecialSummaryMetric BuildSpecialMetric(
        OfzSpecialMetricKind kind,
        string code,
        string label,
        string unit,
        IEnumerable<OfzDailyTrade> trades,
        Func<OfzDailyTrade, SpecialSummaryObservation?> selector)
    {
        var observations = trades
            .Select(selector)
            .Where(item => item.HasValue && double.IsFinite(item.Value.Value))
            .Select(item => item!.Value)
            .OrderBy(item => item.TradeDate)
            .ToArray();
        var latest = observations.LastOrDefault();

        return new OfzSpecialSummaryMetric
        {
            Kind = kind,
            Code = code,
            Label = label,
            Unit = unit,
            Value = observations.Length == 0 ? null : latest.Value,
            ObservedAt = observations.Length == 0 ? null : latest.TradeDate,
            Source = observations.Length == 0 ? OfzSpecialMetricSource.Missing : latest.Source,
            Availability = GetSpecialAvailability(observations.Length),
            HistoricalPointCount = observations.Length
        };
    }

    private static IReadOnlyList<OfzDataLimitation> BuildSpecialMetricLimitations(
        IReadOnlyCollection<OfzSpecialSummaryMetric> specialMetrics,
        OfzCouponType? couponTypeFilter)
    {
        if (couponTypeFilter is not (OfzCouponType.Floating or OfzCouponType.InflationLinked) ||
            specialMetrics.Count == 0)
        {
            return [];
        }

        List<OfzDataLimitation> limitations = [];
        if (specialMetrics.Any(metric => metric.Source == OfzSpecialMetricSource.CbrKeyRate))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.CbrKeyRateFallback,
                Scope = OfzSummaryScope.SpecialMetric,
                CouponType = couponTypeFilter,
                Text = "Ключевая ставка для части дат взята из официального сервиса ЦБ, потому что CBRCLOSE отсутствует."
            });
        }

        if (specialMetrics.Any(metric => metric.Availability == OfzSpecialMetricAvailability.Missing))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.MissingSpecialMetric,
                Scope = OfzSummaryScope.SpecialMetric,
                CouponType = couponTypeFilter,
                Text = "Часть специальных ISS-полей отсутствует и не заменяется нулем."
            });
        }

        if (specialMetrics.Any(metric => metric.Availability == OfzSpecialMetricAvailability.InsufficientHistory))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.InsufficientSpecialMetricHistory,
                Scope = OfzSummaryScope.SpecialMetric,
                CouponType = couponTypeFilter,
                Text = "Для части специальных ISS-полей меньше двух исторических наблюдений."
            });
        }

        if (specialMetrics.Any(metric => metric.IsProvisional))
        {
            limitations.Add(new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.Provisional,
                Scope = OfzSummaryScope.SpecialMetric,
                CouponType = couponTypeFilter,
                Text = "Часть специальных значений предварительная для текущей даты."
            });
        }

        return limitations;
    }

    private static OfzSpecialMetricAvailability GetSpecialAvailability(int observationsCount)
    {
        return observationsCount switch
        {
            0 => OfzSpecialMetricAvailability.Missing,
            1 => OfzSpecialMetricAvailability.InsufficientHistory,
            _ => OfzSpecialMetricAvailability.Historical
        };
    }

    private static SpecialSummaryObservation? FromSpecialHistory(
        DateTime tradeDate,
        double? value,
        OfzSpecialMetricSource source)
    {
        return value.HasValue && double.IsFinite(value.Value)
            ? new SpecialSummaryObservation(tradeDate.Date, value.Value, source)
            : null;
    }

    private static SpecialSummaryObservation? GetSpecialCbrObservation(
        OfzDailyTrade trade,
        CbrKeyRateLookup cbrKeyRateLookup)
    {
        if (trade.ImpliedCbrRate.HasValue && double.IsFinite(trade.ImpliedCbrRate.Value))
        {
            return new SpecialSummaryObservation(
                trade.TradeDate.Date,
                trade.ImpliedCbrRate.Value,
                OfzSpecialMetricSource.History);
        }

        var cbrRate = cbrKeyRateLookup.GetLatestOnOrBefore(trade.TradeDate);
        return cbrRate is null
            ? null
            : new SpecialSummaryObservation(
                trade.TradeDate.Date,
                cbrRate.Rate,
                OfzSpecialMetricSource.CbrKeyRate);
    }

    private static double? GetSpecialCbrRateValue(
        OfzDailyTrade trade,
        CbrKeyRateLookup cbrKeyRateLookup)
    {
        return trade.ImpliedCbrRate.HasValue && double.IsFinite(trade.ImpliedCbrRate.Value)
            ? trade.ImpliedCbrRate.Value
            : cbrKeyRateLookup.GetLatestOnOrBefore(trade.TradeDate)?.Rate;
    }

    private static OfzSpecialSummaryMetric? FindSpecialMetric(
        IEnumerable<OfzSpecialSummaryMetric> specialMetrics,
        OfzSpecialMetricKind kind)
    {
        return specialMetrics.FirstOrDefault(metric => metric.Kind == kind);
    }

    private static DateTime? MaxObservedAt(params OfzSpecialSummaryMetric?[] metrics)
    {
        var observedDates = metrics
            .Where(metric => metric?.ObservedAt.HasValue == true)
            .Select(metric => metric!.ObservedAt!.Value.Date)
            .ToArray();

        return observedDates.Length == 0 ? null : observedDates.Max();
    }

    private static OfzSpecialMetricAvailability? GetWorstSpecialAvailability(
        IReadOnlyCollection<OfzSpecialSummaryMetric> specialMetrics)
    {
        if (specialMetrics.Count == 0)
        {
            return null;
        }

        return specialMetrics
            .OrderByDescending(metric => GetSpecialAvailabilityRank(metric.Availability))
            .First()
            .Availability;
    }

    private static int GetSpecialAvailabilityRank(OfzSpecialMetricAvailability availability)
    {
        return availability switch
        {
            OfzSpecialMetricAvailability.Missing => 5,
            OfzSpecialMetricAvailability.InsufficientHistory => 4,
            OfzSpecialMetricAvailability.Provisional => 3,
            OfzSpecialMetricAvailability.SnapshotOnly => 2,
            OfzSpecialMetricAvailability.Historical => 1,
            _ => 0
        };
    }

    private static string FormatSpecialPercent(double? value)
    {
        return value.HasValue ? $"{value.Value:N2}%" : "n/a";
    }

    private static string FormatSpecialPoints(double? value)
    {
        return value.HasValue ? $"{value.Value:N2} п.п." : "n/a";
    }

    private static string FormatSpecialDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("dd.MM.yyyy") : "n/a";
    }

    private static IReadOnlyList<MarketBreadthDay> BuildBreadthDays(
        IReadOnlyCollection<OfzDailyTrade> trades,
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzMarketBreadthOptions options)
    {
        var tradesByKey = trades
            .GroupBy(trade => (trade.SecId, TradeDate: trade.TradeDate.Date))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(trade => trade.Value ?? 0)
                    .ThenByDescending(trade => trade.NumTrades ?? 0)
                    .First());
        var metricsByKey = metrics
            .GroupBy(metric => (metric.SecId, TradeDate: metric.TradeDate.Date))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(metric => metric.ActivityScore ?? 0)
                    .ThenByDescending(metric => metric.Value ?? 0)
                    .First());
        var liquidityByKey = liquidityMetrics
            .GroupBy(metric => (metric.SecId, TradeDate: metric.TradeDate.Date))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(GetWeakLiquidityPriority)
                    .ThenByDescending(metric => metric.Value ?? 0)
                    .First());
        var dates = tradesByKey.Keys.Select(key => key.TradeDate)
            .Concat(metricsByKey.Keys.Select(key => key.TradeDate))
            .Concat(liquidityByKey.Keys.Select(key => key.TradeDate))
            .Distinct()
            .OrderBy(date => date)
            .ToList();
        var lastYieldsBySecId = new Dictionary<string, double>(StringComparer.Ordinal);
        List<MarketBreadthDay> days = [];

        foreach (var tradeDate in dates)
        {
            var secIds = tradesByKey.Keys.Where(key => key.TradeDate == tradeDate).Select(key => key.SecId)
                .Concat(metricsByKey.Keys.Where(key => key.TradeDate == tradeDate).Select(key => key.SecId))
                .Concat(liquidityByKey.Keys.Where(key => key.TradeDate == tradeDate).Select(key => key.SecId))
                .Where(secId => !string.IsNullOrWhiteSpace(secId))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(secId => secId, StringComparer.Ordinal)
                .ToList();
            var observations = secIds
                .Select(secId =>
                {
                    tradesByKey.TryGetValue((secId, tradeDate), out var trade);
                    metricsByKey.TryGetValue((secId, tradeDate), out var metric);
                    liquidityByKey.TryGetValue((secId, tradeDate), out var liquidityMetric);
                    lastYieldsBySecId.TryGetValue(secId, out var previousYield);

                    return BuildBreadthObservation(
                        secId,
                        tradeDate,
                        trade,
                        metric,
                        liquidityMetric,
                        GetIssue(issuesBySecId, secId),
                        lastYieldsBySecId.ContainsKey(secId) ? previousYield : null,
                        options.UnchangedYieldMoveThreshold);
                })
                .ToList();

            days.Add(BuildBreadthDay(tradeDate, observations, options));

            foreach (var observation in observations.Where(item => item.CurrentYield.HasValue))
            {
                lastYieldsBySecId[observation.SecId] = observation.CurrentYield!.Value;
            }
        }

        return days;
    }

    private static BreadthObservation BuildBreadthObservation(
        string secId,
        DateTime tradeDate,
        OfzDailyTrade? trade,
        OfzActivityMetric? metric,
        OfzLiquidityMetric? liquidityMetric,
        OfzIssue? issue,
        double? previousYield,
        double unchangedThreshold)
    {
        var currentYield = trade?.PreferredYield ?? NormalizePositive(metric?.YieldValue);
        var yieldMove = currentYield.HasValue && previousYield.HasValue
            ? currentYield.Value - previousYield.Value
            : (double?)null;
        var direction = yieldMove.HasValue
            ? ClassifyDirection(yieldMove.Value, unchangedThreshold)
            : OfzYieldDirection.NotComparable;
        var value = NormalizePositive(metric?.Value) ?? NormalizePositive(trade?.Value) ?? NormalizePositive(liquidityMetric?.Value);
        var numTrades = NormalizePositive(metric?.NumTrades) ?? NormalizePositive(trade?.NumTrades) ?? NormalizePositive(liquidityMetric?.NumTrades);
        var couponType = issue?.CouponType ?? OfzCouponType.Unknown;

        return new BreadthObservation(
            secId,
            GetShortName(issue, secId),
            couponType,
            GetCouponTypeMarker(couponType),
            tradeDate,
            value,
            numTrades,
            currentYield,
            yieldMove,
            direction,
            metric?.ActivityScore,
            liquidityMetric?.LiquidityScore,
            liquidityMetric?.LiquidityBucket,
            liquidityMetric?.Spread,
            liquidityMetric?.IsSnapshot ?? false,
            liquidityMetric?.IsProvisional ?? false,
            liquidityMetric?.Status == OfzLiquidityMetricStatus.MissingQuotes,
            issue is null || couponType == OfzCouponType.Unknown);
    }

    private static MarketBreadthDay BuildBreadthDay(
        DateTime tradeDate,
        IReadOnlyList<BreadthObservation> observations,
        OfzMarketBreadthOptions options)
    {
        var comparable = observations.Where(item => item.YieldDirection != OfzYieldDirection.NotComparable).ToList();
        var yieldMoves = comparable.Where(item => item.YieldMove.HasValue).Select(item => item.YieldMove!.Value).ToArray();
        var activeIssueCount = observations.Count(item => item.IsActive);
        var totalValue = observations.Sum(item => item.Value ?? 0);
        var totalNumTrades = observations.Sum(item => item.NumTrades ?? 0);
        var concentration = BuildConcentration(observations, totalValue, options);
        var limitations = BuildBreadthLimitations(tradeDate, observations, comparable.Count).ToList();
        var isProvisional = observations.Any(item => item.IsSnapshot || item.IsProvisional);

        return new MarketBreadthDay
        {
            TradeDate = tradeDate,
            IssueCount = observations.Count,
            ComparableIssueCount = comparable.Count,
            NotComparableIssueCount = observations.Count - comparable.Count,
            ActiveIssueCount = activeIssueCount,
            ActiveIssueShare = observations.Count == 0 ? null : (double)activeIssueCount / observations.Count,
            TotalValue = totalValue > 0 ? totalValue : null,
            NumTrades = totalNumTrades > 0 ? totalNumTrades : null,
            Direction = new YieldDirectionBreakdown
            {
                YieldUpCount = comparable.Count(item => item.YieldDirection == OfzYieldDirection.Up),
                YieldDownCount = comparable.Count(item => item.YieldDirection == OfzYieldDirection.Down),
                UnchangedCount = comparable.Count(item => item.YieldDirection == OfzYieldDirection.Unchanged),
                NotComparableCount = observations.Count - comparable.Count,
                DominantDirection = GetDominantDirection(comparable, options.BroadMoveShare),
                DominantDirectionShare = GetDominantDirectionShare(comparable),
                MedianYieldMove = Median(yieldMoves)
            },
            Concentration = concentration,
            TypeShares = BuildTypeShares(observations, totalValue),
            TopContributors = concentration.TopIssues,
            Limitations = limitations,
            IsProvisional = isProvisional
        };
    }

    private static TurnoverConcentration BuildConcentration(
        IReadOnlyList<BreadthObservation> observations,
        double totalValue,
        OfzMarketBreadthOptions options)
    {
        var turnoverBase = observations
            .Where(item => item.Value is > 0)
            .OrderByDescending(item => item.Value!.Value)
            .ThenBy(item => item.SecId, StringComparer.Ordinal)
            .ToList();
        var top5Value = turnoverBase.Take(5).Sum(item => item.Value ?? 0);
        var top10Value = turnoverBase.Take(10).Sum(item => item.Value ?? 0);
        var top5Share = totalValue > 0 ? top5Value / totalValue : (double?)null;
        var top10Share = totalValue > 0 ? top10Value / totalValue : (double?)null;
        var contributors = turnoverBase
            .Take(options.MaxTopContributors)
            .Select(item => ToContributor(item, totalValue, GetContributorReason(item)))
            .ToList();

        return new TurnoverConcentration
        {
            TotalValue = totalValue > 0 ? totalValue : null,
            IssueBaseCount = turnoverBase.Count,
            Top5Value = top5Value > 0 ? top5Value : null,
            Top5Share = top5Share,
            Top10Value = top10Value > 0 ? top10Value : null,
            Top10Share = top10Share,
            TopIssues = contributors,
            IsHighConcentration = top5Share >= options.HighTop5Share || top10Share >= options.HighTop10Share
        };
    }

    private static IReadOnlyList<TypeTurnoverShare> BuildTypeShares(
        IReadOnlyList<BreadthObservation> observations,
        double totalValue)
    {
        return observations
            .GroupBy(item => item.CouponType)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var groupValue = group.Sum(item => item.Value ?? 0);
                var numTrades = group.Sum(item => item.NumTrades ?? 0);

                return new TypeTurnoverShare
                {
                    CouponType = group.Key,
                    CouponTypeMarker = GetCouponTypeMarker(group.Key),
                    IssueCount = group.Count(),
                    ActiveIssueCount = group.Count(item => item.IsActive),
                    TotalValue = groupValue > 0 ? groupValue : null,
                    ValueShare = totalValue > 0 ? groupValue / totalValue : null,
                    NumTrades = numTrades > 0 ? numTrades : null,
                    MissingTypeCount = group.Count(item => item.IsUnknownType)
                };
            })
            .ToList();
    }

    private static IEnumerable<OfzDataLimitation> BuildBreadthLimitations(
        DateTime tradeDate,
        IReadOnlyCollection<BreadthObservation> observations,
        int comparableIssueCount)
    {
        if (observations.Count > 0 && comparableIssueCount == 0)
        {
            yield return new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.InsufficientBaseline,
                Scope = OfzSummaryScope.MarketBreadth,
                TradeDate = tradeDate,
                Text = "Для breadth day нет пар текущая/предыдущая доходность."
            };
        }

        if (observations.Any(item => item.IsSnapshot))
        {
            yield return new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.SnapshotOnly,
                Scope = OfzSummaryScope.MarketBreadth,
                TradeDate = tradeDate,
                Text = "Часть breadth evidence построена по snapshot liquidity."
            };
        }

        if (observations.Any(item => item.IsProvisional))
        {
            yield return new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.Provisional,
                Scope = OfzSummaryScope.MarketBreadth,
                TradeDate = tradeDate,
                Text = "Часть breadth evidence предварительная для текущей даты."
            };
        }

        if (observations.Any(item => item.IsUnknownType))
        {
            yield return new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.UnknownCouponType,
                Scope = OfzSummaryScope.MarketBreadth,
                TradeDate = tradeDate,
                Text = "Для части выпусков тип ОФЗ не классифицирован."
            };
        }

        if (observations.Any(item => item.CouponType == OfzCouponType.Currency) &&
            observations.Any(item => item.CouponType is not OfzCouponType.Currency and not OfzCouponType.Unknown))
        {
            yield return new OfzDataLimitation
            {
                Kind = OfzDataLimitationKind.CurrencyMixed,
                Scope = OfzSummaryScope.MarketBreadth,
                TradeDate = tradeDate,
                Text = "В breadth day есть валютные и рублевые выпуски; обороты не нормализуются между валютами."
            };
        }
    }

    private static OfzFindingEvidence CreateEvidence(MarketBreadthDay day)
    {
        return new OfzFindingEvidence
        {
            TradeDate = day.TradeDate,
            IssueCount = day.IssueCount,
            ActiveIssueCount = day.ActiveIssueCount,
            ComparableIssueCount = day.ComparableIssueCount,
            NotComparableIssueCount = day.NotComparableIssueCount,
            ActiveIssueShare = day.ActiveIssueShare,
            TotalValue = day.TotalValue,
            TotalNumTrades = day.NumTrades,
            YieldUpCount = day.Direction.YieldUpCount,
            YieldDownCount = day.Direction.YieldDownCount,
            UnchangedCount = day.Direction.UnchangedCount,
            DominantDirection = day.Direction.DominantDirection,
            DominantDirectionShare = day.Direction.DominantDirectionShare,
            MedianYieldMove = day.Direction.MedianYieldMove,
            IssueBaseCount = day.Concentration.IssueBaseCount,
            Top5Value = day.Concentration.Top5Value,
            Top5Share = day.Concentration.Top5Share,
            Top10Value = day.Concentration.Top10Value,
            Top10Share = day.Concentration.Top10Share,
            IsSnapshot = day.Limitations.Any(limitation => limitation.Kind == OfzDataLimitationKind.SnapshotOnly),
            IsProvisional = day.IsProvisional
        };
    }

    private static OfzDominantYieldDirection GetDominantDirection(
        IReadOnlyCollection<BreadthObservation> comparable,
        double broadMoveShare)
    {
        if (comparable.Count == 0)
        {
            return OfzDominantYieldDirection.None;
        }

        var up = comparable.Count(item => item.YieldDirection == OfzYieldDirection.Up);
        var down = comparable.Count(item => item.YieldDirection == OfzYieldDirection.Down);
        var unchanged = comparable.Count(item => item.YieldDirection == OfzYieldDirection.Unchanged);
        var maxCount = Math.Max(up, Math.Max(down, unchanged));
        var share = (double)maxCount / comparable.Count;

        if (share < broadMoveShare)
        {
            return OfzDominantYieldDirection.Mixed;
        }

        if (maxCount == unchanged)
        {
            return OfzDominantYieldDirection.Flat;
        }

        return maxCount == up ? OfzDominantYieldDirection.Up : OfzDominantYieldDirection.Down;
    }

    private static double? GetDominantDirectionShare(IReadOnlyCollection<BreadthObservation> comparable)
    {
        if (comparable.Count == 0)
        {
            return null;
        }

        var up = comparable.Count(item => item.YieldDirection == OfzYieldDirection.Up);
        var down = comparable.Count(item => item.YieldDirection == OfzYieldDirection.Down);
        var unchanged = comparable.Count(item => item.YieldDirection == OfzYieldDirection.Unchanged);
        return (double)Math.Max(up, Math.Max(down, unchanged)) / comparable.Count;
    }

    private static MarketBreadthContributor ToContributor(
        BreadthObservation item,
        double totalValue,
        MarketBreadthContributorReason reason)
    {
        return new MarketBreadthContributor
        {
            SecId = item.SecId,
            ShortName = item.ShortName,
            CouponType = item.CouponType,
            CouponTypeMarker = item.CouponTypeMarker,
            TradeDate = item.TradeDate,
            Value = item.Value,
            ValueShare = item.Value.HasValue && totalValue > 0 ? item.Value.Value / totalValue : null,
            NumTrades = item.NumTrades,
            YieldMove = item.YieldMove,
            YieldDirection = item.YieldDirection,
            ActivityScore = item.ActivityScore,
            LiquidityScore = item.LiquidityScore,
            LiquidityBucket = item.LiquidityBucket,
            Spread = item.Spread,
            Reason = reason
        };
    }

    private static MarketBreadthContributorReason GetContributorReason(BreadthObservation item)
    {
        if (item.IsWeakLiquidity)
        {
            return MarketBreadthContributorReason.WeakLiquidity;
        }

        if (item.YieldDirection == OfzYieldDirection.Up)
        {
            return MarketBreadthContributorReason.YieldUp;
        }

        if (item.YieldDirection == OfzYieldDirection.Down)
        {
            return MarketBreadthContributorReason.YieldDown;
        }

        return item.ActivityScore is > 0
            ? MarketBreadthContributorReason.HighActivity
            : MarketBreadthContributorReason.TopTurnover;
    }

    private static OfzYieldDirection ClassifyDirection(double yieldMove, double unchangedThreshold)
    {
        if (yieldMove > unchangedThreshold)
        {
            return OfzYieldDirection.Up;
        }

        if (yieldMove < -unchangedThreshold)
        {
            return OfzYieldDirection.Down;
        }

        return OfzYieldDirection.Unchanged;
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

    private static IReadOnlyList<OfzDataLimitation> BuildIndexLimitations(
        IReadOnlyCollection<OfzIndexContextDay> indexContextDays,
        IReadOnlyList<OfzDataLimitation> globalLimitations)
    {
        return globalLimitations
            .Concat(indexContextDays.SelectMany(day => day.Limitations))
            .GroupBy(limitation => (
                limitation.Kind,
                limitation.Scope,
                limitation.SecId ?? string.Empty,
                limitation.TradeDate?.Date,
                limitation.Text))
            .Select(group => group.First())
            .ToList();
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
        IReadOnlyCollection<OfzIndexContextDay> indexContextDays,
        OfzCashflowContext cashflowContext,
        OfzSeasonalityContext seasonalityContext,
        IReadOnlyCollection<OfzSpecialSummaryMetric> specialMetrics,
        IReadOnlyDictionary<string, OfzIssue> issuesBySecId,
        OfzCouponType? couponTypeFilter)
    {
        var dates = trades.Select(trade => trade.TradeDate.Date)
            .Concat(metrics.Select(metric => metric.TradeDate.Date))
            .Concat(liquidityMetrics.Select(metric => metric.TradeDate.Date))
            .Concat(indexContextDays.Select(day => day.TradeDate.Date))
            .Distinct()
            .Count();

        return new OfzSummarySourceCounts
        {
            Issues = issuesBySecId.Values.Count(issue => !couponTypeFilter.HasValue || issue.CouponType == couponTypeFilter.Value),
            Trades = trades.Count,
            ActivityMetrics = metrics.Count,
            LiquidityMetrics = liquidityMetrics.Count(metric => !metric.IsSnapshot),
            SnapshotLiquidityMetrics = liquidityMetrics.Count(metric => metric.IsSnapshot),
            SpecialMetricObservations = specialMetrics.Sum(metric => metric.HistoricalPointCount),
            SpecialMetricSeries = specialMetrics.Count(metric => metric.HasValue),
            IndexPoints = indexContextDays.SelectMany(day => day.Points).Count(),
            IndexContextDays = indexContextDays.Count,
            CashflowEvents = cashflowContext.Events
                .Concat(cashflowContext.UpcomingEvents)
                .GroupBy(item => new { item.SecId, item.EventType, item.EventDate, item.SourceKind, item.SourceKey })
                .Count(),
            CashflowIssues = cashflowContext.IssueCalendars.Count(calendar => calendar.HasEvents),
            SeasonalityObservations = seasonalityContext.ObservationCount,
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

    private static IEnumerable<T> ApplyDateRange<T>(
        IEnumerable<T> items,
        DateTime startDate,
        DateTime endDate)
        where T : class
    {
        return items.Where(item =>
        {
            var date = GetTradeDate(item);
            return !date.HasValue || (date.Value >= startDate && date.Value <= endDate);
        });
    }

    private static (DateTime StartDate, DateTime EndDate) NormalizeOutputRange(
        DateTime startDate,
        DateTime endDate,
        DateTime outputStartDate,
        DateTime outputEndDate)
    {
        if (outputStartDate > outputEndDate)
        {
            (outputStartDate, outputEndDate) = (outputEndDate, outputStartDate);
        }

        if (outputStartDate < startDate)
        {
            outputStartDate = startDate;
        }

        if (outputEndDate > endDate)
        {
            outputEndDate = endDate;
        }

        return (outputStartDate, outputEndDate);
    }

    private static DateTime? GetTradeDate<T>(T item)
    {
        return item switch
        {
            OfzDailyTrade trade => trade.TradeDate.Date,
            OfzActivityMetric metric => metric.TradeDate.Date,
            OfzLiquidityMetric metric => metric.TradeDate.Date,
            MarketBreadthDay day => day.TradeDate.Date,
            OfzIndexContextDay day => day.TradeDate.Date,
            _ => null
        };
    }

    private static DateTime GetMinDate(
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyCollection<MarketBreadthDay> breadthDays,
        IReadOnlyCollection<OfzIndexContextDay> indexContextDays,
        DateTime fallback)
    {
        return metrics.Select(metric => metric.TradeDate.Date)
            .Concat(liquidityMetrics.Select(metric => metric.TradeDate.Date))
            .Concat(breadthDays.Select(day => day.TradeDate.Date))
            .Concat(indexContextDays.Select(day => day.TradeDate.Date))
            .DefaultIfEmpty(fallback)
            .Min();
    }

    private static DateTime GetMaxDate(
        IReadOnlyCollection<OfzActivityMetric> metrics,
        IReadOnlyCollection<OfzLiquidityMetric> liquidityMetrics,
        IReadOnlyCollection<MarketBreadthDay> breadthDays,
        IReadOnlyCollection<OfzIndexContextDay> indexContextDays,
        DateTime fallback)
    {
        return metrics.Select(metric => metric.TradeDate.Date)
            .Concat(liquidityMetrics.Select(metric => metric.TradeDate.Date))
            .Concat(breadthDays.Select(day => day.TradeDate.Date))
            .Concat(indexContextDays.Select(day => day.TradeDate.Date))
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
        return OfzIssueClassifier.GetCouponTypeMarker(couponType);
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

    private static string FormatNullablePercent(double? value)
    {
        return value.HasValue ? value.Value.ToString("P0") : "n/a";
    }

    private static string FormatSignedPercent(double? value)
    {
        return value.HasValue ? value.Value.ToString("+0.00%;-0.00%;0.00%") : "n/a";
    }

    private static string FormatSignedPoints(double? value)
    {
        return value.HasValue ? value.Value.ToString("+0.00;-0.00;0.00") + " п.п." : "n/a";
    }

    private static string FormatRatio(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.00") + "x" : "n/a";
    }

    private static string FormatCashflowEventType(OfzCashflowEventType eventType)
    {
        return eventType switch
        {
            OfzCashflowEventType.Coupon => "купоном",
            OfzCashflowEventType.Amortization => "амортизацией",
            OfzCashflowEventType.Maturity => "погашением",
            OfzCashflowEventType.Offer => "офертой",
            OfzCashflowEventType.Buyback => "buyback",
            OfzCashflowEventType.CallOption => "call option",
            OfzCashflowEventType.PutOption => "put option",
            _ => eventType.ToString()
        };
    }

    private static string FormatDaysToEvent(int daysToEvent)
    {
        return daysToEvent switch
        {
            0 => "в день события",
            > 0 => $"за {daysToEvent} дн.",
            _ => $"через {Math.Abs(daysToEvent)} дн. после события"
        };
    }

    private static double? NormalizePositive(double? value)
    {
        return value is > 0 ? value : null;
    }

    private static int? NormalizePositive(int? value)
    {
        return value is > 0 ? value : null;
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToLowerInvariant();
    }

    private readonly record struct SpecialSummaryObservation(
        DateTime TradeDate,
        double Value,
        OfzSpecialMetricSource Source);

    private sealed record BreadthObservation(
        string SecId,
        string ShortName,
        OfzCouponType CouponType,
        string CouponTypeMarker,
        DateTime TradeDate,
        double? Value,
        int? NumTrades,
        double? CurrentYield,
        double? YieldMove,
        OfzYieldDirection YieldDirection,
        double? ActivityScore,
        double? LiquidityScore,
        OfzLiquidityBucket? LiquidityBucket,
        double? Spread,
        bool IsSnapshot,
        bool IsProvisional,
        bool IsMissingQuotes,
        bool IsUnknownType)
    {
        public bool IsActive => Value is > 0 || NumTrades is > 0;

        public bool IsWeakLiquidity =>
            IsMissingQuotes ||
            LiquidityBucket is OfzLiquidityBucket.Weak or OfzLiquidityBucket.Problem or OfzLiquidityBucket.MissingData;
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
        ArgumentOutOfRangeException.ThrowIfNegative(options.Breadth.UnchangedYieldMoveThreshold);
        ArgumentOutOfRangeException.ThrowIfNegative(options.Breadth.BroadMoveShare);
        ArgumentOutOfRangeException.ThrowIfNegative(options.Breadth.HighTop5Share);
        ArgumentOutOfRangeException.ThrowIfNegative(options.Breadth.HighTop10Share);
        ArgumentOutOfRangeException.ThrowIfNegative(options.Breadth.TypeDominanceShare);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Breadth.MinimumComparableIssuesForBroadMove);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Breadth.MaxTopContributors);
        ArgumentOutOfRangeException.ThrowIfNegative(options.IndexContext.MeaningfulCloseChangePercent);
        ArgumentOutOfRangeException.ThrowIfNegative(options.IndexContext.MeaningfulYieldChange);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Seasonality.MinWeekdayBaselineObservations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Seasonality.MinMonthBaselineObservations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Seasonality.HighActivityRatioThreshold);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Seasonality.LowActivityRatioThreshold);
        ArgumentOutOfRangeException.ThrowIfNegative(options.Seasonality.UnchangedYieldMoveThreshold);

        if (options.IndexContext.MeaningfulCloseChangePercent > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "MeaningfulCloseChangePercent must be a fraction between 0 and 1.");
        }

        if (options.Seasonality.LowActivityRatioThreshold >= options.Seasonality.HighActivityRatioThreshold)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "LowActivityRatioThreshold must be less than HighActivityRatioThreshold.");
        }
    }
}
