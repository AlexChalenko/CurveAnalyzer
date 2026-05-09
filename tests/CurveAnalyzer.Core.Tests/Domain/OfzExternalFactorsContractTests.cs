using System.Text.Json;
using System.Text.Json.Serialization;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Domain;

public class OfzExternalFactorsContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void MarketSummary_SerializesExternalFactorsContractVersion16()
    {
        var tradeDate = new DateTime(2026, 05, 08);
        var summary = new OfzMarketSummary
        {
            StartDate = tradeDate.AddDays(-1),
            EndDate = tradeDate,
            InsightStartDate = tradeDate,
            InsightEndDate = tradeDate,
            GeneratedAt = new DateTime(2026, 05, 08, 12, 0, 0, DateTimeKind.Utc),
            ExternalFactorsContext = new OfzExternalFactorsContext
            {
                StartDate = tradeDate.AddDays(-1),
                EndDate = tradeDate,
                InsightStartDate = tradeDate,
                InsightEndDate = tradeDate,
                ObservationCount = 1,
                FactorSeries =
                [
                    new OfzExternalFactorSeries
                    {
                        Kind = OfzExternalFactorKind.MarketIndex,
                        Code = "RGBI",
                        Label = "Индекс Мосбиржи гос облигаций RGBI",
                        Unit = "index",
                        Scope = OfzExternalFactorScope.Market,
                        Source = OfzExternalFactorSource.MoexIndex,
                        Availability = OfzExternalFactorAvailability.Provisional,
                        Observations =
                        [
                            new OfzExternalFactorObservation
                            {
                                TradeDate = tradeDate,
                                Value = 119.29,
                                DailyChange = -0.14,
                                DailyChangePercent = -0.001172,
                                Direction = OfzMarketIndexDirection.Flat,
                                PreviousTradeDate = tradeDate.AddDays(-1),
                                SourceLabel = "snapshot",
                                IsSnapshot = true,
                                IsProvisional = true
                            }
                        ],
                        LatestObservation = new OfzExternalFactorObservation
                        {
                            TradeDate = tradeDate,
                            Value = 119.29,
                            SourceLabel = "snapshot",
                            IsSnapshot = true,
                            IsProvisional = true
                        },
                        Limitations =
                        [
                            new OfzDataLimitation
                            {
                                Kind = OfzDataLimitationKind.Provisional,
                                Scope = OfzSummaryScope.ExternalFactors,
                                Text = "RGBI: факторное значение предварительное.",
                                SecId = "RGBI",
                                TradeDate = tradeDate
                            }
                        ]
                    }
                ],
                Links =
                [
                    new OfzExternalFactorActivityLink
                    {
                        TradeDate = tradeDate,
                        FactorCode = "RGBI",
                        FactorObservationDate = tradeDate,
                        LinkKind = OfzExternalFactorLinkKind.ActivityWithoutFactorMove,
                        ActivityValue = 4_394_790,
                        ActivityScore = 8,
                        YieldMove = 0.02,
                        Text = "08.05.2026: активность прошла на фоне спокойного RGBI."
                    }
                ],
                Limitations =
                [
                    new OfzDataLimitation
                    {
                        Kind = OfzDataLimitationKind.Provisional,
                        Scope = OfzSummaryScope.ExternalFactors,
                        Text = "Часть factor context предварительная.",
                        TradeDate = tradeDate
                    }
                ]
            },
            Findings =
            [
                new OfzSummaryFinding
                {
                    Id = "external-factor-activitywithoutfactormove-rgbi-2026-05-08",
                    Kind = OfzSummaryFindingKind.ExternalFactorActivity,
                    Priority = 660,
                    Scope = OfzSummaryScope.ExternalFactors,
                    Title = "Локальная активность относительно фактора",
                    Text = "08.05.2026: активность прошла на фоне спокойного RGBI.",
                    Evidence = new OfzFindingEvidence
                    {
                        TradeDate = tradeDate,
                        ExternalFactorCode = "RGBI",
                        ExternalFactorKind = OfzExternalFactorKind.MarketIndex,
                        ExternalFactorSource = OfzExternalFactorSource.MoexIndex,
                        ExternalFactorLinkKind = OfzExternalFactorLinkKind.ActivityWithoutFactorMove,
                        ExternalFactorObservationDate = tradeDate,
                        ExternalFactorValue = 119.29,
                        ExternalFactorDailyChangePercent = -0.001172,
                        ExternalFactorDirection = OfzMarketIndexDirection.Flat,
                        ExternalFactorMoveIsMeaningful = false,
                        IsSnapshot = true,
                        IsProvisional = true
                    },
                    DrillDown = new OfzSummaryDrillDown
                    {
                        Target = OfzSummaryDrillDownTarget.ExternalFactors,
                        TradeDate = tradeDate
                    }
                }
            ],
            SourceCounts = new OfzSummarySourceCounts
            {
                ExternalFactorObservations = 1,
                ExternalFactorSeries = 1,
                ExternalFactorLinks = 1
            }
        };

        var json = JsonSerializer.Serialize(summary, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("1.6", root.GetProperty("schemaVersion").GetString());
        var context = root.GetProperty("externalFactorsContext");
        Assert.Equal(1, context.GetProperty("observationCount").GetInt32());
        Assert.Equal("2026-05-08", context.GetProperty("insightEndDate").GetString());

        var series = Assert.Single(context.GetProperty("factorSeries").EnumerateArray());
        Assert.Equal("MarketIndex", series.GetProperty("kind").GetString());
        Assert.Equal("RGBI", series.GetProperty("code").GetString());
        Assert.Equal("MoexIndex", series.GetProperty("source").GetString());
        Assert.Equal("Provisional", series.GetProperty("availability").GetString());
        Assert.Equal("2026-05-08", series.GetProperty("latestObservation").GetProperty("tradeDate").GetString());
        Assert.True(series.GetProperty("latestObservation").GetProperty("isSnapshot").GetBoolean());

        var link = Assert.Single(context.GetProperty("links").EnumerateArray());
        Assert.Equal("ActivityWithoutFactorMove", link.GetProperty("linkKind").GetString());
        Assert.Equal("2026-05-08", link.GetProperty("factorObservationDate").GetString());

        var evidence = root.GetProperty("findings").EnumerateArray().Single().GetProperty("evidence");
        Assert.Equal("RGBI", evidence.GetProperty("externalFactorCode").GetString());
        Assert.Equal("MarketIndex", evidence.GetProperty("externalFactorKind").GetString());
        Assert.Equal("MoexIndex", evidence.GetProperty("externalFactorSource").GetString());
        Assert.Equal("ActivityWithoutFactorMove", evidence.GetProperty("externalFactorLinkKind").GetString());
        Assert.Equal("Flat", evidence.GetProperty("externalFactorDirection").GetString());
        Assert.False(evidence.GetProperty("externalFactorMoveIsMeaningful").GetBoolean());

        var sourceCounts = root.GetProperty("sourceCounts");
        Assert.Equal(1, sourceCounts.GetProperty("externalFactorObservations").GetInt32());
        Assert.Equal(1, sourceCounts.GetProperty("externalFactorSeries").GetInt32());
        Assert.Equal(1, sourceCounts.GetProperty("externalFactorLinks").GetInt32());
    }
}
