using System.Diagnostics;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Services;

public class OfzSeasonalityContextBuilderTests
{
    [Fact]
    public void Build_AggregatesWeekdayAndMonthBucketsWithoutReplacingMissingWithZero()
    {
        var startDate = new DateTime(2026, 04, 01);
        var context = OfzSeasonalityContextBuilder.Build(
            [
                ActivityMetric("SU26238RMFS4", startDate, value: 100_000_000, numTrades: 10, yieldMove: 0.02),
                ActivityMetric("SU26239RMFS2", startDate, value: null, numTrades: null, yieldMove: null),
                ActivityMetric("SU26238RMFS4", startDate.AddDays(1), value: null, numTrades: 5, yieldMove: -0.03),
                ActivityMetric("SU26238RMFS4", startDate.AddDays(7), value: 200_000_000, numTrades: 20, yieldMove: 0),
                ActivityMetric("SU26238RMFS4", startDate.AddMonths(1), value: 300_000_000, numTrades: 30, yieldMove: 0)
            ],
            new OfzSeasonalityContextOptions
            {
                StartDate = startDate,
                EndDate = startDate.AddMonths(1),
                MinWeekdayBaselineObservations = 2,
                MinMonthBaselineObservations = 1
            });

        var wednesday = Assert.Single(context.WeekdayBuckets, bucket => bucket.Key == "Wednesday");
        Assert.Equal(2, wednesday.ObservationCount);
        Assert.Equal(2, wednesday.ActiveDayCount);
        Assert.Equal(150_000_000, wednesday.MedianTotalValue);
        Assert.Equal(15, wednesday.MedianNumTrades);
        Assert.Equal(1, wednesday.MedianActiveIssueCount);

        var thursday = Assert.Single(context.WeekdayBuckets, bucket => bucket.Key == "Thursday");
        Assert.Null(thursday.MedianTotalValue);
        Assert.Equal(5, thursday.MedianNumTrades);
        Assert.Equal(1, thursday.ActiveDayCount);

        var april = Assert.Single(context.MonthBuckets, bucket => bucket.Key == "April");
        Assert.Equal(3, april.ObservationCount);
        Assert.Equal(150_000_000, april.MedianTotalValue);
    }

    [Fact]
    public void Build_KeepsInsufficientBaselineAsLimitation()
    {
        var tradeDate = new DateTime(2026, 04, 06);
        var context = OfzSeasonalityContextBuilder.Build(
            [ActivityMetric("SU26238RMFS4", tradeDate, value: 100_000_000, numTrades: 10)],
            new OfzSeasonalityContextOptions
            {
                StartDate = tradeDate,
                EndDate = tradeDate,
                MinWeekdayBaselineObservations = 4,
                MinMonthBaselineObservations = 3
            });

        Assert.Empty(context.Findings);
        Assert.Contains(context.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.InsufficientBaseline);
        Assert.Contains(context.WeekdayBuckets.Single().Limitations, limitation =>
            limitation.Kind == OfzDataLimitationKind.InsufficientBaseline);
    }

    [Fact]
    public void Build_UsesOnlyPreviousSameBucketObservationsForFindings()
    {
        var firstMonday = new DateTime(2026, 04, 06);
        var context = OfzSeasonalityContextBuilder.Build(
            [
                ActivityMetric("SU26238RMFS4", firstMonday, value: 100_000_000, numTrades: 10),
                ActivityMetric("SU26238RMFS4", firstMonday.AddDays(7), value: 110_000_000, numTrades: 11),
                ActivityMetric("SU26238RMFS4", firstMonday.AddDays(14), value: 90_000_000, numTrades: 9),
                ActivityMetric("SU26238RMFS4", firstMonday.AddDays(21), value: 100_000_000, numTrades: 10),
                ActivityMetric("SU26238RMFS4", firstMonday.AddDays(28), value: 260_000_000, numTrades: 26),
                ActivityMetric("SU26238RMFS4", firstMonday.AddDays(35), value: 10_000_000, numTrades: 1)
            ],
            new OfzSeasonalityContextOptions
            {
                StartDate = firstMonday,
                EndDate = firstMonday.AddDays(35),
                InsightStartDate = firstMonday.AddDays(28),
                InsightEndDate = firstMonday.AddDays(28),
                MinWeekdayBaselineObservations = 4
            });

        var finding = Assert.Single(context.Findings, item => item.BucketKind == OfzSeasonalityBucketKind.Weekday);
        Assert.Equal(OfzSeasonalityFindingKind.HighSeasonalActivity, finding.Kind);
        Assert.Equal(firstMonday.AddDays(28), finding.TradeDate);
        Assert.Equal(100_000_000, finding.BaselineMedianValue);
        Assert.Equal(2.6, finding.ValueRatio!.Value, 1);
        Assert.Equal(4, finding.BaselineObservationCount);
    }

    [Fact]
    public void Build_RespectsLastAvailableDaySignalScopeForFindings()
    {
        var firstMonday = new DateTime(2026, 04, 06);
        var context = OfzSeasonalityContextBuilder.Build(
            [
                ActivityMetric("SU26238RMFS4", firstMonday, value: 100_000_000, numTrades: 10),
                ActivityMetric("SU26238RMFS4", firstMonday.AddDays(7), value: 100_000_000, numTrades: 10),
                ActivityMetric("SU26238RMFS4", firstMonday.AddDays(14), value: 100_000_000, numTrades: 10),
                ActivityMetric("SU26238RMFS4", firstMonday.AddDays(21), value: 100_000_000, numTrades: 10),
                ActivityMetric("SU26238RMFS4", firstMonday.AddDays(28), value: 250_000_000, numTrades: 25),
                ActivityMetric("SU26238RMFS4", firstMonday.AddDays(35), value: 20_000_000, numTrades: 2)
            ],
            new OfzSeasonalityContextOptions
            {
                StartDate = firstMonday,
                EndDate = firstMonday.AddDays(35),
                InsightStartDate = firstMonday.AddDays(28),
                InsightEndDate = firstMonday.AddDays(35),
                SignalScope = OfzSummarySignalScope.LastAvailableDay,
                MinWeekdayBaselineObservations = 4
            });

        var finding = Assert.Single(context.Findings);
        Assert.Equal(firstMonday.AddDays(35), finding.TradeDate);
        Assert.Equal(OfzSeasonalityFindingKind.LowSeasonalActivity, finding.Kind);
    }

    [Fact]
    public void Build_OneYearSyntheticDatasetCompletesQuickly()
    {
        var startDate = new DateTime(2025, 05, 12);
        var metrics = Enumerable.Range(0, 260)
            .SelectMany(dayIndex =>
            {
                var date = startDate.AddDays(dayIndex);
                return Enumerable.Range(0, 20)
                    .Select(issueIndex => ActivityMetric(
                        $"SU26{issueIndex:D3}RMFS{issueIndex}",
                        date,
                        value: 50_000_000 + dayIndex * 10_000 + issueIndex,
                        numTrades: 10 + issueIndex,
                        yieldMove: issueIndex % 2 == 0 ? 0.02 : -0.02));
            })
            .ToArray();

        var stopwatch = Stopwatch.StartNew();
        var context = OfzSeasonalityContextBuilder.Build(
            metrics,
            new OfzSeasonalityContextOptions
            {
                StartDate = startDate,
                EndDate = startDate.AddDays(259),
                InsightStartDate = startDate.AddDays(250),
                InsightEndDate = startDate.AddDays(259)
            });
        stopwatch.Stop();

        Assert.Equal(260, context.ObservationCount);
        Assert.NotEmpty(context.WeekdayBuckets);
        Assert.NotEmpty(context.MonthBuckets);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), stopwatch.Elapsed.ToString());
    }

    private static OfzActivityMetric ActivityMetric(
        string secId,
        DateTime tradeDate,
        double? value,
        int? numTrades,
        double? yieldMove = null)
    {
        return new OfzActivityMetric
        {
            SecId = secId,
            TradeDate = tradeDate,
            Value = value,
            NumTrades = numTrades,
            YieldMove = yieldMove,
            YieldValue = yieldMove.HasValue ? 12 + yieldMove.Value : null,
            PreviousYieldValue = yieldMove.HasValue ? 12 : null,
            ActivityScore = value.HasValue ? Math.Max(1, value.Value / 100_000_000) : null,
            BaselineMedianValue = 100_000_000,
            BaselineDays = 10,
            Status = yieldMove.HasValue ? OfzActivityMetricStatus.Ready : OfzActivityMetricStatus.MissingYield
        };
    }
}
