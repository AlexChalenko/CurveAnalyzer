using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Services;

public class OfzActivityAnalyzerTests
{
    [Fact]
    public void CalculateMetrics_UsesMedianBaselineAndMinimumBaselineDays()
    {
        var trades = Enumerable.Range(1, 10)
            .Select(day => Trade("SU26238RMFS4", new DateTime(2026, 01, day), day % 2 == 0 ? 120_000_000 : 100_000_000, 7))
            .Append(Trade("SU26238RMFS4", new DateTime(2026, 01, 11), 330_000_000, 7.25))
            .ToList();

        var metrics = OfzActivityAnalyzer.CalculateMetrics(trades);

        var metric = Assert.Single(metrics, item => item.TradeDate == new DateTime(2026, 01, 11));
        Assert.Equal(OfzActivityMetricStatus.Ready, metric.Status);
        Assert.Equal(10, metric.BaselineDays);
        Assert.Equal(110_000_000, metric.BaselineMedianValue);
        Assert.Equal(3, metric.ActivityScore);
    }

    [Fact]
    public void CalculateMetrics_ReportsInsufficientBaselineAndMissingValue()
    {
        var trades = Enumerable.Range(1, 9)
            .Select(day => Trade("SU26238RMFS4", new DateTime(2026, 01, day), 100_000_000, 7))
            .Append(Trade("SU26238RMFS4", new DateTime(2026, 01, 10), 200_000_000, 7.1))
            .Append(Trade("SU26238RMFS4", new DateTime(2026, 01, 11), null, 7.2))
            .ToList();

        var metrics = OfzActivityAnalyzer.CalculateMetrics(trades);

        var insufficient = Assert.Single(metrics, item => item.TradeDate == new DateTime(2026, 01, 10));
        Assert.Equal(OfzActivityMetricStatus.InsufficientBaseline, insufficient.Status);
        Assert.Null(insufficient.ActivityScore);

        var missingValue = Assert.Single(metrics, item => item.TradeDate == new DateTime(2026, 01, 11));
        Assert.Equal(OfzActivityMetricStatus.MissingValue, missingValue.Status);
        Assert.Null(missingValue.ActivityScore);
    }

    [Fact]
    public void CalculateMetrics_ReportsInsufficientBaselineWhenMedianIsTooSmall()
    {
        var trades = Enumerable.Range(1, 10)
            .Select(day => Trade("SU26238RMFS4", new DateTime(2026, 01, day), 1_000_000, 7))
            .Append(Trade("SU26238RMFS4", new DateTime(2026, 01, 11), 2_000_000_000, 7.2))
            .ToList();

        var metrics = OfzActivityAnalyzer.CalculateMetrics(trades);

        var metric = Assert.Single(metrics, item => item.TradeDate == new DateTime(2026, 01, 11));
        Assert.Equal(OfzActivityMetricStatus.InsufficientBaseline, metric.Status);
        Assert.Equal(1_000_000, metric.BaselineMedianValue);
        Assert.Null(metric.ActivityScore);
    }

    [Fact]
    public void CalculateMetrics_UsesWeightedAverageYieldWithCloseFallback()
    {
        var trades = Enumerable.Range(1, 10)
            .Select(day => Trade(
                "SU26238RMFS4",
                new DateTime(2026, 01, day),
                100_000_000,
                yieldAtWeightedAveragePrice: day == 10 ? null : 7,
                yieldClose: day == 10 ? 7.1 : null))
            .Append(Trade("SU26238RMFS4", new DateTime(2026, 01, 11), 200_000_000, yieldAtWeightedAveragePrice: 7.35, yieldClose: 9))
            .ToList();

        var metrics = OfzActivityAnalyzer.CalculateMetrics(trades);

        var metric = Assert.Single(metrics, item => item.TradeDate == new DateTime(2026, 01, 11));
        Assert.Equal(OfzActivityMetricStatus.Ready, metric.Status);
        Assert.Equal(7.35, metric.YieldValue);
        Assert.Equal(7.1, metric.PreviousYieldValue);
        Assert.Equal(0.25, metric.YieldMove!.Value, 10);
    }

    [Fact]
    public void CalculateMetrics_TreatsZeroYieldAsMissing()
    {
        var trades = Enumerable.Range(1, 10)
            .Select(day => Trade("SU26238RMFS4", new DateTime(2026, 01, day), 100_000_000, 0, 0))
            .Append(Trade("SU26238RMFS4", new DateTime(2026, 01, 11), 200_000_000, 0, 0))
            .ToList();

        var metrics = OfzActivityAnalyzer.CalculateMetrics(trades);

        var metric = Assert.Single(metrics, item => item.TradeDate == new DateTime(2026, 01, 11));
        Assert.Equal(OfzActivityMetricStatus.MissingYield, metric.Status);
        Assert.Null(metric.YieldValue);
        Assert.Null(metric.YieldMove);
    }

    [Fact]
    public void GetTopAnomalies_RanksByActivityScoreThenValue()
    {
        OfzActivityMetric[] metrics =
        [
            Metric("LOW_VALUE", 5, 100),
            Metric("HIGH_VALUE", 5, 200),
            Metric("BEST_SCORE", 6, 50)
        ];

        OfzIssue[] issues =
        [
            new() { SecId = "LOW_VALUE", ShortName = "ОФЗ low", FaceUnit = "SUR" },
            new() { SecId = "HIGH_VALUE", ShortName = "ОФЗ high", FaceUnit = "SUR" },
            new() { SecId = "BEST_SCORE", ShortName = "ОФЗ best", FaceUnit = "SUR" }
        ];

        var anomalies = OfzActivityAnalyzer.GetTopAnomalies(metrics, issues, topCount: 3, minimumBaselineMedianValue: 0);

        Assert.Collection(
            anomalies,
            first => Assert.Equal("BEST_SCORE", first.SecId),
            second => Assert.Equal("HIGH_VALUE", second.SecId),
            third => Assert.Equal("LOW_VALUE", third.SecId));
        Assert.Equal([1, 2, 3], anomalies.Select(item => item.Rank));
    }

    [Fact]
    public void GetTopAnomalies_ExcludesTinyBaselineMedianByDefault()
    {
        OfzActivityMetric[] metrics =
        [
            Metric("NOISY", 1000, 1_000_000_000, baselineMedianValue: 1_000_000),
            Metric("STABLE", 20, 1_000_000_000, baselineMedianValue: 50_000_000)
        ];

        OfzIssue[] issues =
        [
            new() { SecId = "NOISY", ShortName = "ОФЗ noisy", FaceUnit = "SUR" },
            new() { SecId = "STABLE", ShortName = "ОФЗ stable", FaceUnit = "SUR" }
        ];

        var anomalies = OfzActivityAnalyzer.GetTopAnomalies(metrics, issues, topCount: 5);

        var anomaly = Assert.Single(anomalies);
        Assert.Equal("STABLE", anomaly.SecId);
    }

    [Fact]
    public void BuildHeatmapCells_CreatesMatrixCellsWithNoDataStatus()
    {
        var date1 = new DateTime(2026, 01, 11);
        var date2 = new DateTime(2026, 01, 12);
        OfzActivityMetric[] metrics =
        [
            Metric("SU26238RMFS4", date1, 1.2, 120_000_000),
            Metric("SU26239RMFS2", date2, 6, 600_000_000)
        ];

        OfzIssue[] issues =
        [
            new() { SecId = "SU26238RMFS4", ShortName = "ОФЗ 26238", FaceUnit = "SUR" },
            new() { SecId = "SU26239RMFS2", ShortName = "ОФЗ 26239", FaceUnit = "SUR" }
        ];

        var cells = OfzActivityAnalyzer.BuildHeatmapCells(metrics, issues, date1, date2);

        Assert.Equal(4, cells.Count);
        var missingCell = Assert.Single(cells, cell => cell.SecId == "SU26238RMFS4" && cell.TradeDate == date2);
        Assert.Equal(OfzActivityMetricStatus.NoData, missingCell.Status);
        Assert.Equal(0, missingCell.ScoreBucket);
        Assert.Null(missingCell.ActivityScore);

        var activeCell = Assert.Single(cells, cell => cell.SecId == "SU26239RMFS2" && cell.TradeDate == date2);
        Assert.Equal("ОФЗ 26239", activeCell.ShortName);
        Assert.Equal(4, activeCell.ScoreBucket);
    }

    [Fact]
    public void BuildHeatmapCells_SeparatesInsufficientBaselineFromNoData()
    {
        var date = new DateTime(2026, 01, 11);
        OfzActivityMetric[] metrics =
        [
            new()
            {
                SecId = "SU26238RMFS4",
                TradeDate = date,
                Value = 100_000_000,
                BaselineMedianValue = 5_000_000,
                BaselineDays = 10,
                Status = OfzActivityMetricStatus.InsufficientBaseline
            }
        ];

        var cells = OfzActivityAnalyzer.BuildHeatmapCells(metrics, [], date, date);

        var cell = Assert.Single(cells);
        Assert.Equal(OfzActivityMetricStatus.InsufficientBaseline, cell.Status);
        Assert.Equal(1, cell.ScoreBucket);
        Assert.Null(cell.ActivityScore);
    }

    [Fact]
    public void BuildIssueDetail_OrdersByDateAndUsesPreferredYieldAndPriceFallback()
    {
        OfzDailyTrade[] trades =
        [
            Trade(
                "SU26238RMFS4",
                new DateTime(2026, 01, 12),
                120_000_000,
                yieldAtWeightedAveragePrice: null,
                yieldClose: 7.15,
                weightedAveragePrice: null,
                closePrice: 96.4),
            Trade(
                "SU26238RMFS4",
                new DateTime(2026, 01, 11),
                100_000_000,
                yieldAtWeightedAveragePrice: 7.05,
                yieldClose: 7.25,
                weightedAveragePrice: 96.7,
                closePrice: 96.1),
            Trade(
                "OTHER",
                new DateTime(2026, 01, 11),
                900_000_000,
                yieldAtWeightedAveragePrice: 8.1,
                weightedAveragePrice: 101)
        ];
        var issue = new OfzIssue
        {
            SecId = "SU26238RMFS4",
            ShortName = "ОФЗ 26238",
            FaceUnit = "SUR"
        };

        var detail = OfzActivityAnalyzer.BuildIssueDetail("SU26238RMFS4", trades, issue);

        Assert.Equal("ОФЗ 26238", detail.ShortName);
        Assert.True(detail.HasYield);
        Assert.True(detail.HasPrice);
        Assert.Collection(
            detail.Points,
            first =>
            {
                Assert.Equal(new DateTime(2026, 01, 11), first.TradeDate);
                Assert.Equal(96.7, first.Price);
                Assert.Equal(7.05, first.Yield);
            },
            second =>
            {
                Assert.Equal(new DateTime(2026, 01, 12), second.TradeDate);
                Assert.Equal(96.4, second.Price);
                Assert.Equal(7.15, second.Yield);
            });
    }

    [Fact]
    public void BuildActivityIndex_AggregatesActiveMetricsByDate()
    {
        var date1 = new DateTime(2026, 04, 20);
        var date2 = new DateTime(2026, 04, 21);
        OfzActivityMetric[] metrics =
        [
            ScatterMetric("ACTIVE_1", date1, 2, 100_000_000, 12, 900),
            ScatterMetric("ACTIVE_2", date1, 4, 300_000_000, 14, 1500, numTrades: 20),
            ScatterMetric("QUIET", date1, 1.5, 900_000_000, 13, 1200),
            new()
            {
                SecId = "NO_SCORE",
                TradeDate = date1,
                Value = 50_000_000,
                NumTrades = 10,
                Status = OfzActivityMetricStatus.InsufficientBaseline
            },
            ScatterMetric("ACTIVE_3", date2, 6, 600_000_000, 15, 1800, numTrades: null)
        ];

        var points = OfzActivityAnalyzer.BuildActivityIndex(metrics);

        Assert.Collection(
            points,
            first =>
            {
                Assert.Equal(date1, first.TradeDate);
                Assert.Equal(1_350_000_000, first.TotalValue);
                Assert.Equal(50, first.TotalNumTrades);
                Assert.Equal(4, first.ActiveIssueCount);
                Assert.Equal(3, first.RankableIssueCount);
                Assert.Equal(2, first.MedianActivityScore);
                Assert.Equal(8, first.ActivityIndex);
            },
            second =>
            {
                Assert.Equal(date2, second.TradeDate);
                Assert.Equal(600_000_000, second.TotalValue);
                Assert.Equal(0, second.TotalNumTrades);
                Assert.Equal(1, second.ActiveIssueCount);
                Assert.Equal(1, second.RankableIssueCount);
                Assert.Equal(6, second.MedianActivityScore);
                Assert.Equal(6, second.ActivityIndex);
            });
    }

    [Fact]
    public void BuildActivityIndex_KeepsTurnoverWhenNoRankableScores()
    {
        var date = new DateTime(2026, 04, 20);
        OfzActivityMetric[] metrics =
        [
            new()
            {
                SecId = "NO_SCORE",
                TradeDate = date,
                Value = 100_000_000,
                NumTrades = 7,
                Status = OfzActivityMetricStatus.InsufficientBaseline
            }
        ];

        var point = Assert.Single<OfzActivityIndexPoint>(OfzActivityAnalyzer.BuildActivityIndex(metrics));

        Assert.Equal(100_000_000, point.TotalValue);
        Assert.Equal(7, point.TotalNumTrades);
        Assert.Equal(1, point.ActiveIssueCount);
        Assert.Equal(0, point.RankableIssueCount);
        Assert.Null(point.MedianActivityScore);
        Assert.Equal(0, point.ActivityIndex);
    }

    [Fact]
    public void BuildDurationYieldScatter_FiltersMissingFieldsAndKeepsTopActivity()
    {
        var date = new DateTime(2026, 04, 20);
        OfzActivityMetric[] metrics =
        [
            ScatterMetric("VALID_LOW", date, 5, 1_000_000_000, 12.4, 900),
            ScatterMetric("VALID_HIGH", date, 10, 2_000_000_000, 11.8, 500),
            ScatterMetric("VALID_HIGH", date.AddDays(1), 8, 5_000_000_000, 12.1, 600),
            ScatterMetric("NO_DURATION", date, 20, 3_000_000_000, 13.1, null),
            ScatterMetric("NO_YIELD", date, 20, 3_000_000_000, null, 1200),
            ScatterMetric("QUIET", date, 1.5, 9_000_000_000, 14.2, 1500),
            ScatterMetric("MISSING_PREVIOUS_YIELD", date, 6, 900_000_000, 13.4, 1000, status: OfzActivityMetricStatus.MissingYield)
        ];
        OfzIssue[] issues =
        [
            new() { SecId = "VALID_LOW", ShortName = "ОФЗ 26238", FaceUnit = "SUR" },
            new() { SecId = "VALID_HIGH", ShortName = "ОФЗ 29019", FaceUnit = "SUR" },
            new() { SecId = "MISSING_PREVIOUS_YIELD", ShortName = "ОФЗ 26239", FaceUnit = "SUR" }
        ];

        var points = OfzActivityAnalyzer.BuildDurationYieldScatter(metrics, issues);

        Assert.DoesNotContain(points, point => point.SecId is "NO_DURATION" or "NO_YIELD");
        Assert.Contains(points, point => point.SecId == "MISSING_PREVIOUS_YIELD");
        Assert.Equal(4, points.Count);

        var point = Assert.Single(points, point => point.SecId == "VALID_HIGH");
        Assert.Equal("VALID_HIGH", point.SecId);
        Assert.Equal("ОФЗ 29019", point.ShortName);
        Assert.Equal(500, point.Duration);
        Assert.Equal(11.8, point.Yield);
        Assert.Equal(10, point.ActivityScore);
        Assert.True(point.ScoreBucket >= 4);
    }

    [Fact]
    public void BuildDurationYieldScatter_CanFilterByTradeDate()
    {
        var date = new DateTime(2026, 04, 20);
        OfzActivityMetric[] metrics =
        [
            ScatterMetric("A", date, 3, 300_000_000, 12.4, 900),
            ScatterMetric("B", date.AddDays(1), 8, 800_000_000, 13.4, 1200)
        ];

        var point = Assert.Single(OfzActivityAnalyzer.BuildDurationYieldScatter(metrics, [], tradeDate: date));

        Assert.Equal("A", point.SecId);
        Assert.Equal(date, point.TradeDate);
    }

    [Fact]
    public void BuildActivityInsights_DetectsRepeatedIssueAndCouponTypeConcentration()
    {
        OfzActivityMetric[] metrics =
        [
            InsightMetric("FLOAT_1", new DateTime(2026, 04, 01), 3, 300_000_000, 500),
            InsightMetric("FLOAT_1", new DateTime(2026, 04, 02), 4, 400_000_000, 600),
            InsightMetric("FLOAT_2", new DateTime(2026, 04, 02), 2.5, 250_000_000, 700),
            InsightMetric("FIXED_1", new DateTime(2026, 04, 02), 2.2, 220_000_000, 800)
        ];
        OfzIssue[] issues =
        [
            new() { SecId = "FLOAT_1", ShortName = "ОФЗ 29019", FaceUnit = "SUR", BondType = "Флоатер" },
            new() { SecId = "FLOAT_2", ShortName = "ОФЗ 29020", FaceUnit = "SUR", BondType = "Флоатер" },
            new() { SecId = "FIXED_1", ShortName = "ОФЗ 26238", FaceUnit = "SUR", BondType = "Фикс с известным купоном" }
        ];

        var insights = OfzActivityAnalyzer.BuildActivityInsights(metrics, issues);

        Assert.Contains(insights, insight =>
            insight.Kind == OfzActivityInsightKind.RepeatedIssueActivity &&
            insight.SecId == "FLOAT_1");
        Assert.Contains(insights, insight =>
            insight.Kind == OfzActivityInsightKind.CouponTypeConcentration &&
            insight.CouponType == OfzCouponType.Floating);
    }

    [Fact]
    public void BuildActivityInsights_DetectsYieldMoveAndBlockLikeActivity()
    {
        OfzActivityMetric[] metrics =
        [
            InsightMetric(
                "SU26238RMFS4",
                new DateTime(2026, 04, 20),
                8,
                2_000_000_000,
                120,
                yieldMove: -0.35),
            InsightMetric(
                "SU26239RMFS2",
                new DateTime(2026, 04, 20),
                2.5,
                300_000_000,
                1_500)
        ];
        OfzIssue[] issues =
        [
            new() { SecId = "SU26238RMFS4", ShortName = "ОФЗ 26238", FaceUnit = "SUR", BondType = "Фикс с известным купоном" },
            new() { SecId = "SU26239RMFS2", ShortName = "ОФЗ 26239", FaceUnit = "SUR", BondType = "Фикс с известным купоном" }
        ];

        var insights = OfzActivityAnalyzer.BuildActivityInsights(metrics, issues);

        Assert.Contains(insights, insight =>
            insight.Kind == OfzActivityInsightKind.YieldMoveActivity &&
            insight.SecId == "SU26238RMFS4" &&
            insight.YieldMove == -0.35);
        Assert.Contains(insights, insight =>
            insight.Kind == OfzActivityInsightKind.BlockLikeActivity &&
            insight.SecId == "SU26238RMFS4");
        Assert.Contains(insights, insight =>
            insight.Kind == OfzActivityInsightKind.TradeCountActivity &&
            insight.SecId == "SU26239RMFS2");
    }

    private static OfzDailyTrade Trade(
        string secId,
        DateTime date,
        double? value,
        double? yieldAtWeightedAveragePrice,
        double? yieldClose = null,
        double? weightedAveragePrice = null,
        double? closePrice = null)
    {
        return new OfzDailyTrade
        {
            SecId = secId,
            TradeDate = date,
            Value = value,
            NumTrades = value.HasValue ? 10 : null,
            YieldAtWeightedAveragePrice = yieldAtWeightedAveragePrice,
            YieldClose = yieldClose,
            WeightedAveragePrice = weightedAveragePrice,
            ClosePrice = closePrice,
            Duration = 1000
        };
    }

    private static OfzActivityMetric Metric(
        string secId,
        double activityScore,
        double value,
        double baselineMedianValue = 100)
    {
        return Metric(secId, new DateTime(2026, 01, 11), activityScore, value, baselineMedianValue);
    }

    private static OfzActivityMetric Metric(
        string secId,
        DateTime tradeDate,
        double activityScore,
        double value,
        double baselineMedianValue = 100)
    {
        return new OfzActivityMetric
        {
            SecId = secId,
            TradeDate = tradeDate,
            Value = value,
            ActivityScore = activityScore,
            BaselineMedianValue = baselineMedianValue,
            BaselineDays = 10,
            Status = OfzActivityMetricStatus.Ready
        };
    }

    private static OfzActivityMetric InsightMetric(
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
            YieldMove = yieldMove,
            Status = yieldMove.HasValue
                ? OfzActivityMetricStatus.Ready
                : OfzActivityMetricStatus.MissingYield
        };
    }

    private static OfzActivityMetric ScatterMetric(
        string secId,
        DateTime tradeDate,
        double activityScore,
        double value,
        double? yield,
        double? duration,
        int? numTrades = 10,
        OfzActivityMetricStatus? status = null)
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
            YieldValue = yield,
            Duration = duration,
            Status = status ?? (yield.HasValue
                ? OfzActivityMetricStatus.Ready
                : OfzActivityMetricStatus.MissingYield)
        };
    }
}
