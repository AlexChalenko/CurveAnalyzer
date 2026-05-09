using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Services;

public class OfzExternalFactorsContextBuilderTests
{
    [Fact]
    public void Build_UsesLatestCbrRateOnOrBeforeActivityDateWithoutFutureLookup()
    {
        var tradeDate = new DateTime(2026, 05, 05);
        var context = OfzExternalFactorsContextBuilder.Build(
            [ActivityMetric("SU26238RMFS4", tradeDate, 2_000_000, 8, yieldMove: 0.15)],
            [
                new CbrKeyRate { Date = tradeDate.AddDays(-4), Rate = 14.50, LoadedAt = tradeDate },
                new CbrKeyRate { Date = tradeDate.AddDays(1), Rate = 15.00, LoadedAt = tradeDate.AddDays(1) }
            ],
            [],
            [],
            new OfzExternalFactorsContextOptions
            {
                StartDate = tradeDate,
                EndDate = tradeDate,
                InsightStartDate = tradeDate,
                InsightEndDate = tradeDate
            });

        var cbrSeries = Assert.Single(context.FactorSeries, series => series.Code == "cbr_key_rate");
        var link = Assert.Single(context.Links, item => item.FactorCode == "cbr_key_rate");

        Assert.Equal(OfzExternalFactorAvailability.Historical, cbrSeries.Availability);
        Assert.Equal(tradeDate.AddDays(-4), link.FactorObservationDate);
        Assert.Equal(14.50, cbrSeries.LatestObservation?.Value);
        Assert.DoesNotContain(cbrSeries.Observations, observation => observation.TradeDate == tradeDate.AddDays(1));
    }

    [Fact]
    public void Build_RepresentsMissingFactorsAsLimitationsWithoutZeroValues()
    {
        var tradeDate = new DateTime(2026, 05, 05);
        var context = OfzExternalFactorsContextBuilder.Build(
            [ActivityMetric("SU26238RMFS4", tradeDate, 2_000_000, 8)],
            [],
            [],
            []);

        var cbrSeries = Assert.Single(context.FactorSeries, series => series.Code == "cbr_key_rate");
        var missingLink = Assert.Single(context.Links, link => link.LinkKind == OfzExternalFactorLinkKind.MissingFactor);

        Assert.Equal(OfzExternalFactorAvailability.Missing, cbrSeries.Availability);
        Assert.Empty(cbrSeries.Observations);
        Assert.Null(cbrSeries.LatestObservation);
        Assert.Equal(0, context.ObservationCount);
        Assert.Contains(context.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.MissingExternalFactor);
        Assert.Equal("cbr_key_rate", missingLink.FactorCode);
    }

    [Fact]
    public void Build_PropagatesSnapshotAndProvisionalIndexStatus()
    {
        var tradeDate = new DateTime(2026, 05, 08);
        var indexDay = new OfzIndexContextDay
        {
            TradeDate = tradeDate,
            Points =
            [
                new OfzIndexContextPoint
                {
                    SecId = "RGBI",
                    DisplayName = "RGBI",
                    Role = OfzMarketIndexRole.WholeMarket,
                    ReturnKind = OfzMarketIndexReturnKind.Price,
                    DurationBucket = OfzIndexDurationBucket.All,
                    TradeDate = tradeDate,
                    Close = 119.29,
                    DailyChangePercent = -0.003,
                    Direction = OfzMarketIndexDirection.Down,
                    IsMeaningful = true,
                    SourceKind = OfzMarketIndexSourceKind.Snapshot,
                    IsProvisional = true,
                    Limitations =
                    [
                        new OfzDataLimitation
                        {
                            Kind = OfzDataLimitationKind.Provisional,
                            Scope = OfzSummaryScope.IndexContext,
                            Text = "RGBI: индексное значение предварительное.",
                            SecId = "RGBI",
                            TradeDate = tradeDate
                        }
                    ]
                }
            ]
        };

        var context = OfzExternalFactorsContextBuilder.Build(
            [ActivityMetric("SU26238RMFS4", tradeDate, 2_000_000, 8)],
            [new CbrKeyRate { Date = tradeDate.AddDays(-1), Rate = 14.50 }],
            [indexDay],
            []);

        var rgbi = Assert.Single(context.FactorSeries, series => series.Code == "RGBI");
        var observation = Assert.Single(rgbi.Observations);
        var link = Assert.Single(context.Links, item => item.FactorCode == "RGBI");

        Assert.Equal(OfzExternalFactorAvailability.Provisional, rgbi.Availability);
        Assert.True(observation.IsSnapshot);
        Assert.True(observation.IsProvisional);
        Assert.True(observation.IsMeaningful);
        Assert.Equal(OfzExternalFactorLinkKind.ActivityWithFactorMove, link.LinkKind);
        Assert.Contains(rgbi.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.Provisional);
    }

    private static OfzActivityMetric ActivityMetric(
        string secId,
        DateTime tradeDate,
        double value,
        int numTrades,
        double? yieldMove = null)
    {
        return new OfzActivityMetric
        {
            SecId = secId,
            TradeDate = tradeDate,
            Value = value,
            NumTrades = numTrades,
            ActivityScore = value / 1_000_000,
            YieldMove = yieldMove,
            YieldValue = yieldMove.HasValue ? 12 + yieldMove.Value : null,
            PreviousYieldValue = yieldMove.HasValue ? 12 : null,
            BaselineMedianValue = 1_000_000,
            BaselineDays = 10,
            Status = yieldMove.HasValue ? OfzActivityMetricStatus.Ready : OfzActivityMetricStatus.MissingYield
        };
    }
}
