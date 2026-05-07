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
    public void CalculateLiquidityMetrics_KeepsMissingSpreadSeparateFromValidZeroSpread()
    {
        OfzDailyTrade[] trades =
        [
            LiquidityTrade("MISSING", new DateTime(2026, 04, 20), 100_000_000, bid: null, offer: null, spread: null),
            LiquidityTrade("ZERO_WITHOUT_QUOTES", new DateTime(2026, 04, 20), 110_000_000, bid: null, offer: null, spread: 0),
            LiquidityTrade("ZERO", new DateTime(2026, 04, 20), 120_000_000, bid: 100, offer: 100, spread: 0),
            LiquidityTrade("PROVIDED", new DateTime(2026, 04, 20), 130_000_000, bid: null, offer: null, spread: 0.12),
            LiquidityTrade("CALCULATED", new DateTime(2026, 04, 20), 140_000_000, bid: 99.7, offer: 100.1, spread: null)
        ];

        var metrics = OfzActivityAnalyzer.CalculateLiquidityMetrics(trades);

        var missing = Assert.Single(metrics, metric => metric.SecId == "MISSING");
        Assert.Null(missing.Spread);
        Assert.Null(missing.LiquidityScore);
        Assert.Equal(OfzSpreadSource.Missing, missing.SpreadSource);
        Assert.Equal(OfzLiquidityMetricStatus.MissingQuotes, missing.Status);
        Assert.Equal(OfzLiquidityBucket.MissingData, missing.LiquidityBucket);

        var zeroWithoutQuotes = Assert.Single(metrics, metric => metric.SecId == "ZERO_WITHOUT_QUOTES");
        Assert.Null(zeroWithoutQuotes.Spread);
        Assert.Null(zeroWithoutQuotes.LiquidityScore);
        Assert.Equal(OfzSpreadSource.Missing, zeroWithoutQuotes.SpreadSource);
        Assert.Equal(OfzLiquidityMetricStatus.MissingQuotes, zeroWithoutQuotes.Status);
        Assert.Equal(OfzLiquidityBucket.MissingData, zeroWithoutQuotes.LiquidityBucket);

        var zero = Assert.Single(metrics, metric => metric.SecId == "ZERO");
        Assert.Equal(0, zero.Spread);
        Assert.Equal(0, zero.LiquidityScore);
        Assert.Equal(OfzSpreadSource.Provided, zero.SpreadSource);
        Assert.Equal(OfzLiquidityMetricStatus.Ready, zero.Status);
        Assert.Equal(OfzLiquidityBucket.Good, zero.LiquidityBucket);

        var provided = Assert.Single(metrics, metric => metric.SecId == "PROVIDED");
        Assert.Equal(0.12, provided.Spread);
        Assert.Equal(OfzSpreadSource.Provided, provided.SpreadSource);

        var calculated = Assert.Single(metrics, metric => metric.SecId == "CALCULATED");
        Assert.Equal(0.4, calculated.Spread!.Value, 10);
        Assert.Equal(OfzSpreadSource.CalculatedFromBidOffer, calculated.SpreadSource);
    }

    [Fact]
    public void GetWeakLiquidityRankings_OrdersByBucketScoreAndValue()
    {
        var date = new DateTime(2026, 04, 20);
        OfzDailyTrade[] trades =
        [
            LiquidityTrade("GOOD", date, 2_000_000_000, bid: 100, offer: 100.02, spread: null),
            LiquidityTrade("WEAK", date, 600_000_000, bid: 100, offer: 100.32, spread: null),
            LiquidityTrade("PROBLEM_LOW_VALUE", date, 300_000_000, bid: 100, offer: 100.8, spread: null),
            LiquidityTrade("PROBLEM_HIGH_VALUE", date, 900_000_000, bid: 100, offer: 100.8, spread: null)
        ];
        OfzIssue[] issues =
        [
            new() { SecId = "WEAK", ShortName = "Weak issue", FaceUnit = "SUR" },
            new() { SecId = "PROBLEM_LOW_VALUE", ShortName = "Problem low", FaceUnit = "SUR" },
            new() { SecId = "PROBLEM_HIGH_VALUE", ShortName = "Problem high", FaceUnit = "SUR" }
        ];

        var liquidityMetrics = OfzActivityAnalyzer.CalculateLiquidityMetrics(trades);
        var rankings = OfzActivityAnalyzer.GetWeakLiquidityRankings(liquidityMetrics, issues);

        Assert.DoesNotContain(rankings, item => item.SecId == "GOOD");
        Assert.Collection(
            rankings,
            first =>
            {
                Assert.Equal("PROBLEM_HIGH_VALUE", first.SecId);
                Assert.Equal(OfzLiquidityBucket.Problem, first.LiquidityBucket);
                Assert.Equal(1, first.Rank);
            },
            second => Assert.Equal("PROBLEM_LOW_VALUE", second.SecId),
            third =>
            {
                Assert.Equal("WEAK", third.SecId);
                Assert.Equal(OfzLiquidityBucket.Weak, third.LiquidityBucket);
            });
    }

    [Fact]
    public void GetWeakLiquidityRankings_IncludesCurrentSnapshotWithMissingQuotes()
    {
        var date = new DateTime(2026, 04, 20);
        OfzLiquiditySnapshot[] snapshots =
        [
            new()
            {
                SecId = "ACTIVE_MISSING",
                TradeDate = date,
                ObservedAt = new DateTime(2026, 04, 20, 12, 0, 0, DateTimeKind.Utc),
                ValueToday = 700_000_000,
                NumTrades = 10
            }
        ];

        var liquidityMetrics = OfzActivityAnalyzer.CalculateSnapshotLiquidityMetrics(snapshots);
        var item = Assert.Single(OfzActivityAnalyzer.GetWeakLiquidityRankings(liquidityMetrics, []));

        Assert.Equal("ACTIVE_MISSING", item.SecId);
        Assert.Equal(OfzLiquidityBucket.MissingData, item.LiquidityBucket);
        Assert.Equal(OfzLiquidityMetricStatus.MissingQuotes, item.Status);
        Assert.True(item.IsSnapshot);
        Assert.Null(item.Spread);
        Assert.Null(item.LiquidityScore);
        Assert.Equal(700_000_000, item.Value);
    }

    [Fact]
    public void BuildIssueLiquidityProfile_KeepsCurrentSnapshotSeparateFromHistoricalMetrics()
    {
        var date = new DateTime(2026, 04, 20);
        OfzDailyTrade[] trades =
        [
            LiquidityTrade("SU26238RMFS4", date, 100_000_000, bid: 99.9, offer: 100.1, spread: null)
        ];
        var snapshot = new OfzLiquiditySnapshot
        {
            SecId = "SU26238RMFS4",
            TradeDate = date,
            ObservedAt = new DateTime(2026, 04, 20, 12, 0, 0, DateTimeKind.Utc),
            Bid = 99.5,
            Offer = 100.5,
            Spread = 1,
            BidDepthTotal = 10_000_000,
            OfferDepthTotal = 11_000_000,
            IsProvisional = true
        };

        var profile = OfzActivityAnalyzer.BuildIssueLiquidityProfile("SU26238RMFS4", trades, snapshot);

        var historical = Assert.Single(profile.HistoricalMetrics);
        Assert.False(historical.IsSnapshot);
        Assert.False(historical.IsProvisional);
        Assert.Equal(0.2, historical.Spread!.Value, 10);
        Assert.Equal(OfzSpreadSource.CalculatedFromBidOffer, historical.SpreadSource);

        Assert.NotNull(profile.CurrentSnapshotMetric);
        Assert.True(profile.CurrentSnapshotMetric.IsSnapshot);
        Assert.True(profile.CurrentSnapshotMetric.IsProvisional);
        Assert.Equal(OfzLiquidityMetricStatus.SnapshotOnly, profile.CurrentSnapshotMetric.Status);
        Assert.Equal(1, profile.CurrentSnapshotMetric.Spread);
    }

    [Fact]
    public void BuildIssueDetail_KeepsMissingSnapshotQuotesAsMissingValues()
    {
        var date = new DateTime(2026, 04, 20);
        OfzDailyTrade[] trades =
        [
            LiquidityTrade("SU26238RMFS4", date, 100_000_000, bid: null, offer: null, spread: null)
        ];
        var snapshot = new OfzLiquiditySnapshot
        {
            SecId = "SU26238RMFS4",
            TradeDate = date,
            ObservedAt = new DateTime(2026, 04, 20, 12, 0, 0, DateTimeKind.Utc),
            ValueToday = 150_000_000,
            NumTrades = 200,
            IsProvisional = true
        };

        var detail = OfzActivityAnalyzer.BuildIssueDetail("SU26238RMFS4", trades, currentSnapshot: snapshot);

        Assert.False(detail.HasSpread);
        Assert.NotNull(detail.CurrentLiquiditySnapshot);
        Assert.Null(detail.CurrentLiquiditySnapshot.Spread);
        Assert.Null(detail.CurrentLiquiditySnapshot.LiquidityScore);
        Assert.Equal(OfzSpreadSource.Missing, detail.CurrentLiquiditySnapshot.SpreadSource);
        Assert.Equal(OfzLiquidityMetricStatus.MissingQuotes, detail.CurrentLiquiditySnapshot.Status);
        Assert.Equal(OfzLiquidityBucket.MissingData, detail.CurrentLiquiditySnapshot.LiquidityBucket);
    }

    [Fact]
    public void BuildDurationYieldScatter_AddsOptionalLiquidityFieldsWhenAvailable()
    {
        var date = new DateTime(2026, 04, 20);
        OfzActivityMetric[] metrics =
        [
            ScatterMetric("SU26238RMFS4", date, 5, 1_000_000_000, 12.4, 900)
        ];
        OfzLiquidityMetric[] liquidityMetrics =
        [
            new()
            {
                SecId = "SU26238RMFS4",
                TradeDate = date,
                Spread = 0.32,
                SpreadSource = OfzSpreadSource.CalculatedFromBidOffer,
                LiquidityScore = 32,
                LiquidityBucket = OfzLiquidityBucket.Weak,
                Status = OfzLiquidityMetricStatus.Ready
            }
        ];

        var point = Assert.Single(OfzActivityAnalyzer.BuildDurationYieldScatter(metrics, [], liquidityMetrics));

        Assert.Equal(0.32, point.Spread);
        Assert.Equal(32, point.LiquidityScore);
        Assert.Equal(OfzLiquidityBucket.Weak, point.LiquidityBucket);
        Assert.Equal(OfzSpreadSource.CalculatedFromBidOffer, point.SpreadSource);
    }

    [Fact]
    public void BuildSpreadSignals_GeneratesEvidenceAndAvoidsRecommendationLanguage()
    {
        var date = new DateTime(2026, 04, 20);
        OfzLiquidityMetric[] metrics =
        [
            new()
            {
                SecId = "WIDE",
                TradeDate = date,
                Spread = 0.7,
                SpreadSource = OfzSpreadSource.CalculatedFromBidOffer,
                LiquidityScore = 70,
                LiquidityBucket = OfzLiquidityBucket.Problem,
                Status = OfzLiquidityMetricStatus.Ready,
                Value = 1_500_000_000,
                NumTrades = 1_100
            },
            new()
            {
                SecId = "MISSING",
                TradeDate = date,
                SpreadSource = OfzSpreadSource.Missing,
                LiquidityBucket = OfzLiquidityBucket.MissingData,
                Status = OfzLiquidityMetricStatus.MissingQuotes,
                IsSnapshot = true,
                Value = 800_000_000,
                NumTrades = 500
            }
        ];
        OfzIssue[] issues =
        [
            new() { SecId = "WIDE", ShortName = "ОФЗ wide", FaceUnit = "SUR" },
            new() { SecId = "MISSING", ShortName = "ОФЗ missing", FaceUnit = "SUR" }
        ];

        var signals = OfzActivityAnalyzer.BuildSpreadSignals(metrics, issues);

        Assert.Contains(signals, signal => signal.Kind == OfzSpreadSignalKind.ActivityWithWideSpread);
        Assert.Contains(signals, signal => signal.Kind == OfzSpreadSignalKind.MissingQuotesOnActiveDay);
        Assert.All(signals, signal =>
        {
            Assert.NotEqual(default, signal.TradeDate);
            Assert.False(ContainsRecommendationLanguage(signal.Text), signal.Text);
        });
    }

    [Fact]
    public void BuildLiquidityInsights_ReturnsConcreteSpreadSignalText()
    {
        var date = new DateTime(2026, 04, 20);
        OfzLiquidityMetric[] metrics =
        [
            new()
            {
                SecId = "WIDE",
                TradeDate = date,
                Spread = 0.7,
                SpreadSource = OfzSpreadSource.Provided,
                LiquidityScore = 70,
                LiquidityBucket = OfzLiquidityBucket.Problem,
                Status = OfzLiquidityMetricStatus.Ready,
                Value = 1_500_000_000,
                NumTrades = 1_100
            }
        ];

        var insight = Assert.Single(OfzActivityAnalyzer.BuildLiquidityInsights(metrics, [], maxInsights: 1));

        Assert.Equal(OfzActivityInsightKind.LiquiditySignal, insight.Kind);
        Assert.Equal("WIDE", insight.SecId);
        Assert.Contains("spread", insight.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("20.04.2026", insight.Text, StringComparison.Ordinal);
        Assert.False(ContainsRecommendationLanguage(insight.Text), insight.Text);
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
        Assert.False(detail.HasSpread);
        Assert.Collection(
            detail.Points,
            first => Assert.Null(first.Spread),
            second => Assert.Null(second.Spread));
    }

    [Fact]
    public void BuildIssueDetail_MapsHistoricalZSpreadForDetailChart()
    {
        OfzDailyTrade[] trades =
        [
            new()
            {
                SecId = "SU26238RMFS4",
                TradeDate = new DateTime(2026, 04, 20),
                Value = 100_000_000,
                ZSpread = 115.25,
                ZSpreadAtWeightedAveragePrice = 112.5
            }
        ];

        var detail = OfzActivityAnalyzer.BuildIssueDetail("SU26238RMFS4", trades);
        var point = Assert.Single(detail.Points);

        Assert.Equal(115.25, point.ZSpread);
        Assert.Equal(112.5, point.ZSpreadAtWeightedAveragePrice);
        Assert.False(detail.HasSpread);
    }

    [Fact]
    public void BuildIssueDetail_ProjectsStoredClassificationMetadata()
    {
        var loadedAt = new DateTime(2026, 05, 06, 9, 0, 0, DateTimeKind.Utc);
        var issue = new OfzIssue
        {
            SecId = "SU29019RMFS5",
            ShortName = "ОФЗ 29019",
            FaceUnit = "SUR",
            CurrencyId = "SUR",
            BondType = "Флоатер"
        };
        issue.ApplyClassification(
            new OfzIssueClassification
            {
                CouponType = OfzCouponType.Floating,
                CouponTypeMarker = "ОФЗ-ПК",
                Reliability = OfzClassificationReliability.Reliable,
                Source = OfzIssueClassificationSources.History,
                Evidence = "source=history; rules=source-floating",
                IsIndexedNominal = false,
                IsAmortizing = false,
                NominalCurrency = "RUB"
            },
            loadedAt);

        var detail = OfzActivityAnalyzer.BuildIssueDetail(
            "SU29019RMFS5",
            [Trade("SU29019RMFS5", new DateTime(2026, 05, 06), 100_000_000, 13.2)],
            issue);

        Assert.Equal(OfzCouponType.Floating, detail.CouponType);
        Assert.Equal("ОФЗ-ПК", detail.CouponTypeMarker);
        Assert.Equal(OfzClassificationReliability.Reliable, detail.ClassificationReliability);
        Assert.Equal("надежная", detail.ClassificationReliabilityText);
        Assert.Equal(OfzIssueClassificationSources.History, detail.ClassificationSource);
        Assert.Equal("source=history; rules=source-floating", detail.ClassificationEvidence);
        Assert.Equal(loadedAt, detail.ClassificationLoadedAt);
        Assert.Equal("RUB", detail.NominalCurrency);
        Assert.Equal("нет", detail.IndexedNominalText);
        Assert.Equal("нет", detail.AmortizingText);
        Assert.True(detail.HasClassificationEvidence);
    }

    [Fact]
    public void BuildIssueDetail_RecomputesStaleUnknownClassificationSourceForDisplay()
    {
        var issue = new OfzIssue
        {
            SecId = "SU26212RMFS9",
            ShortName = "ОФЗ-ПД 26212",
            FaceUnit = "SUR",
            NormalizedCouponType = OfzCouponType.Fixed,
            NormalizedTypeMarker = "ОФЗ-ПД",
            ClassificationReliability = OfzClassificationReliability.Reliable,
            ClassificationSource = OfzIssueClassificationSources.Unknown,
            ClassificationEvidence = "source=unknown; rules=source-fixed"
        };

        var detail = OfzActivityAnalyzer.BuildIssueDetail(
            "SU26212RMFS9",
            [Trade("SU26212RMFS9", new DateTime(2026, 05, 06), 100_000_000, 13.2)],
            issue);

        Assert.Equal(OfzCouponType.Fixed, detail.CouponType);
        Assert.Equal("ОФЗ-ПД", detail.CouponTypeMarker);
        Assert.Equal(OfzClassificationReliability.Reliable, detail.ClassificationReliability);
        Assert.Equal(OfzIssueClassificationSources.MetadataFields, detail.ClassificationSource);
        Assert.Contains("source=metadata-fields", detail.ClassificationEvidence, StringComparison.Ordinal);
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

    private static OfzDailyTrade LiquidityTrade(
        string secId,
        DateTime date,
        double? value,
        double? bid,
        double? offer,
        double? spread)
    {
        return new OfzDailyTrade
        {
            SecId = secId,
            TradeDate = date,
            Value = value,
            NumTrades = value.HasValue ? 10 : null,
            Bid = bid,
            Offer = offer,
            Spread = spread,
            Duration = 1000
        };
    }

    private static bool ContainsRecommendationLanguage(string text)
    {
        string[] blockedWords = ["купить", "покупать", "продать", "продавать", "buy", "sell"];
        return blockedWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
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
