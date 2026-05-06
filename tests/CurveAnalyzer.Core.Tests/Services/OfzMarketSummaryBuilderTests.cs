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

        Assert.Equal("1.1", root.GetProperty("schemaVersion").GetString());
        AssertJsonDateOnly(root.GetProperty("startDate"));
        AssertJsonDateOnly(root.GetProperty("endDate"));
        AssertJsonDateOnly(root.GetProperty("insightStartDate"));
        AssertJsonDateOnly(root.GetProperty("insightEndDate"));
        Assert.Contains("T", root.GetProperty("generatedAt").GetString());
        Assert.True(root.TryGetProperty("findings", out _));
        Assert.True(root.TryGetProperty("segments", out _));
        Assert.True(root.TryGetProperty("breadthDays", out _));
        Assert.True(root.TryGetProperty("limitations", out _));
        Assert.True(root.TryGetProperty("sourceCounts", out _));
        Assert.NotEmpty(root.GetProperty("breadthDays").EnumerateArray());

        var breadthDay = root.GetProperty("breadthDays").EnumerateArray().First();
        Assert.True(breadthDay.TryGetProperty("direction", out var direction));
        Assert.True(direction.TryGetProperty("yieldUpCount", out _));
        Assert.True(direction.TryGetProperty("yieldDownCount", out _));
        Assert.True(direction.TryGetProperty("notComparableCount", out _));
        Assert.True(breadthDay.TryGetProperty("concentration", out var concentration));
        Assert.True(concentration.TryGetProperty("top5Share", out _));
        Assert.True(concentration.TryGetProperty("top10Share", out _));
        Assert.True(breadthDay.TryGetProperty("typeShares", out _));
        Assert.True(breadthDay.TryGetProperty("topContributors", out _));

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
            LiquidityMetrics = input.LiquidityMetrics
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
        double duration)
    {
        return new OfzDailyTrade
        {
            SecId = secId,
            TradeDate = tradeDate,
            Value = value,
            NumTrades = numTrades,
            YieldAtWeightedAveragePrice = yield,
            Duration = duration
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
            evidence.GSpreadBp.HasValue;
    }

    private static bool ContainsRecommendationLanguage(string text)
    {
        string[] blockedWords = ["купить", "покупать", "продать", "продавать", "держать", "рекоменд", "buy", "sell"];
        return blockedWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
