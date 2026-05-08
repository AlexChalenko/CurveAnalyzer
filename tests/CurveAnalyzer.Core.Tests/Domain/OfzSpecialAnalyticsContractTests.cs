using System.Text.Json;
using System.Text.Json.Serialization;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Domain;

public class OfzSpecialAnalyticsContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void MarketSummary_SerializesSpecialMetricsContractVersion12()
    {
        var tradeDate = new DateTime(2026, 05, 06);
        var summary = new OfzMarketSummary
        {
            StartDate = tradeDate.AddDays(-1),
            EndDate = tradeDate,
            GeneratedAt = new DateTime(2026, 05, 06, 12, 0, 0, DateTimeKind.Utc),
            CouponTypeFilter = OfzCouponType.Floating,
            CouponTypeMarker = "ОФЗ-ПК",
            InsightStartDate = tradeDate.AddDays(-1),
            InsightEndDate = tradeDate,
            SpecialMetrics =
            [
                new OfzSpecialSummaryMetric
                {
                    Kind = OfzSpecialMetricKind.ImpliedFloatingRate,
                    Code = "implied_floating_rate",
                    Label = "Ожидаемая ставка купона",
                    Unit = "%",
                    Value = 13.4,
                    ObservedAt = tradeDate,
                    Source = OfzSpecialMetricSource.History,
                    Availability = OfzSpecialMetricAvailability.Historical,
                    HistoricalPointCount = 2
                }
            ],
            Findings =
            [
                new OfzSummaryFinding
                {
                    Id = "special-floating-metrics",
                    Kind = OfzSummaryFindingKind.SpecialMetric,
                    Priority = 690,
                    Scope = OfzSummaryScope.SpecialMetric,
                    Title = "Спецметрики ОФЗ-ПК",
                    Text = "ОФЗ-ПК: ожидаемая ставка купона 13,40%.",
                    Evidence = new OfzFindingEvidence
                    {
                        TradeDate = tradeDate,
                        CouponType = OfzCouponType.Floating,
                        CouponTypeMarker = "ОФЗ-ПК",
                        SpecialMetricKind = OfzSpecialMetricKind.ImpliedFloatingRate,
                        SpecialMetricCode = "implied_floating_rate",
                        SpecialMetricCount = 1,
                        ImpliedFloatingRate = 13.4,
                        SpecialMetricAvailability = OfzSpecialMetricAvailability.Historical
                    }
                }
            ],
            Limitations =
            [
                new OfzDataLimitation
                {
                    Kind = OfzDataLimitationKind.InsufficientSpecialMetricHistory,
                    Scope = OfzSummaryScope.SpecialMetric,
                    CouponType = OfzCouponType.Floating,
                    Text = "Для части специальных ISS-полей меньше двух исторических наблюдений."
                }
            ],
            SourceCounts = new OfzSummarySourceCounts
            {
                Issues = 1,
                Trades = 2,
                Dates = 2,
                SpecialMetricObservations = 2,
                SpecialMetricSeries = 1
            }
        };

        var json = JsonSerializer.Serialize(summary, JsonOptions);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("1.2", root.GetProperty("schemaVersion").GetString());

        var metric = Assert.Single(root.GetProperty("specialMetrics").EnumerateArray());
        Assert.Equal("ImpliedFloatingRate", metric.GetProperty("kind").GetString());
        Assert.Equal("implied_floating_rate", metric.GetProperty("code").GetString());
        Assert.Equal("Ожидаемая ставка купона", metric.GetProperty("label").GetString());
        Assert.Equal("%", metric.GetProperty("unit").GetString());
        Assert.Equal(13.4, metric.GetProperty("value").GetDouble());
        Assert.Equal("2026-05-06", metric.GetProperty("observedAt").GetString());
        Assert.Equal("History", metric.GetProperty("source").GetString());
        Assert.Equal("Historical", metric.GetProperty("availability").GetString());
        Assert.Equal(2, metric.GetProperty("historicalPointCount").GetInt32());
        Assert.True(metric.GetProperty("hasValue").GetBoolean());

        var evidence = root.GetProperty("findings").EnumerateArray().Single().GetProperty("evidence");
        Assert.Equal("ImpliedFloatingRate", evidence.GetProperty("specialMetricKind").GetString());
        Assert.Equal("implied_floating_rate", evidence.GetProperty("specialMetricCode").GetString());
        Assert.Equal(1, evidence.GetProperty("specialMetricCount").GetInt32());
        Assert.Equal(13.4, evidence.GetProperty("impliedFloatingRate").GetDouble());
        Assert.Equal("Historical", evidence.GetProperty("specialMetricAvailability").GetString());

        var limitation = Assert.Single(root.GetProperty("limitations").EnumerateArray());
        Assert.Equal("InsufficientSpecialMetricHistory", limitation.GetProperty("kind").GetString());
        Assert.Equal("SpecialMetric", limitation.GetProperty("scope").GetString());

        var sourceCounts = root.GetProperty("sourceCounts");
        Assert.Equal(2, sourceCounts.GetProperty("specialMetricObservations").GetInt32());
        Assert.Equal(1, sourceCounts.GetProperty("specialMetricSeries").GetInt32());
    }
}
