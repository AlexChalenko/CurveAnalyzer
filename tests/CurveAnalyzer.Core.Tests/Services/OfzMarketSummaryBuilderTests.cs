using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Services;

public class OfzMarketSummaryBuilderTests
{
    private static readonly DateTime StartDate = new(2026, 04, 01);
    private static readonly DateTime EndDate = new(2026, 04, 20);

    [Fact]
    public void BuildMarketSummary_ReturnsRankedConciseFindingsWithEvidence()
    {
        var summary = BuildMarketSummary(
            SeedInput(),
            new OfzMarketSummaryOptions { MaxFindings = 5 });

        Assert.InRange(summary.Findings.Count, 1, 5);
        Assert.Equal(
            summary.Findings.OrderByDescending(finding => finding.Priority).Select(finding => finding.Id),
            summary.Findings.Select(finding => finding.Id));
        Assert.All(summary.Findings, finding =>
        {
            Assert.False(string.IsNullOrWhiteSpace(finding.Id));
            Assert.False(string.IsNullOrWhiteSpace(finding.Title));
            Assert.False(string.IsNullOrWhiteSpace(finding.Text));
            Assert.True(HasEvidenceOrLimitation(finding), finding.Id);
        });
        Assert.Contains(summary.Findings, finding => finding.Kind == OfzSummaryFindingKind.MarketActivity);
        Assert.Contains(summary.Findings, finding =>
            finding.Evidence.TotalValue is > 0 ||
            finding.Evidence.Value is > 0);
    }

    [Fact]
    public void BuildMarketSummary_AddsDataLimitationWhenLiquidityMissing()
    {
        var seed = SeedInput();
        var input = new OfzMarketSummaryInput
        {
            Issues = seed.Issues,
            Trades = seed.Trades,
            ActivityMetrics = seed.ActivityMetrics,
            LiquidityMetrics =
            [
                LiquidityMetric(
                    "SU26238RMFS4",
                    EndDate,
                    spread: null,
                    OfzSpreadSource.Missing,
                    OfzLiquidityBucket.MissingData,
                    OfzLiquidityMetricStatus.MissingQuotes,
                    value: 2_000_000_000,
                    numTrades: 1_100)
            ]
        };

        var summary = BuildMarketSummary(input);

        Assert.Contains(summary.Limitations, limitation =>
            limitation.Kind == OfzDataLimitationKind.MissingQuotes ||
            limitation.Kind == OfzDataLimitationKind.MissingSpread);
        Assert.Contains(summary.Findings, finding =>
            finding.Kind is OfzSummaryFindingKind.WeakLiquidity or OfzSummaryFindingKind.DataQuality &&
            finding.Limitations.Any(limitation =>
                limitation.Kind == OfzDataLimitationKind.MissingQuotes ||
                limitation.Kind == OfzDataLimitationKind.MissingSpread));
        Assert.All(summary.Findings.Select(finding => finding.Evidence), evidence =>
        {
            if (evidence.SpreadSource == OfzSpreadSource.Missing)
            {
                Assert.Null(evidence.Spread);
            }
        });
    }

    [Fact]
    public void BuildMarketSummary_ReturnsInsufficientDataState()
    {
        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            Issues = [Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном")],
            Trades = [],
            ActivityMetrics = [],
            LiquidityMetrics = []
        });

        Assert.Empty(summary.Findings);
        Assert.Contains(summary.Limitations, limitation =>
            limitation.Kind is OfzDataLimitationKind.NoData or OfzDataLimitationKind.InsufficientBaseline);
        Assert.Equal(1, summary.SourceCounts.Issues);
        Assert.Equal(0, summary.SourceCounts.ActivityMetrics);
        Assert.Equal(0, summary.SourceCounts.LiquidityMetrics);
    }

    [Fact]
    public void BuildMarketSummary_AvoidsRecommendationLanguage()
    {
        var summary = BuildMarketSummary(SeedInput());

        Assert.All(summary.Findings, finding =>
        {
            Assert.False(ContainsRecommendationLanguage(finding.Title), finding.Title);
            Assert.False(ContainsRecommendationLanguage(finding.Text), finding.Text);
        });
        Assert.All(summary.Limitations, limitation =>
            Assert.False(ContainsRecommendationLanguage(limitation.Text), limitation.Text));
    }

    [Fact]
    public void BuildMarketSummary_AttachesIssueAndDateDrillDown()
    {
        var summary = BuildMarketSummary(SeedInput());

        var issueFinding = Assert.Single(summary.Findings, finding =>
            finding.Kind == OfzSummaryFindingKind.RepeatedIssue &&
            finding.Evidence.SecId == "SU26238RMFS4");

        Assert.NotNull(issueFinding.DrillDown);
        Assert.Equal("SU26238RMFS4", issueFinding.DrillDown.SecId);
        Assert.Equal(EndDate, issueFinding.DrillDown.TradeDate);
        Assert.Equal("IssueDetail", issueFinding.DrillDown.Target.ToString());
    }

    [Fact]
    public void BuildMarketSummary_MarksSnapshotAndProvisionalEvidence()
    {
        var observedAt = new DateTime(2026, 04, 20, 12, 0, 0, DateTimeKind.Utc);
        var seed = SeedInput();
        var input = new OfzMarketSummaryInput
        {
            Issues = seed.Issues,
            Trades = seed.Trades,
            ActivityMetrics = seed.ActivityMetrics,
            LiquidityMetrics =
            [
                LiquidityMetric(
                    "SU26238RMFS4",
                    EndDate,
                    spread: 0.7,
                    OfzSpreadSource.Provided,
                    OfzLiquidityBucket.Problem,
                    OfzLiquidityMetricStatus.SnapshotOnly,
                    value: 2_000_000_000,
                    numTrades: 1_100,
                    isSnapshot: true,
                    isProvisional: true,
                    observedAt: observedAt)
            ]
        };

        var summary = BuildMarketSummary(input);

        var snapshotFinding = Assert.Single(summary.Findings, finding =>
            finding.Kind == OfzSummaryFindingKind.WeakLiquidity &&
            finding.Evidence.IsSnapshot);
        Assert.True(snapshotFinding.Evidence.IsProvisional);
        Assert.Contains(snapshotFinding.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.SnapshotOnly);
        Assert.Contains(snapshotFinding.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.Provisional);
    }

    [Fact]
    public void BuildMarketSummary_BuildsSegmentSummaries()
    {
        var summary = BuildMarketSummary(SeedInput());

        var fixedSegment = Assert.Single(summary.Segments, segment => segment.CouponType == OfzCouponType.Fixed);
        Assert.Equal("ОФЗ-ПД", fixedSegment.CouponTypeMarker);
        Assert.True(fixedSegment.IssueCount >= 2);
        Assert.True(fixedSegment.ActiveIssueCount >= 2);
        Assert.True(fixedSegment.TotalValue >= 3_400_000_000);
        Assert.True(fixedSegment.TotalNumTrades >= 1_280);
        Assert.NotEmpty(fixedSegment.TopIssues);
        Assert.Contains(fixedSegment.TopIssues, issue => issue.Reason == OfzIssueFocusReason.TopValue);

        var floatingSegment = Assert.Single(summary.Segments, segment => segment.CouponType == OfzCouponType.Floating);
        Assert.Equal("ОФЗ-ПК", floatingSegment.CouponTypeMarker);
        Assert.True(floatingSegment.TotalValue > 0);
    }

    [Fact]
    public void BuildMarketSummary_HonorsCouponTypeFilterAndMarksCurrency()
    {
        var seed = SeedInput();
        var input = new OfzMarketSummaryInput
        {
            StartDate = StartDate,
            EndDate = EndDate,
            CouponTypeFilter = OfzCouponType.Currency,
            Issues =
            [
                .. seed.Issues,
                Issue("RU000A0ZZZ99", "ОФЗ USD", "Валютная", faceUnit: "USD", currencyId: "USD")
            ],
            Trades = seed.Trades,
            ActivityMetrics =
            [
                .. seed.ActivityMetrics,
                ActivityMetric("RU000A0ZZZ99", EndDate, 9, 500_000, 30, yieldMove: 0.03)
            ],
            LiquidityMetrics =
            [
                .. seed.LiquidityMetrics,
                LiquidityMetric(
                    "RU000A0ZZZ99",
                    EndDate,
                    spread: 0.12,
                    OfzSpreadSource.Provided,
                    OfzLiquidityBucket.Good,
                    OfzLiquidityMetricStatus.Ready,
                    value: 500_000,
                    numTrades: 30)
            ]
        };

        var summary = BuildMarketSummary(input);

        Assert.Equal(OfzCouponType.Currency, summary.CouponTypeFilter);
        Assert.All(summary.Segments, segment => Assert.Equal(OfzCouponType.Currency, segment.CouponType));
        Assert.All(summary.Findings.Where(finding => finding.Evidence.CouponType.HasValue), finding =>
            Assert.Equal(OfzCouponType.Currency, finding.Evidence.CouponType));
        Assert.Contains(summary.Segments, segment => segment.CouponTypeMarker == "Валютная");
    }

    [Fact]
    public void MarketSummary_SerializesStableContractFields()
    {
        var summary = BuildMarketSummary(SeedInput());
        var json = JsonSerializer.Serialize(summary, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("1.6", root.GetProperty("schemaVersion").GetString());
        AssertJsonDateOnly(root.GetProperty("startDate"));
        AssertJsonDateOnly(root.GetProperty("endDate"));
        AssertJsonDateOnly(root.GetProperty("insightStartDate"));
        AssertJsonDateOnly(root.GetProperty("insightEndDate"));
        Assert.Contains("T", root.GetProperty("generatedAt").GetString());
        Assert.True(root.TryGetProperty("findings", out _));
        Assert.True(root.TryGetProperty("segments", out _));
        Assert.True(root.TryGetProperty("breadthDays", out _));
        Assert.True(root.TryGetProperty("indexContextDays", out _));
        Assert.True(root.TryGetProperty("indexSegments", out _));
        Assert.True(root.TryGetProperty("seasonalityContext", out _));
        Assert.True(root.TryGetProperty("specialMetrics", out _));
        Assert.True(root.TryGetProperty("limitations", out _));
        Assert.True(root.TryGetProperty("sourceCounts", out _));
        Assert.NotEmpty(root.GetProperty("breadthDays").EnumerateArray());

        var breadthDay = root.GetProperty("breadthDays").EnumerateArray().First();
        AssertJsonDateOnly(breadthDay.GetProperty("tradeDate"));
        Assert.True(breadthDay.TryGetProperty("direction", out var direction));
        Assert.True(direction.TryGetProperty("yieldUpCount", out _));
        Assert.True(direction.TryGetProperty("yieldDownCount", out _));
        Assert.True(direction.TryGetProperty("notComparableCount", out _));
        Assert.True(breadthDay.TryGetProperty("concentration", out var concentration));
        Assert.True(concentration.TryGetProperty("top5Share", out _));
        Assert.True(concentration.TryGetProperty("top10Share", out _));
        Assert.True(concentration.TryGetProperty("topIssues", out var topIssues));
        AssertJsonDateOnly(topIssues.EnumerateArray().First().GetProperty("tradeDate"));
        Assert.True(breadthDay.TryGetProperty("typeShares", out _));
        Assert.True(breadthDay.TryGetProperty("topContributors", out var topContributors));
        AssertJsonDateOnly(topContributors.EnumerateArray().First().GetProperty("tradeDate"));

        var finding = root.GetProperty("findings").EnumerateArray().First();
        Assert.True(finding.TryGetProperty("id", out _));
        Assert.True(finding.TryGetProperty("kind", out _));
        Assert.Equal(JsonValueKind.String, finding.GetProperty("kind").ValueKind);
        Assert.True(finding.TryGetProperty("priority", out _));
        Assert.True(finding.TryGetProperty("scope", out _));
        Assert.Equal(JsonValueKind.String, finding.GetProperty("scope").ValueKind);
        Assert.True(finding.TryGetProperty("title", out _));
        Assert.True(finding.TryGetProperty("text", out _));
        Assert.True(finding.TryGetProperty("evidence", out _));
        Assert.True(finding.TryGetProperty("limitations", out _));

        foreach (var findingElement in root.GetProperty("findings").EnumerateArray())
        {
            var evidence = findingElement.GetProperty("evidence");
            if (evidence.TryGetProperty("tradeDate", out var tradeDate) &&
                tradeDate.ValueKind == JsonValueKind.String)
            {
                AssertJsonDateOnly(tradeDate);
            }
        }
    }

    [Fact]
    public void BuildMarketSummary_ClassifiesBreadthDirectionWithoutLookAhead()
    {
        var startDate = new DateTime(2026, 04, 01);
        var day2 = startDate.AddDays(1);
        var day3 = startDate.AddDays(2);
        var issue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");
        var input = new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = day3,
            Issues = [issue],
            Trades =
            [
                Trade(issue.SecId, startDate, 100_000_000, 10, 13.00, 800),
                Trade(issue.SecId, day2, 110_000_000, 11, 13.10, 800),
                Trade(issue.SecId, day3, 120_000_000, 12, 12.90, 800)
            ],
            ActivityMetrics = [],
            LiquidityMetrics = []
        };

        var summary = BuildMarketSummary(input);

        Assert.Equal(OfzYieldDirection.NotComparable, DirectionFor(summary, startDate, issue.SecId));
        Assert.Equal(OfzYieldDirection.Up, DirectionFor(summary, day2, issue.SecId));
        Assert.Equal(OfzYieldDirection.Down, DirectionFor(summary, day3, issue.SecId));

        var withoutFuture = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = day2,
            Issues = input.Issues,
            Trades = input.Trades.Take(2),
            ActivityMetrics = input.ActivityMetrics,
            LiquidityMetrics = input.LiquidityMetrics
        });
        Assert.Equal(OfzYieldDirection.Up, DirectionFor(withoutFuture, day2, issue.SecId));
    }

    [Fact]
    public void BuildMarketSummary_UsesEarlierHistoryForFirstInsightBreadthDay()
    {
        var startDate = new DateTime(2026, 04, 01);
        var insightStartDate = startDate.AddDays(2);
        var issue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");

        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = insightStartDate,
            InsightStartDate = insightStartDate,
            InsightEndDate = insightStartDate,
            Issues = [issue],
            Trades =
            [
                Trade(issue.SecId, startDate, 100_000_000, 10, 13.00, 800),
                Trade(issue.SecId, insightStartDate, 120_000_000, 12, 13.20, 800)
            ],
            ActivityMetrics = [],
            LiquidityMetrics = []
        });

        var day = Assert.Single(summary.BreadthDays);
        Assert.Equal(insightStartDate, day.TradeDate);
        Assert.Equal(1, day.ComparableIssueCount);
        Assert.Equal(0, day.NotComparableIssueCount);
        Assert.Equal(1, day.Direction.YieldUpCount);
        Assert.Equal(OfzYieldDirection.Up, DirectionFor(summary, insightStartDate, issue.SecId));
    }

    [Fact]
    public void BuildMarketSummary_BuildsDirectionCountsAndComparableBase()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var issues = new[]
        {
            Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном"),
            Issue("SU26239RMFS2", "ОФЗ 26239", "Фикс с известным купоном"),
            Issue("SU29019RMFS0", "ОФЗ 29019", "Флоатер")
        };
        var input = new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            Issues = issues,
            Trades =
            [
                Trade(issues[0].SecId, startDate, 100_000_000, 10, 13.00, 800),
                Trade(issues[1].SecId, startDate, 100_000_000, 10, 13.00, 800),
                Trade(issues[2].SecId, startDate, 100_000_000, 10, 13.00, 800),
                Trade(issues[0].SecId, tradeDate, 100_000_000, 10, 13.20, 800),
                Trade(issues[1].SecId, tradeDate, 100_000_000, 10, 12.80, 800),
                Trade(issues[2].SecId, tradeDate, 100_000_000, 10, 13.005, 800)
            ],
            ActivityMetrics = [],
            LiquidityMetrics = []
        };

        var day = BuildMarketSummary(input).BreadthDays.Single(item => item.TradeDate == tradeDate);

        Assert.Equal(3, day.ComparableIssueCount);
        Assert.Equal(0, day.NotComparableIssueCount);
        Assert.Equal(1, day.Direction.YieldUpCount);
        Assert.Equal(1, day.Direction.YieldDownCount);
        Assert.Equal(1, day.Direction.UnchangedCount);
        Assert.Equal(OfzDominantYieldDirection.Mixed, day.Direction.DominantDirection);
    }

    [Fact]
    public void BuildMarketSummary_DoesNotTreatMissingPreviousYieldAsUnchanged()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var issueWithPrevious = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");
        var issueWithoutPrevious = Issue("SU26239RMFS2", "ОФЗ 26239", "Фикс с известным купоном");
        var input = new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            Issues = [issueWithPrevious, issueWithoutPrevious],
            Trades =
            [
                Trade(issueWithPrevious.SecId, startDate, 100_000_000, 10, 13.00, 800),
                Trade(issueWithPrevious.SecId, tradeDate, 100_000_000, 10, 13.005, 800),
                Trade(issueWithoutPrevious.SecId, tradeDate, 100_000_000, 10, 12.50, 800)
            ],
            ActivityMetrics = [],
            LiquidityMetrics = []
        };

        var day = BuildMarketSummary(input).BreadthDays.Single(item => item.TradeDate == tradeDate);

        Assert.Equal(1, day.ComparableIssueCount);
        Assert.Equal(1, day.NotComparableIssueCount);
        Assert.Equal(1, day.Direction.UnchangedCount);
        Assert.Equal(1, day.Direction.NotComparableCount);
    }

    [Fact]
    public void BuildMarketSummary_BuildsTopTurnoverConcentration()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var issues = Enumerable.Range(0, 6)
            .Select(index => Issue($"SU2623{index}RMFS{index}", $"ОФЗ 2623{index}", "Фикс с известным купоном"))
            .ToArray();
        var values = new[] { 50_000_000d, 20_000_000d, 10_000_000d, 10_000_000d, 5_000_000d, 5_000_000d };
        var trades = issues
            .Select(issue => Trade(issue.SecId, startDate, 1_000_000, 1, 13.00, 800))
            .Concat(issues.Select((issue, index) => Trade(issue.SecId, tradeDate, values[index], 10 + index, 13.10 + index * 0.01, 800)))
            .ToArray();

        var day = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            Issues = issues,
            Trades = trades,
            ActivityMetrics = [],
            LiquidityMetrics = []
        }).BreadthDays.Single(item => item.TradeDate == tradeDate);

        Assert.Equal(6, day.Concentration.IssueBaseCount);
        Assert.Equal(95_000_000, day.Concentration.Top5Value);
        Assert.Equal(0.95, day.Concentration.Top5Share!.Value, 3);
        Assert.Equal(100_000_000, day.Concentration.Top10Value);
        Assert.Equal(1.0, day.Concentration.Top10Share!.Value, 3);
        Assert.True(day.Concentration.IsHighConcentration);
        Assert.Equal(6, day.Concentration.TopIssues.Count);
    }

    [Fact]
    public void BuildMarketSummary_ExcludesMissingTurnoverFromConcentrationBase()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var issues = new[]
        {
            Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном"),
            Issue("SU26239RMFS2", "ОФЗ 26239", "Фикс с известным купоном")
        };

        var day = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            Issues = issues,
            Trades =
            [
                Trade(issues[0].SecId, startDate, 1_000_000, 1, 13.00, 800),
                Trade(issues[1].SecId, startDate, 1_000_000, 1, 13.00, 800),
                Trade(issues[0].SecId, tradeDate, 10_000_000, 10, 13.20, 800),
                new OfzDailyTrade
                {
                    SecId = issues[1].SecId,
                    TradeDate = tradeDate,
                    Value = null,
                    NumTrades = 10,
                    YieldAtWeightedAveragePrice = 13.20,
                    Duration = 800
                }
            ],
            ActivityMetrics = [],
            LiquidityMetrics = []
        }).BreadthDays.Single(item => item.TradeDate == tradeDate);

        Assert.Equal(1, day.Concentration.IssueBaseCount);
        Assert.Single(day.Concentration.TopIssues);
        Assert.Equal(issues[0].SecId, day.Concentration.TopIssues[0].SecId);
    }

    [Fact]
    public void BuildMarketSummary_BuildsBreadthDayDrillDownContributorsAndLimitations()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var leadingIssue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");
        var unknownIssue = Issue("RUUNKNOWN", "Неизвестный выпуск", "Bond type n/a", faceUnit: null, currencyId: null);

        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            Issues = [leadingIssue, unknownIssue],
            Trades =
            [
                Trade(leadingIssue.SecId, startDate, 1_000_000, 1, 13.00, 800),
                Trade(unknownIssue.SecId, startDate, 1_000_000, 1, 13.00, 800),
                Trade(leadingIssue.SecId, tradeDate, 90_000_000, 90, 13.20, 800),
                Trade(unknownIssue.SecId, tradeDate, 10_000_000, 10, 12.80, 800)
            ],
            ActivityMetrics = [],
            LiquidityMetrics =
            [
                LiquidityMetric(
                    leadingIssue.SecId,
                    tradeDate,
                    spread: 0.08,
                    OfzSpreadSource.Provided,
                    OfzLiquidityBucket.Good,
                    OfzLiquidityMetricStatus.SnapshotOnly,
                    value: 90_000_000,
                    numTrades: 90,
                    isSnapshot: true,
                    isProvisional: true)
            ]
        });

        var day = summary.BreadthDays.Single(item => item.TradeDate == tradeDate);
        var leadingContributor = day.TopContributors.Single(item => item.SecId == leadingIssue.SecId);
        var unknownContributor = day.TopContributors.Single(item => item.SecId == unknownIssue.SecId);

        Assert.Equal(2, day.TopContributors.Count);
        Assert.Equal(0.90, leadingContributor.ValueShare!.Value, 3);
        Assert.Equal(OfzYieldDirection.Up, leadingContributor.YieldDirection);
        Assert.Equal(MarketBreadthContributorReason.YieldUp, leadingContributor.Reason);
        Assert.Equal(OfzYieldDirection.Down, unknownContributor.YieldDirection);
        Assert.Equal(OfzCouponType.Unknown, unknownContributor.CouponType);
        Assert.Contains(day.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.UnknownCouponType);
        Assert.Contains(day.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.SnapshotOnly);
        Assert.Contains(day.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.Provisional);
    }

    [Fact]
    public void BuildMarketSummary_BuildsTypeSharesAndHonorsCouponFilter()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var fixedIssue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");
        var floatingIssue = Issue("SU29019RMFS0", "ОФЗ 29019", "Флоатер");
        var currencyIssue = Issue("RU000A0ZZZ99", "ОФЗ USD", "Валютная", faceUnit: "USD", currencyId: "USD");
        var trades = new[]
        {
            Trade(fixedIssue.SecId, startDate, 1_000_000, 1, 13.00, 800),
            Trade(floatingIssue.SecId, startDate, 1_000_000, 1, 13.00, 800),
            Trade(currencyIssue.SecId, startDate, 1_000_000, 1, 13.00, 800),
            Trade(fixedIssue.SecId, tradeDate, 60_000_000, 60, 13.10, 800),
            Trade(floatingIssue.SecId, tradeDate, 30_000_000, 30, 13.20, 800),
            Trade(currencyIssue.SecId, tradeDate, 10_000_000, 10, 13.30, 800)
        };

        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            Issues = [fixedIssue, floatingIssue, currencyIssue],
            Trades = trades,
            ActivityMetrics = [],
            LiquidityMetrics = []
        });
        var day = summary.BreadthDays.Single(item => item.TradeDate == tradeDate);

        Assert.Equal(0.60, day.TypeShares.Single(share => share.CouponType == OfzCouponType.Fixed).ValueShare!.Value, 2);
        Assert.Equal(0.30, day.TypeShares.Single(share => share.CouponType == OfzCouponType.Floating).ValueShare!.Value, 2);
        Assert.Contains(day.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.CurrencyMixed);

        var filtered = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            CouponTypeFilter = OfzCouponType.Floating,
            Issues = [fixedIssue, floatingIssue, currencyIssue],
            Trades = trades,
            ActivityMetrics = [],
            LiquidityMetrics = []
        });

        var filteredDay = filtered.BreadthDays.Single(item => item.TradeDate == tradeDate);
        Assert.Single(filteredDay.TypeShares);
        Assert.Equal(OfzCouponType.Floating, filteredDay.TypeShares[0].CouponType);
        Assert.Equal(1.0, filteredDay.TypeShares[0].ValueShare!.Value, 3);
    }

    [Fact]
    public void BuildMarketSummary_BuildsFloatingSpecialMetricsForTypeFilter()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var fixedIssue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");
        var floatingIssue = Issue("SU29019RMFS0", "ОФЗ 29019", "Флоатер");

        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            CouponTypeFilter = OfzCouponType.Floating,
            Issues = [fixedIssue, floatingIssue],
            Trades =
            [
                Trade(fixedIssue.SecId, startDate, 20_000_000, 20, 12.00, 800),
                Trade(fixedIssue.SecId, tradeDate, 22_000_000, 22, 12.10, 800),
                Trade(floatingIssue.SecId, startDate, 10_000_000, 10, 13.00, 650, impliedFloatingRate: 12.80, impliedCbrRate: 7.50),
                Trade(floatingIssue.SecId, tradeDate, 12_000_000, 12, 13.20, 640, impliedFloatingRate: 13.40, impliedCbrRate: 7.75)
            ],
            ActivityMetrics = [],
            LiquidityMetrics = []
        });

        Assert.Equal(OfzCouponType.Floating, summary.CouponTypeFilter);
        var day = summary.BreadthDays.Single(item => item.TradeDate == tradeDate);
        Assert.Single(day.TypeShares);
        Assert.Equal(OfzCouponType.Floating, day.TypeShares[0].CouponType);

        Assert.Equal(3, summary.SpecialMetrics.Count);
        Assert.Equal(13.40, summary.SpecialMetrics.Single(metric => metric.Kind == OfzSpecialMetricKind.ImpliedFloatingRate).Value);
        Assert.Equal(7.75, summary.SpecialMetrics.Single(metric => metric.Kind == OfzSpecialMetricKind.ImpliedCbrRate).Value);
        Assert.Equal(5.65, summary.SpecialMetrics.Single(metric => metric.Kind == OfzSpecialMetricKind.ImpliedFloatingRateSpread).Value);
        Assert.All(summary.SpecialMetrics, metric => Assert.Equal(OfzSpecialMetricAvailability.Historical, metric.Availability));

        var finding = Assert.Single(summary.Findings, item => item.Kind == OfzSummaryFindingKind.SpecialMetric);
        Assert.Equal(OfzSummaryScope.SpecialMetric, finding.Scope);
        Assert.Contains("ОФЗ-ПК", finding.Text, StringComparison.Ordinal);
        Assert.Equal(OfzCouponType.Floating, finding.Evidence.CouponType);
        Assert.Equal(13.40, finding.Evidence.ImpliedFloatingRate);
        Assert.Equal(7.75, finding.Evidence.ImpliedCbrRate);
        Assert.Equal(5.65, finding.Evidence.ImpliedFloatingRateSpread);
        Assert.Equal(OfzSpecialMetricAvailability.Historical, finding.Evidence.SpecialMetricAvailability);
    }

    [Fact]
    public void BuildMarketSummary_UsesCbrKeyRateFallbackForFloatingSpecialMetrics()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var floatingIssue = Issue("SU29019RMFS0", "ОФЗ 29019", "Флоатер");

        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            CouponTypeFilter = OfzCouponType.Floating,
            Issues = [floatingIssue],
            Trades =
            [
                Trade(floatingIssue.SecId, startDate, 10_000_000, 10, 13.00, 650, impliedFloatingRate: 12.80),
                Trade(floatingIssue.SecId, tradeDate, 12_000_000, 12, 13.20, 640, impliedFloatingRate: 13.40)
            ],
            ActivityMetrics = [],
            LiquidityMetrics = [],
            CbrKeyRates =
            [
                new CbrKeyRate { Date = startDate, Rate = 7.50 },
                new CbrKeyRate { Date = tradeDate, Rate = 7.75 }
            ]
        });

        var cbrMetric = summary.SpecialMetrics.Single(metric => metric.Kind == OfzSpecialMetricKind.ImpliedCbrRate);
        var spreadMetric = summary.SpecialMetrics.Single(metric => metric.Kind == OfzSpecialMetricKind.ImpliedFloatingRateSpread);

        Assert.Equal(7.75, cbrMetric.Value);
        Assert.Equal(OfzSpecialMetricSource.CbrKeyRate, cbrMetric.Source);
        Assert.Equal(5.65, spreadMetric.Value);
        Assert.Contains(summary.Limitations, limitation =>
            limitation.Kind == OfzDataLimitationKind.CbrKeyRateFallback);

        var finding = Assert.Single(summary.Findings, item => item.Kind == OfzSummaryFindingKind.SpecialMetric);
        Assert.Equal(7.75, finding.Evidence.ImpliedCbrRate);
        Assert.Equal(5.65, finding.Evidence.ImpliedFloatingRateSpread);
    }

    [Fact]
    public void BuildMarketSummary_BuildsInflationSpecialMetricsForTypeFilter()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var linkerIssue = Issue("SU52002RMFS1", "ОФЗ 52002", "Индексируемый номинал");

        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            CouponTypeFilter = OfzCouponType.InflationLinked,
            Issues = [linkerIssue],
            Trades =
            [
                Trade(linkerIssue.SecId, startDate, 10_000_000, 10, 8.80, 1_500, impliedInflation: 5.10),
                Trade(linkerIssue.SecId, tradeDate, 11_000_000, 11, 8.90, 1_490, impliedInflation: 5.25)
            ],
            ActivityMetrics = [],
            LiquidityMetrics = []
        });

        var metric = Assert.Single(summary.SpecialMetrics);
        Assert.Equal(OfzSpecialMetricKind.ImpliedInflation, metric.Kind);
        Assert.Equal(5.25, metric.Value);
        Assert.Equal(OfzSpecialMetricAvailability.Historical, metric.Availability);

        var finding = Assert.Single(summary.Findings, item => item.Kind == OfzSummaryFindingKind.SpecialMetric);
        Assert.Contains("ОФЗ-ИН", finding.Text, StringComparison.Ordinal);
        Assert.Equal(OfzCouponType.InflationLinked, finding.Evidence.CouponType);
        Assert.Equal(5.25, finding.Evidence.ImpliedInflation);
    }

    [Fact]
    public void BuildMarketSummary_AddsSpecialLimitationsWithoutZeroSubstitution()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var floatingIssue = Issue("SU29019RMFS0", "ОФЗ 29019", "Флоатер");

        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            CouponTypeFilter = OfzCouponType.Floating,
            Issues = [floatingIssue],
            Trades =
            [
                Trade(floatingIssue.SecId, startDate, 10_000_000, 10, 13.00, 650),
                Trade(floatingIssue.SecId, tradeDate, 12_000_000, 12, 13.20, 640)
            ],
            ActivityMetrics = [],
            LiquidityMetrics = []
        });

        Assert.Equal(3, summary.SpecialMetrics.Count);
        Assert.All(summary.SpecialMetrics, metric =>
        {
            Assert.Null(metric.Value);
            Assert.Equal(OfzSpecialMetricAvailability.Missing, metric.Availability);
        });
        Assert.Contains(summary.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.MissingSpecialMetric);
        Assert.DoesNotContain(summary.Findings, finding => finding.Kind == OfzSummaryFindingKind.SpecialMetric);

        var json = JsonSerializer.Serialize(summary, JsonOptions);
        using var document = JsonDocument.Parse(json);
        Assert.All(document.RootElement.GetProperty("specialMetrics").EnumerateArray(), metric =>
            Assert.Equal(JsonValueKind.Null, metric.GetProperty("value").ValueKind));
    }

    [Fact]
    public void BuildMarketSummary_SpecialFindingsAvoidRecommendationLanguage()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var floatingIssue = Issue("SU29019RMFS0", "ОФЗ 29019", "Флоатер");

        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            CouponTypeFilter = OfzCouponType.Floating,
            Issues = [floatingIssue],
            Trades =
            [
                Trade(floatingIssue.SecId, startDate, 10_000_000, 10, 13.00, 650, impliedFloatingRate: 12.80, impliedCbrRate: 7.50),
                Trade(floatingIssue.SecId, tradeDate, 12_000_000, 12, 13.20, 640, impliedFloatingRate: 13.40, impliedCbrRate: 7.75)
            ],
            ActivityMetrics = [],
            LiquidityMetrics = []
        });

        Assert.All(summary.Findings.Where(finding => finding.Kind == OfzSummaryFindingKind.SpecialMetric), finding =>
        {
            Assert.False(ContainsRecommendationLanguage(finding.Title), finding.Title);
            Assert.False(ContainsRecommendationLanguage(finding.Text), finding.Text);
        });
    }

    [Fact]
    public void BuildMarketSummary_KeepsUnknownCouponTypeVisible()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var issue = Issue("RUUNKNOWN", "Неизвестный выпуск", "Bond type n/a", faceUnit: null, currencyId: null);

        var day = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            Issues = [issue],
            Trades =
            [
                Trade(issue.SecId, startDate, 1_000_000, 1, 13.00, 800),
                Trade(issue.SecId, tradeDate, 2_000_000, 2, 13.10, 800)
            ],
            ActivityMetrics = [],
            LiquidityMetrics = []
        }).BreadthDays.Single(item => item.TradeDate == tradeDate);

        var share = Assert.Single(day.TypeShares);
        Assert.Equal(OfzCouponType.Unknown, share.CouponType);
        Assert.Equal(1, share.MissingTypeCount);
        Assert.Contains(day.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.UnknownCouponType);
    }

    [Fact]
    public void BuildMarketSummary_MarksBreadthDaySnapshotAndProvisional()
    {
        var startDate = new DateTime(2026, 04, 01);
        var tradeDate = startDate.AddDays(1);
        var issue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");

        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = startDate,
            EndDate = tradeDate,
            Issues = [issue],
            Trades =
            [
                Trade(issue.SecId, startDate, 1_000_000, 1, 13.00, 800),
                Trade(issue.SecId, tradeDate, 2_000_000, 2, 13.20, 800)
            ],
            ActivityMetrics = [],
            LiquidityMetrics =
            [
                LiquidityMetric(
                    issue.SecId,
                    tradeDate,
                    spread: 0.2,
                    OfzSpreadSource.Provided,
                    OfzLiquidityBucket.Normal,
                    OfzLiquidityMetricStatus.SnapshotOnly,
                    value: 2_000_000,
                    numTrades: 2,
                    isSnapshot: true,
                    isProvisional: true)
            ]
        }, new OfzMarketSummaryOptions
        {
            Breadth = new OfzMarketBreadthOptions { MinimumComparableIssuesForBroadMove = 1 }
        });

        var day = summary.BreadthDays.Single(item => item.TradeDate == tradeDate);
        Assert.True(day.IsProvisional);
        Assert.Contains(day.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.SnapshotOnly);
        Assert.Contains(day.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.Provisional);

        var finding = Assert.Single(summary.Findings, finding => finding.Kind == OfzSummaryFindingKind.MarketBreadth);
        Assert.Equal(OfzSummaryDrillDownTarget.MarketBreadthDay, finding.DrillDown?.Target);
        Assert.True(finding.Evidence.IsProvisional);
    }

    [Fact]
    public void MarketSummary_StructuredOutputMatchesDisplayedFindings()
    {
        var summary = BuildMarketSummary(SeedInput());

        var displayed = summary.Findings
            .Select(finding => new
            {
                finding.Id,
                finding.Kind,
                finding.Priority,
                finding.Title,
                finding.Text
            })
            .ToArray();

        var structured = JsonSerializer
            .Deserialize<OfzMarketSummary>(JsonSerializer.Serialize(summary, JsonOptions), JsonOptions)!
            .Findings
            .Select(finding => new
            {
                finding.Id,
                finding.Kind,
                finding.Priority,
                finding.Title,
                finding.Text
            })
            .ToArray();

        Assert.Equal(displayed, structured);
    }

    [Fact]
    public void BuildMarketSummary_CompletesLightweightPerfSmokeForTradingYearAndHundredIssues()
    {
        var startDate = new DateTime(2026, 01, 01);
        var dates = Enumerable.Range(0, 365).Select(offset => startDate.AddDays(offset)).ToArray();
        var issues = Enumerable.Range(0, 100)
            .Select(index => Issue($"SU262{index:00}RMFS{index % 10}", $"ОФЗ 262{index:00}", "Фикс с известным купоном"))
            .ToArray();
        var activityMetrics = issues
            .SelectMany((issue, issueIndex) => dates.Select((date, dateIndex) =>
                ActivityMetric(
                    issue.SecId,
                    date,
                    1 + (issueIndex % 7) + (dateIndex % 3) * 0.1,
                    50_000_000 + issueIndex * 1_000_000 + dateIndex * 100_000,
                    10 + issueIndex,
                    yieldMove: issueIndex % 5 == 0 ? 0.12 : null)))
            .ToArray();
        var liquidityMetrics = issues
            .SelectMany((issue, issueIndex) => dates.Select(date =>
                LiquidityMetric(
                    issue.SecId,
                    date,
                    spread: issueIndex % 10 == 0 ? 0.65 : 0.08,
                    OfzSpreadSource.Provided,
                    issueIndex % 10 == 0 ? OfzLiquidityBucket.Problem : OfzLiquidityBucket.Good,
                    OfzLiquidityMetricStatus.Ready,
                    value: 50_000_000 + issueIndex * 1_000_000,
                    numTrades: 10 + issueIndex)))
            .ToArray();

        var stopwatch = Stopwatch.StartNew();
        var summary = BuildMarketSummary(
            new OfzMarketSummaryInput
            {
                StartDate = dates.First(),
                EndDate = dates.Last(),
                Issues = issues,
                Trades = [],
                ActivityMetrics = activityMetrics,
                LiquidityMetrics = liquidityMetrics
            },
            new OfzMarketSummaryOptions
            {
                MaxFindings = 7
            });
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), stopwatch.Elapsed.ToString());
        Assert.NotEmpty(summary.Findings);
        Assert.NotEmpty(summary.Segments);
        Assert.Equal(365, summary.BreadthDays.Count);
        Assert.Equal(100, summary.SourceCounts.Issues);
        Assert.Equal(36_500, summary.SourceCounts.ActivityMetrics);
        Assert.Equal(36_500, summary.SourceCounts.LiquidityMetrics);
    }

    [Fact]
    public void BuildMarketSummary_AddsActivityWithIndexMoveFinding()
    {
        var summary = BuildMarketSummary(
            MinimalIndexInput(closeOnActiveDate: 100.5, previousClose: 100),
            new OfzMarketSummaryOptions { MaxFindings = 10 });
        var finding = Assert.Single(summary.Findings, item => item.Kind == OfzSummaryFindingKind.ActivityWithIndexMove);

        Assert.Equal("RGBI", finding.Evidence.IndexSecId);
        Assert.True(finding.Evidence.IndexMoveIsMeaningful);
        Assert.Equal(OfzSummaryDrillDownTarget.IndexContextDay, finding.DrillDown?.Target);
        Assert.Equal(EndDate, finding.DrillDown?.TradeDate);
    }

    [Fact]
    public void BuildMarketSummary_AddsActivityWithoutIndexMoveFindingForLocalActivity()
    {
        var summary = BuildMarketSummary(
            MinimalIndexInput(closeOnActiveDate: 100.05, previousClose: 100),
            new OfzMarketSummaryOptions { MaxFindings = 10 });
        var finding = Assert.Single(summary.Findings, item => item.Kind == OfzSummaryFindingKind.ActivityWithoutIndexMove);

        Assert.Equal("RGBI", finding.Evidence.IndexSecId);
        Assert.False(finding.Evidence.IndexMoveIsMeaningful);
        Assert.InRange(finding.Evidence.IndexDailyChangePercent!.Value, 0.0004, 0.0006);
    }

    [Fact]
    public void BuildMarketSummary_IndexBackedFindingsAvoidRecommendationLanguage()
    {
        var summary = BuildMarketSummary(
            MinimalIndexInput(closeOnActiveDate: 100.5, previousClose: 100),
            new OfzMarketSummaryOptions { MaxFindings = 10 });
        var indexFindings = summary.Findings
            .Where(finding => finding.Scope == OfzSummaryScope.IndexContext)
            .ToArray();

        Assert.NotEmpty(indexFindings);
        Assert.All(indexFindings, finding =>
        {
            Assert.False(ContainsRecommendationLanguage(finding.Title), finding.Title);
            Assert.False(ContainsRecommendationLanguage(finding.Text), finding.Text);
        });
    }

    [Fact]
    public void BuildMarketSummary_FiltersIndexFindingsByInsightPeriod()
    {
        var seed = MinimalIndexInput(closeOnActiveDate: 100.5, previousClose: 100);
        var input = new OfzMarketSummaryInput
        {
            StartDate = seed.StartDate,
            EndDate = seed.EndDate,
            InsightStartDate = EndDate,
            InsightEndDate = EndDate,
            Issues = seed.Issues,
            Trades = seed.Trades,
            ActivityMetrics = seed.ActivityMetrics,
            LiquidityMetrics = seed.LiquidityMetrics,
            IndexPoints = seed.IndexPoints
        };

        var summary = BuildMarketSummary(input, new OfzMarketSummaryOptions { MaxFindings = 10 });

        Assert.All(summary.IndexContextDays, day => Assert.Equal(EndDate, day.TradeDate));
        Assert.All(
            summary.Findings.Where(finding => finding.Scope == OfzSummaryScope.IndexContext),
            finding => Assert.Equal(EndDate, finding.DrillDown?.TradeDate));
    }

    [Fact]
    public void BuildMarketSummary_SerializesIndexContextContractFields()
    {
        var summary = BuildMarketSummary(
            MinimalIndexInput(closeOnActiveDate: 100.5, previousClose: 100),
            new OfzMarketSummaryOptions { MaxFindings = 10 });

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(summary, JsonOptions));
        var root = document.RootElement;

        Assert.Equal("1.6", root.GetProperty("schemaVersion").GetString());
        Assert.True(root.TryGetProperty("indexContextDays", out var days));
        Assert.True(days.GetArrayLength() > 0);
        Assert.True(root.TryGetProperty("indexSegments", out _));
        AssertJsonDateOnly(days[0].GetProperty("tradeDate"));
        Assert.False(days[0].TryGetProperty("wholeMarketPoints", out _));
        Assert.False(days[0].TryGetProperty("segmentPoints", out _));
        Assert.False(days[0].TryGetProperty("priceIndexPoint", out _));
        Assert.False(days[0].TryGetProperty("totalReturnIndexPoint", out _));

        var point = days[0].GetProperty("points").EnumerateArray().First();
        AssertJsonDateOnly(point.GetProperty("tradeDate"));
        Assert.False(point.TryGetProperty("sourceLabel", out _));
    }

    [Fact]
    public void BuildMarketSummary_CouponTypeFilterDoesNotRecalculateMarketIndexContext()
    {
        var seed = SeedInput();
        var summary = BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = StartDate,
            EndDate = EndDate,
            CouponTypeFilter = OfzCouponType.Fixed,
            Issues = seed.Issues,
            Trades = seed.Trades,
            ActivityMetrics = seed.ActivityMetrics,
            LiquidityMetrics = seed.LiquidityMetrics,
            IndexPoints = WholeMarketIndexPoints(closeOnActiveDate: 100.5, previousClose: 100)
        });

        var day = Assert.Single(summary.IndexContextDays, item => item.TradeDate == EndDate);
        Assert.NotNull(day.PriceIndexPoint);
        Assert.Equal("RGBI", day.PriceIndexPoint.SecId);
        Assert.Equal(0.005, day.PriceIndexPoint.DailyChangePercent);
        Assert.Equal(4, summary.SourceCounts.IndexPoints);
    }

    [Fact]
    public void BuildMarketSummary_AddsExternalFactorsContextAndFinding()
    {
        var seed = MinimalIndexInput(closeOnActiveDate: 100.5, previousClose: 100);
        var input = new OfzMarketSummaryInput
        {
            StartDate = seed.StartDate,
            EndDate = seed.EndDate,
            Issues = seed.Issues,
            Trades = seed.Trades,
            ActivityMetrics = seed.ActivityMetrics,
            LiquidityMetrics = seed.LiquidityMetrics,
            IndexPoints = seed.IndexPoints,
            CbrKeyRates =
            [
                new CbrKeyRate { Date = EndDate.AddDays(-2), Rate = 14.50 },
                new CbrKeyRate { Date = EndDate.AddDays(1), Rate = 15.00 }
            ]
        };

        var summary = BuildMarketSummary(input, new OfzMarketSummaryOptions { MaxFindings = 10 });
        var context = Assert.IsType<OfzExternalFactorsContext>(summary.ExternalFactorsContext);
        var finding = Assert.Single(summary.Findings, item =>
            item.Kind == OfzSummaryFindingKind.ExternalFactorActivity &&
            item.Evidence.ExternalFactorCode == "RGBI");

        Assert.Contains(context.FactorSeries, series => series.Code == "cbr_key_rate");
        Assert.Contains(context.FactorSeries, series => series.Code == "RGBI");
        Assert.True(summary.SourceCounts.ExternalFactorObservations >= 2);
        Assert.True(summary.SourceCounts.ExternalFactorSeries >= 2);
        Assert.True(summary.SourceCounts.ExternalFactorLinks >= 1);
        Assert.Equal(OfzSummaryScope.ExternalFactors, finding.Scope);
        Assert.Equal(OfzSummaryDrillDownTarget.ExternalFactors, finding.DrillDown?.Target);
        Assert.Equal("RGBI", finding.Evidence.ExternalFactorCode);
        Assert.Equal(OfzExternalFactorKind.MarketIndex, finding.Evidence.ExternalFactorKind);
        Assert.Equal(OfzExternalFactorSource.MoexIndex, finding.Evidence.ExternalFactorSource);
        Assert.Equal(OfzExternalFactorLinkKind.YieldMoveWithFactorMove, finding.Evidence.ExternalFactorLinkKind);
        Assert.True(finding.Evidence.ExternalFactorMoveIsMeaningful);
    }

    [Fact]
    public void BuildMarketSummary_KeepsExternalFactorFindingWhenDefaultLimitWouldTrimIt()
    {
        var seed = SeedInput();
        var input = new OfzMarketSummaryInput
        {
            StartDate = seed.StartDate,
            EndDate = seed.EndDate,
            Issues = seed.Issues,
            Trades = seed.Trades,
            ActivityMetrics = seed.ActivityMetrics,
            LiquidityMetrics = seed.LiquidityMetrics,
            IndexPoints = WholeMarketIndexPoints(closeOnActiveDate: 100.05, previousClose: 100),
            CbrKeyRates = [new CbrKeyRate { Date = EndDate.AddDays(-2), Rate = 14.50 }]
        };

        var summary = BuildMarketSummary(input);
        var factorFinding = Assert.Single(summary.Findings, item => item.Kind == OfzSummaryFindingKind.ExternalFactorActivity);

        Assert.Equal(7, summary.Findings.Count);
        Assert.Equal(
            summary.Findings.OrderByDescending(finding => finding.Priority).ThenBy(finding => finding.Id).Select(finding => finding.Id),
            summary.Findings.Select(finding => finding.Id));
        Assert.Equal(OfzExternalFactorLinkKind.ActivityWithoutFactorMove, factorFinding.Evidence.ExternalFactorLinkKind);
        Assert.False(factorFinding.Evidence.ExternalFactorMoveIsMeaningful);
    }

    [Fact]
    public void BuildMarketSummary_AddsExternalActivityWithoutFactorMoveFinding()
    {
        var seed = MinimalIndexInput(closeOnActiveDate: 100.05, previousClose: 100);
        var input = new OfzMarketSummaryInput
        {
            StartDate = seed.StartDate,
            EndDate = seed.EndDate,
            Issues = seed.Issues,
            Trades = seed.Trades,
            ActivityMetrics = seed.ActivityMetrics,
            LiquidityMetrics = seed.LiquidityMetrics,
            IndexPoints = seed.IndexPoints,
            CbrKeyRates = [new CbrKeyRate { Date = EndDate.AddDays(-2), Rate = 14.50 }]
        };

        var summary = BuildMarketSummary(input, new OfzMarketSummaryOptions { MaxFindings = 10 });
        var finding = Assert.Single(summary.Findings, item =>
            item.Kind == OfzSummaryFindingKind.ExternalFactorActivity &&
            item.Evidence.ExternalFactorCode == "RGBI");

        Assert.Equal(OfzExternalFactorLinkKind.ActivityWithoutFactorMove, finding.Evidence.ExternalFactorLinkKind);
        Assert.False(finding.Evidence.ExternalFactorMoveIsMeaningful);
        Assert.InRange(finding.Evidence.ExternalFactorDailyChangePercent!.Value, 0.0004, 0.0006);
    }

    [Fact]
    public void BuildMarketSummary_ExternalFactorsHonorLastAvailableDaySignalScope()
    {
        var issue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");
        var firstDate = EndDate.AddDays(-1);
        var previousDate = EndDate.AddDays(-2);
        var input = new OfzMarketSummaryInput
        {
            StartDate = previousDate,
            EndDate = EndDate,
            SignalScope = OfzSummarySignalScope.LastAvailableDay,
            Issues = [issue],
            Trades =
            [
                Trade(issue.SecId, previousDate, 1_000_000, 1, 12.0, 800),
                Trade(issue.SecId, firstDate, 900_000_000, 600, 12.1, 810),
                Trade(issue.SecId, EndDate, 2_000_000_000, 1_100, 12.4, 800)
            ],
            ActivityMetrics =
            [
                ActivityMetric(issue.SecId, firstDate, 4, 900_000_000, 600, yieldMove: -0.11),
                ActivityMetric(issue.SecId, EndDate, 8, 2_000_000_000, 1_100, yieldMove: -0.35)
            ],
            LiquidityMetrics =
            [
                LiquidityMetric(
                    issue.SecId,
                    EndDate,
                    spread: 0.12,
                    OfzSpreadSource.Provided,
                    OfzLiquidityBucket.Good,
                    OfzLiquidityMetricStatus.Ready,
                    value: 2_000_000_000,
                    numTrades: 1_100)
            ],
            IndexPoints =
            [
                IndexPoint("RGBI", previousDate, 100, yield: 14.1),
                IndexPoint("RGBI", firstDate, 100.5, yield: 14.0),
                IndexPoint("RGBI", EndDate, 101, yield: 13.9),
                IndexPoint("RGBITR", previousDate, 200),
                IndexPoint("RGBITR", firstDate, 200.6),
                IndexPoint("RGBITR", EndDate, 201.1)
            ],
            CbrKeyRates = [new CbrKeyRate { Date = previousDate, Rate = 14.50 }]
        };

        var summary = BuildMarketSummary(input, new OfzMarketSummaryOptions { MaxFindings = 10 });
        var context = Assert.IsType<OfzExternalFactorsContext>(summary.ExternalFactorsContext);

        Assert.NotEmpty(context.Links);
        Assert.All(context.Links, link => Assert.Equal(EndDate, link.TradeDate));
        Assert.All(
            summary.Findings.Where(finding => finding.Scope == OfzSummaryScope.ExternalFactors),
            finding => Assert.Equal(EndDate, finding.DrillDown?.TradeDate));
    }

    [Fact]
    public void BuildMarketSummary_ExternalFactorFindingsAvoidRecommendationLanguage()
    {
        var seed = MinimalIndexInput(closeOnActiveDate: 100.5, previousClose: 100);
        var input = new OfzMarketSummaryInput
        {
            StartDate = seed.StartDate,
            EndDate = seed.EndDate,
            Issues = seed.Issues,
            Trades = seed.Trades,
            ActivityMetrics = seed.ActivityMetrics,
            LiquidityMetrics = seed.LiquidityMetrics,
            IndexPoints = seed.IndexPoints,
            CbrKeyRates = [new CbrKeyRate { Date = EndDate.AddDays(-2), Rate = 14.50 }]
        };

        var summary = BuildMarketSummary(input, new OfzMarketSummaryOptions { MaxFindings = 10 });
        var factorFindings = summary.Findings
            .Where(finding => finding.Scope == OfzSummaryScope.ExternalFactors)
            .ToArray();

        Assert.NotEmpty(factorFindings);
        Assert.All(factorFindings, finding =>
        {
            Assert.False(ContainsRecommendationLanguage(finding.Title), finding.Title);
            Assert.False(ContainsRecommendationLanguage(finding.Text), finding.Text);
        });
    }

    [Fact]
    public void BuildMarketSummary_SerializesExternalFactorsContractFields()
    {
        var seed = MinimalIndexInput(closeOnActiveDate: 100.5, previousClose: 100);
        var input = new OfzMarketSummaryInput
        {
            StartDate = seed.StartDate,
            EndDate = seed.EndDate,
            Issues = seed.Issues,
            Trades = seed.Trades,
            ActivityMetrics = seed.ActivityMetrics,
            LiquidityMetrics = seed.LiquidityMetrics,
            IndexPoints = seed.IndexPoints,
            CbrKeyRates = [new CbrKeyRate { Date = EndDate.AddDays(-2), Rate = 14.50 }]
        };

        var summary = BuildMarketSummary(input, new OfzMarketSummaryOptions { MaxFindings = 10 });
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(summary, JsonOptions));
        var root = document.RootElement;

        Assert.Equal("1.6", root.GetProperty("schemaVersion").GetString());
        Assert.True(root.TryGetProperty("externalFactorsContext", out var context));
        Assert.True(context.GetProperty("factorSeries").GetArrayLength() > 0);
        Assert.True(context.GetProperty("links").GetArrayLength() > 0);
        var series = context.GetProperty("factorSeries").EnumerateArray().First(item => item.GetProperty("code").GetString() == "cbr_key_rate");
        Assert.Equal("PolicyRate", series.GetProperty("kind").GetString());
        AssertJsonDateOnly(series.GetProperty("latestObservation").GetProperty("tradeDate"));

        var evidence = root
            .GetProperty("findings")
            .EnumerateArray()
            .First(item => item.GetProperty("kind").GetString() == "ExternalFactorActivity")
            .GetProperty("evidence");
        Assert.Equal("RGBI", evidence.GetProperty("externalFactorCode").GetString());
        Assert.Equal("MoexIndex", evidence.GetProperty("externalFactorSource").GetString());
        AssertJsonDateOnly(evidence.GetProperty("externalFactorObservationDate"));

        var sourceCounts = root.GetProperty("sourceCounts");
        Assert.True(sourceCounts.GetProperty("externalFactorObservations").GetInt32() > 0);
        Assert.True(sourceCounts.GetProperty("externalFactorSeries").GetInt32() > 0);
        Assert.True(sourceCounts.GetProperty("externalFactorLinks").GetInt32() > 0);
    }

    [Fact]
    public void BuildMarketSummary_AddsCashflowContextAndActivityNearEventFinding()
    {
        var seed = SeedInput();
        var input = new OfzMarketSummaryInput
        {
            StartDate = StartDate,
            EndDate = EndDate,
            Issues = seed.Issues,
            Trades = seed.Trades,
            ActivityMetrics = seed.ActivityMetrics,
            LiquidityMetrics = seed.LiquidityMetrics,
            CashflowDataLoaded = true,
            CashflowEvents =
            [
                CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Coupon, EndDate.AddDays(1), value: null, valueRub: null, valuePercent: 7.1),
                CashflowEvent("SU26239RMFS2", OfzCashflowEventType.Maturity, EndDate.AddDays(20), value: 1_000, valueRub: 1_000, valuePercent: 100)
            ]
        };

        var summary = BuildMarketSummary(input, new OfzMarketSummaryOptions { MaxFindings = 10 });
        var finding = Assert.Single(summary.Findings, item => item.Kind == OfzSummaryFindingKind.ActivityNearCashflowEvent);

        Assert.NotNull(summary.CashflowContext);
        Assert.Equal("SU26238RMFS4", finding.Evidence.SecId);
        Assert.Equal(OfzCashflowEventType.Coupon, finding.Evidence.CashflowEventType);
        Assert.Equal(EndDate.AddDays(1), finding.Evidence.CashflowEventDate);
        Assert.Equal(1, finding.Evidence.CashflowDaysToEvent);
        Assert.Null(finding.Evidence.CashflowValue);
        Assert.Equal(7.1, finding.Evidence.CashflowValuePercent);
        Assert.Equal(OfzSummaryDrillDownTarget.CashflowEvent, finding.DrillDown?.Target);
        Assert.Equal(2, summary.SourceCounts.CashflowEvents);
        Assert.Equal(2, summary.SourceCounts.CashflowIssues);
        Assert.False(ContainsRecommendationLanguage(finding.Title), finding.Title);
        Assert.False(ContainsRecommendationLanguage(finding.Text), finding.Text);
    }

    [Fact]
    public void BuildMarketSummary_SerializesCashflowContractFields()
    {
        var seed = SeedInput();
        var summary = BuildMarketSummary(
            new OfzMarketSummaryInput
            {
                StartDate = StartDate,
                EndDate = EndDate,
                Issues = seed.Issues,
                Trades = seed.Trades,
                ActivityMetrics = seed.ActivityMetrics,
                LiquidityMetrics = seed.LiquidityMetrics,
                CashflowDataLoaded = true,
                CashflowEvents =
                [
                    CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Offer, EndDate, price: 100.2)
                ]
            },
            new OfzMarketSummaryOptions { MaxFindings = 10 });

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(summary, JsonOptions));
        var root = document.RootElement;

        Assert.Equal("1.6", root.GetProperty("schemaVersion").GetString());
        Assert.True(root.TryGetProperty("cashflowContext", out var context));
        AssertJsonDateOnly(context.GetProperty("startDate"));
        AssertJsonDateOnly(context.GetProperty("endDate"));
        Assert.True(context.GetProperty("events").GetArrayLength() > 0);

        var cashflowFinding = root.GetProperty("findings")
            .EnumerateArray()
            .First(item => item.GetProperty("kind").GetString() == "ActivityNearCashflowEvent");
        var evidence = cashflowFinding.GetProperty("evidence");
        Assert.Equal("Offer", evidence.GetProperty("cashflowEventType").GetString());
        AssertJsonDateOnly(evidence.GetProperty("cashflowEventDate"));
        Assert.True(evidence.TryGetProperty("cashflowSourceKind", out _));
    }

    [Fact]
    public void BuildMarketSummary_AddsSeasonalityContextAndFinding()
    {
        var firstMonday = new DateTime(2026, 04, 06);
        var issue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");
        var summary = BuildMarketSummary(
            new OfzMarketSummaryInput
            {
                StartDate = firstMonday,
                EndDate = firstMonday.AddDays(28),
                InsightStartDate = firstMonday.AddDays(28),
                InsightEndDate = firstMonday.AddDays(28),
                Issues = [issue],
                ActivityMetrics =
                [
                    ActivityMetric(issue.SecId, firstMonday, 1, 100_000_000, 10),
                    ActivityMetric(issue.SecId, firstMonday.AddDays(7), 1, 110_000_000, 11),
                    ActivityMetric(issue.SecId, firstMonday.AddDays(14), 1, 90_000_000, 9),
                    ActivityMetric(issue.SecId, firstMonday.AddDays(21), 1, 100_000_000, 10),
                    ActivityMetric(issue.SecId, firstMonday.AddDays(28), 3, 260_000_000, 26)
                ],
                LiquidityMetrics = []
            },
            new OfzMarketSummaryOptions { MaxFindings = 20 });

        Assert.NotNull(summary.SeasonalityContext);
        Assert.Equal(5, summary.SeasonalityContext.ObservationCount);
        Assert.Equal(5, summary.SourceCounts.SeasonalityObservations);
        var seasonalityFinding = Assert.Single(summary.SeasonalityContext.Findings);
        Assert.Equal(OfzSeasonalityFindingKind.HighSeasonalActivity, seasonalityFinding.Kind);
        Assert.Equal("Monday", seasonalityFinding.BucketKey);

        var summaryFinding = Assert.Single(summary.Findings, finding => finding.Kind == OfzSummaryFindingKind.SeasonalityActivity);
        Assert.Equal(OfzSummaryScope.Seasonality, summaryFinding.Scope);
        Assert.Equal(OfzSeasonalityFindingKind.HighSeasonalActivity, summaryFinding.Evidence.SeasonalityFindingKind);
        Assert.Equal(OfzSeasonalityBucketKind.Weekday, summaryFinding.Evidence.SeasonalityBucketKind);
        Assert.Equal("Monday", summaryFinding.Evidence.SeasonalityBucketKey);
        Assert.Equal(2.6, summaryFinding.Evidence.SeasonalityValueRatio!.Value, 1);
        Assert.Equal(OfzSummaryDrillDownTarget.HeatmapDate, summaryFinding.DrillDown?.Target);
        Assert.False(ContainsRecommendationLanguage(summaryFinding.Title), summaryFinding.Title);
        Assert.False(ContainsRecommendationLanguage(summaryFinding.Text), summaryFinding.Text);
    }

    [Fact]
    public void BuildMarketSummary_SeasonalityHonorsCouponFilterAndSignalScope()
    {
        var firstMonday = new DateTime(2026, 04, 06);
        var fixedIssue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");
        var floatingIssue = Issue("SU29019RMFS0", "ОФЗ 29019", "Флоатер");
        var summary = BuildMarketSummary(
            new OfzMarketSummaryInput
            {
                StartDate = firstMonday,
                EndDate = firstMonday.AddDays(35),
                InsightStartDate = firstMonday.AddDays(28),
                InsightEndDate = firstMonday.AddDays(35),
                CouponTypeFilter = OfzCouponType.Floating,
                SignalScope = OfzSummarySignalScope.LastAvailableDay,
                Issues = [fixedIssue, floatingIssue],
                ActivityMetrics =
                [
                    ActivityMetric(fixedIssue.SecId, firstMonday, 1, 100_000_000, 10),
                    ActivityMetric(fixedIssue.SecId, firstMonday.AddDays(7), 1, 100_000_000, 10),
                    ActivityMetric(fixedIssue.SecId, firstMonday.AddDays(14), 1, 100_000_000, 10),
                    ActivityMetric(fixedIssue.SecId, firstMonday.AddDays(21), 1, 100_000_000, 10),
                    ActivityMetric(fixedIssue.SecId, firstMonday.AddDays(28), 4, 400_000_000, 40),
                    ActivityMetric(floatingIssue.SecId, firstMonday, 1, 100_000_000, 10),
                    ActivityMetric(floatingIssue.SecId, firstMonday.AddDays(7), 1, 100_000_000, 10),
                    ActivityMetric(floatingIssue.SecId, firstMonday.AddDays(14), 1, 100_000_000, 10),
                    ActivityMetric(floatingIssue.SecId, firstMonday.AddDays(21), 1, 100_000_000, 10),
                    ActivityMetric(floatingIssue.SecId, firstMonday.AddDays(28), 1, 100_000_000, 10),
                    ActivityMetric(floatingIssue.SecId, firstMonday.AddDays(35), 3, 250_000_000, 25)
                ],
                LiquidityMetrics = []
            },
            new OfzMarketSummaryOptions { MaxFindings = 20 });

        Assert.Equal(OfzCouponType.Floating, summary.CouponTypeFilter);
        Assert.NotNull(summary.SeasonalityContext);
        Assert.Equal(1, summary.SourceCounts.ActivityMetrics);
        Assert.Equal(6, summary.SourceCounts.SeasonalityObservations);
        Assert.All(summary.SeasonalityContext.Findings, finding => Assert.Equal(firstMonday.AddDays(35), finding.TradeDate));
        Assert.DoesNotContain(summary.SeasonalityContext.Findings, finding => finding.TradeDate == firstMonday.AddDays(28));
        Assert.Contains(summary.Findings, finding =>
            finding.Kind == OfzSummaryFindingKind.SeasonalityActivity &&
            finding.Evidence.TradeDate == firstMonday.AddDays(35));
    }

    [Fact]
    public void BuildMarketSummary_SerializesSeasonalityContractFields()
    {
        var firstMonday = new DateTime(2026, 04, 06);
        var issue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");
        var summary = BuildMarketSummary(
            new OfzMarketSummaryInput
            {
                StartDate = firstMonday,
                EndDate = firstMonday.AddDays(28),
                InsightStartDate = firstMonday.AddDays(28),
                InsightEndDate = firstMonday.AddDays(28),
                Issues = [issue],
                ActivityMetrics =
                [
                    ActivityMetric(issue.SecId, firstMonday, 1, 100_000_000, 10),
                    ActivityMetric(issue.SecId, firstMonday.AddDays(7), 1, 100_000_000, 10),
                    ActivityMetric(issue.SecId, firstMonday.AddDays(14), 1, 100_000_000, 10),
                    ActivityMetric(issue.SecId, firstMonday.AddDays(21), 1, 100_000_000, 10),
                    ActivityMetric(issue.SecId, firstMonday.AddDays(28), 3, 260_000_000, 26)
                ],
                LiquidityMetrics = []
            },
            new OfzMarketSummaryOptions { MaxFindings = 20 });

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(summary, JsonOptions));
        var root = document.RootElement;

        Assert.Equal("1.6", root.GetProperty("schemaVersion").GetString());
        Assert.True(root.TryGetProperty("seasonalityContext", out var context));
        AssertJsonDateOnly(context.GetProperty("startDate"));
        AssertJsonDateOnly(context.GetProperty("endDate"));
        Assert.True(context.GetProperty("weekdayBuckets").GetArrayLength() > 0);
        Assert.True(context.GetProperty("monthBuckets").GetArrayLength() > 0);

        var bucket = context.GetProperty("weekdayBuckets").EnumerateArray().First();
        Assert.Equal("Weekday", bucket.GetProperty("kind").GetString());
        Assert.True(bucket.TryGetProperty("medianTotalValue", out _));
        Assert.Equal("Weak", bucket.GetProperty("baselineQuality").GetString());

        var seasonalityFinding = context.GetProperty("findings").EnumerateArray().Single();
        Assert.Equal("HighSeasonalActivity", seasonalityFinding.GetProperty("kind").GetString());
        AssertJsonDateOnly(seasonalityFinding.GetProperty("tradeDate"));
        Assert.Equal("Weekday", seasonalityFinding.GetProperty("bucketKind").GetString());
        Assert.True(seasonalityFinding.TryGetProperty("valueRatio", out _));

        var summaryFinding = root.GetProperty("findings")
            .EnumerateArray()
            .First(item => item.GetProperty("kind").GetString() == "SeasonalityActivity");
        var evidence = summaryFinding.GetProperty("evidence");
        Assert.Equal("HighSeasonalActivity", evidence.GetProperty("seasonalityFindingKind").GetString());
        Assert.Equal("Weekday", evidence.GetProperty("seasonalityBucketKind").GetString());
        Assert.Equal("Monday", evidence.GetProperty("seasonalityBucketKey").GetString());
        Assert.Equal(5, root.GetProperty("sourceCounts").GetProperty("seasonalityObservations").GetInt32());

        var roundTrip = JsonSerializer.Deserialize<OfzMarketSummary>(
            JsonSerializer.Serialize(summary, JsonOptions),
            JsonOptions)!;
        Assert.NotNull(roundTrip.SeasonalityContext);
        Assert.Equal(5, roundTrip.SeasonalityContext.ObservationCount);
        Assert.Single(roundTrip.SeasonalityContext.Findings);
        Assert.Equal(OfzSeasonalityFindingKind.HighSeasonalActivity, roundTrip.SeasonalityContext.Findings[0].Kind);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    private static void AssertJsonDateOnly(JsonElement element)
    {
        var value = element.GetString();
        Assert.NotNull(value);
        Assert.Equal(10, value.Length);
        Assert.Equal('-', value[4]);
        Assert.Equal('-', value[7]);
        Assert.DoesNotContain("T", value);
    }

    private static OfzMarketSummary BuildMarketSummary(
        OfzMarketSummaryInput input,
        OfzMarketSummaryOptions? options = null)
    {
        return OfzMarketSummaryBuilder.Build(
            NormalizeInput(input),
            options);
    }

    private static OfzYieldDirection DirectionFor(OfzMarketSummary summary, DateTime tradeDate, string secId)
    {
        return summary.BreadthDays
            .Single(day => day.TradeDate == tradeDate.Date)
            .TopContributors
            .Single(contributor => contributor.SecId == secId)
            .YieldDirection;
    }

    private static OfzMarketSummaryInput MinimalIndexInput(double closeOnActiveDate, double previousClose)
    {
        var issue = Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном");

        return new OfzMarketSummaryInput
        {
            StartDate = StartDate,
            EndDate = EndDate,
            Issues = [issue],
            Trades = [Trade(issue.SecId, EndDate, 2_000_000_000, 1_100, 12.4, 800)],
            ActivityMetrics = [ActivityMetric(issue.SecId, EndDate, 8, 2_000_000_000, 1_100, yieldMove: -0.35)],
            LiquidityMetrics =
            [
                LiquidityMetric(
                    issue.SecId,
                    EndDate,
                    spread: 0.12,
                    OfzSpreadSource.Provided,
                    OfzLiquidityBucket.Good,
                    OfzLiquidityMetricStatus.Ready,
                    value: 2_000_000_000,
                    numTrades: 1_100)
            ],
            IndexPoints = WholeMarketIndexPoints(closeOnActiveDate, previousClose)
        };
    }

    private static IReadOnlyList<OfzMarketIndexPoint> WholeMarketIndexPoints(double closeOnActiveDate, double previousClose)
    {
        return
        [
            IndexPoint("RGBI", EndDate.AddDays(-1), previousClose, yield: 14.1),
            IndexPoint("RGBI", EndDate, closeOnActiveDate, yield: 14.0),
            IndexPoint("RGBITR", EndDate.AddDays(-1), 200),
            IndexPoint("RGBITR", EndDate, 200.6)
        ];
    }

    private static OfzMarketIndexPoint IndexPoint(
        string secId,
        DateTime tradeDate,
        double? close,
        double? yield = null)
    {
        return new OfzMarketIndexPoint
        {
            SecId = secId,
            ShortName = secId,
            TradeDate = tradeDate.Date,
            Close = close,
            Yield = yield,
            SourceKind = OfzMarketIndexSourceKind.History,
            LoadedAt = DateTime.UtcNow
        };
    }

    private static OfzMarketSummaryInput NormalizeInput(OfzMarketSummaryInput input)
    {
        if (input.StartDate != default || input.EndDate != default)
        {
            return input;
        }

        return new OfzMarketSummaryInput
        {
            StartDate = StartDate,
            EndDate = EndDate,
            CouponTypeFilter = input.CouponTypeFilter,
            SignalScope = input.SignalScope,
            Issues = input.Issues,
            Trades = input.Trades,
            ActivityMetrics = input.ActivityMetrics,
            LiquidityMetrics = input.LiquidityMetrics,
            CbrKeyRates = input.CbrKeyRates,
            IndexPoints = input.IndexPoints,
            CashflowEvents = input.CashflowEvents,
            CashflowDataLoaded = input.CashflowDataLoaded
        };
    }

    private static OfzMarketSummaryInput SeedInput()
    {
        return new OfzMarketSummaryInput
        {
            StartDate = StartDate,
            EndDate = EndDate,
            Issues =
            [
                Issue("SU26238RMFS4", "ОФЗ 26238", "Фикс с известным купоном"),
                Issue("SU26239RMFS2", "ОФЗ 26239", "Фикс с известным купоном"),
                Issue("SU29019RMFS0", "ОФЗ 29019", "Флоатер")
            ],
            Trades =
            [
                Trade("SU26238RMFS4", EndDate.AddDays(-1), 900_000_000, 600, 12.1, 810),
                Trade("SU26238RMFS4", EndDate, 2_000_000_000, 1_100, 12.4, 800),
                Trade("SU26239RMFS2", EndDate, 1_400_000_000, 180, 12.2, 1_000),
                Trade("SU29019RMFS0", EndDate, 700_000_000, 90, 11.8, 500)
            ],
            ActivityMetrics =
            [
                ActivityMetric("SU26238RMFS4", EndDate.AddDays(-1), 4, 900_000_000, 600, yieldMove: -0.11),
                ActivityMetric("SU26238RMFS4", EndDate, 8, 2_000_000_000, 1_100, yieldMove: -0.35),
                ActivityMetric("SU26239RMFS2", EndDate, 3, 1_400_000_000, 180, yieldMove: 0.08),
                ActivityMetric("SU29019RMFS0", EndDate, 5, 700_000_000, 90, yieldMove: null)
            ],
            LiquidityMetrics =
            [
                LiquidityMetric(
                    "SU26238RMFS4",
                    EndDate,
                    spread: 0.72,
                    OfzSpreadSource.CalculatedFromBidOffer,
                    OfzLiquidityBucket.Problem,
                    OfzLiquidityMetricStatus.Ready,
                    value: 2_000_000_000,
                    numTrades: 1_100),
                LiquidityMetric(
                    "SU26239RMFS2",
                    EndDate,
                    spread: 0.12,
                    OfzSpreadSource.Provided,
                    OfzLiquidityBucket.Good,
                    OfzLiquidityMetricStatus.Ready,
                    value: 1_400_000_000,
                    numTrades: 180),
                LiquidityMetric(
                    "SU29019RMFS0",
                    EndDate,
                    spread: 0.18,
                    OfzSpreadSource.Provided,
                    OfzLiquidityBucket.Good,
                    OfzLiquidityMetricStatus.Ready,
                    value: 700_000_000,
                    numTrades: 90)
            ]
        };
    }

    private static OfzIssue Issue(
        string secId,
        string shortName,
        string bondType,
        string? faceUnit = "SUR",
        string? currencyId = "SUR")
    {
        return new OfzIssue
        {
            SecId = secId,
            ShortName = shortName,
            SecName = shortName,
            BondType = bondType,
            FaceUnit = faceUnit,
            CurrencyId = currencyId
        };
    }

    private static OfzDailyTrade Trade(
        string secId,
        DateTime tradeDate,
        double value,
        int numTrades,
        double yield,
        double duration,
        double? impliedFloatingRate = null,
        double? impliedInflation = null,
        double? impliedCbrRate = null)
    {
        return new OfzDailyTrade
        {
            SecId = secId,
            TradeDate = tradeDate,
            Value = value,
            NumTrades = numTrades,
            YieldAtWeightedAveragePrice = yield,
            Duration = duration,
            ImpliedFloatingRate = impliedFloatingRate,
            ImpliedInflation = impliedInflation,
            ImpliedCbrRate = impliedCbrRate
        };
    }

    private static OfzActivityMetric ActivityMetric(
        string secId,
        DateTime tradeDate,
        double activityScore,
        double value,
        int numTrades,
        double? yieldMove = null)
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
            PreviousYieldValue = yieldMove.HasValue ? 12.5 - yieldMove.Value : null,
            YieldMove = yieldMove,
            Duration = 900,
            Status = yieldMove.HasValue
                ? OfzActivityMetricStatus.Ready
                : OfzActivityMetricStatus.MissingYield
        };
    }

    private static OfzLiquidityMetric LiquidityMetric(
        string secId,
        DateTime tradeDate,
        double? spread,
        OfzSpreadSource spreadSource,
        OfzLiquidityBucket bucket,
        OfzLiquidityMetricStatus status,
        double value,
        int numTrades,
        bool isSnapshot = false,
        bool isProvisional = false,
        DateTime? observedAt = null)
    {
        return new OfzLiquidityMetric
        {
            SecId = secId,
            TradeDate = tradeDate,
            Spread = spread,
            SpreadSource = spreadSource,
            LiquidityScore = spread.HasValue ? spread.Value * 100 : null,
            LiquidityBucket = bucket,
            Status = status,
            Value = value,
            NumTrades = numTrades,
            IsSnapshot = isSnapshot,
            IsProvisional = isProvisional,
            ObservedAt = observedAt
        };
    }

    private static OfzCashflowEvent CashflowEvent(
        string secId,
        OfzCashflowEventType eventType,
        DateTime eventDate,
        double? value = 10,
        double? valueRub = 10,
        double? valuePercent = 5,
        double? price = null)
    {
        return new OfzCashflowEvent
        {
            SecId = secId,
            SourceKey = $"{eventType}:{eventDate:yyyy-MM-dd}",
            ShortName = secId == "SU26238RMFS4" ? "ОФЗ 26238" : "ОФЗ 26239",
            EventType = eventType,
            EventDate = eventDate,
            Value = value,
            ValueRub = valueRub,
            ValuePercent = valuePercent,
            Price = price,
            SourceKind = OfzCashflowSourceKind.Schedule,
            LoadedAt = new DateTime(2026, 04, 20, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    private static bool HasEvidenceOrLimitation(OfzSummaryFinding finding)
    {
        var evidence = finding.Evidence;

        return finding.Limitations.Count > 0 ||
            evidence.TradeDate.HasValue ||
            evidence.IssueCount is > 0 ||
            evidence.ActiveIssueCount is > 0 ||
            evidence.RankableIssueCount is > 0 ||
            evidence.TotalValue is > 0 ||
            evidence.Value is > 0 ||
            evidence.TotalNumTrades is > 0 ||
            evidence.NumTrades is > 0 ||
            evidence.ActivityScore.HasValue ||
            evidence.MedianActivityScore.HasValue ||
            evidence.MaxActivityScore.HasValue ||
            evidence.YieldMove.HasValue ||
            evidence.YieldValue.HasValue ||
            evidence.Duration.HasValue ||
            evidence.Spread.HasValue ||
            evidence.LiquidityScore.HasValue ||
            evidence.ZSpread.HasValue ||
            evidence.ZSpreadBp.HasValue ||
            evidence.GSpreadBp.HasValue ||
            evidence.SpecialMetricKind.HasValue ||
            !string.IsNullOrWhiteSpace(evidence.SpecialMetricCode) ||
            evidence.SpecialMetricCount is > 0 ||
            evidence.ImpliedFloatingRate.HasValue ||
            evidence.ImpliedCbrRate.HasValue ||
            evidence.ImpliedFloatingRateSpread.HasValue ||
            evidence.ImpliedInflation.HasValue ||
            evidence.SpecialMetricAvailability.HasValue ||
            !string.IsNullOrWhiteSpace(evidence.IndexSecId) ||
            evidence.IndexClose.HasValue ||
            evidence.IndexDailyChange.HasValue ||
            evidence.IndexDailyChangePercent.HasValue ||
            evidence.IndexYield.HasValue ||
            evidence.IndexYieldChange.HasValue ||
            evidence.IndexDuration.HasValue ||
            evidence.IndexPreviousTradeDate.HasValue ||
            evidence.IndexDirection.HasValue ||
            evidence.CashflowEventType.HasValue ||
            evidence.CashflowEventDate.HasValue ||
            evidence.CashflowDaysToEvent.HasValue ||
            evidence.CashflowValue.HasValue ||
            evidence.CashflowValueRub.HasValue ||
            evidence.CashflowValuePercent.HasValue ||
            evidence.CashflowSourceKind.HasValue ||
            evidence.SeasonalityFindingKind.HasValue ||
            evidence.SeasonalityBucketKind.HasValue ||
            !string.IsNullOrWhiteSpace(evidence.SeasonalityBucketKey) ||
            !string.IsNullOrWhiteSpace(evidence.SeasonalityBucketLabel) ||
            evidence.SeasonalityActualValue.HasValue ||
            evidence.SeasonalityBaselineMedianValue.HasValue ||
            evidence.SeasonalityValueRatio.HasValue ||
            evidence.SeasonalityActualNumTrades.HasValue ||
            evidence.SeasonalityBaselineMedianNumTrades.HasValue ||
            evidence.SeasonalityBaselineObservationCount.HasValue ||
            !string.IsNullOrWhiteSpace(evidence.ExternalFactorCode) ||
            evidence.ExternalFactorKind.HasValue ||
            evidence.ExternalFactorSource.HasValue ||
            evidence.ExternalFactorLinkKind.HasValue ||
            evidence.ExternalFactorObservationDate.HasValue ||
            evidence.ExternalFactorValue.HasValue ||
            evidence.ExternalFactorDailyChange.HasValue ||
            evidence.ExternalFactorDailyChangePercent.HasValue ||
            evidence.ExternalFactorDirection.HasValue ||
            evidence.ExternalFactorMoveIsMeaningful;
    }

    private static bool ContainsRecommendationLanguage(string text)
    {
        string[] blockedWords = ["купить", "покупать", "продать", "продавать", "держать", "рекоменд", "лучше", "хуже", "выгод", "buy", "sell"];
        return blockedWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
