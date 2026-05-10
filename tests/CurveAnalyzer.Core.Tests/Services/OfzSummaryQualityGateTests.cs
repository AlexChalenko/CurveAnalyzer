using System.Text.Json;
using System.Text.Json.Serialization;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Services;

public class OfzSummaryQualityGateTests
{
    private static readonly DateTime StartDate = new(2026, 04, 06);
    private static readonly DateTime EndDate = new(2026, 05, 04);

    [Fact]
    public void FullSummary_ContainsRequiredSectionsAndSchemaVersion16()
    {
        var summary = RepresentativeSummary();
        using var document = Serialize(summary);
        var root = document.RootElement;

        Assert.Equal(OfzMarketSummary.CurrentSchemaVersion, root.GetProperty("schemaVersion").GetString());
        Assert.Equal("1.6", root.GetProperty("schemaVersion").GetString());
        AssertSection(root, "findings");
        AssertSection(root, "segments");
        AssertSection(root, "breadthDays");
        AssertSection(root, "indexContextDays");
        AssertSection(root, "indexSegments");
        AssertSection(root, "cashflowContext");
        AssertSection(root, "seasonalityContext");
        AssertSection(root, "externalFactorsContext");
        AssertSection(root, "specialMetrics");
        AssertSection(root, "limitations");
        AssertSection(root, "sourceCounts");

        Assert.NotNull(summary.CashflowContext);
        Assert.True(summary.CashflowContext.HasEvents, "cashflowContext must keep period/upcoming events.");
        Assert.NotNull(summary.SeasonalityContext);
        Assert.True(summary.SeasonalityContext.ObservationCount > 0, "seasonalityContext must keep observations.");
        Assert.NotNull(summary.ExternalFactorsContext);
        Assert.True(summary.ExternalFactorsContext.FactorSeries.Count > 0, "externalFactorsContext must keep factor series.");
        Assert.True(summary.ExternalFactorsContext.Links.Count > 0, "externalFactorsContext must keep activity links.");
        Assert.True(summary.SpecialMetrics.Count > 0, "specialMetrics must be present for floating OFZ fixture.");
    }

    [Fact]
    public void FullSummary_SourceCountsMatchRepresentativeInput()
    {
        var summary = RepresentativeSummary();
        var counts = summary.SourceCounts;

        Assert.Equal(1, counts.Issues);
        Assert.Equal(5, counts.Trades);
        Assert.Equal(5, counts.ActivityMetrics);
        Assert.Equal(4, counts.LiquidityMetrics);
        Assert.Equal(1, counts.SnapshotLiquidityMetrics);
        Assert.Equal(15, counts.SpecialMetricObservations);
        Assert.Equal(3, counts.SpecialMetricSeries);
        Assert.Equal(4, counts.IndexPoints);
        Assert.Equal(5, counts.IndexContextDays);
        Assert.Equal(1, counts.CashflowEvents);
        Assert.Equal(1, counts.CashflowIssues);
        Assert.Equal(5, counts.SeasonalityObservations);
        Assert.True(counts.ExternalFactorObservations > 0, "external factor observations must be counted.");
        Assert.True(counts.ExternalFactorSeries > 0, "external factor value-bearing series must be counted.");
        Assert.True(counts.ExternalFactorLinks > 0, "external factor activity links must be counted.");
        Assert.True(counts.Dates >= 5, "distinct activity/index dates must be counted.");
    }

    [Fact]
    public void FullSummary_SerializesTradeAndEventDatesAsDateOnly()
    {
        using var document = Serialize(RepresentativeSummary());
        var root = document.RootElement;

        AssertJsonDateOnly(root.GetProperty("startDate"));
        AssertJsonDateOnly(root.GetProperty("endDate"));
        AssertJsonDateOnly(root.GetProperty("insightStartDate"));
        AssertJsonDateOnly(root.GetProperty("insightEndDate"));

        foreach (var day in root.GetProperty("breadthDays").EnumerateArray())
        {
            AssertJsonDateOnly(day.GetProperty("tradeDate"));
            AssertDateOnlyForArray(day.GetProperty("topContributors"), "tradeDate");
        }

        foreach (var day in root.GetProperty("indexContextDays").EnumerateArray())
        {
            AssertJsonDateOnly(day.GetProperty("tradeDate"));
            foreach (var point in day.GetProperty("points").EnumerateArray())
            {
                AssertJsonDateOnly(point.GetProperty("tradeDate"));
                AssertJsonDateOnlyIfPresent(point, "previousTradeDate");
            }
        }

        var cashflow = root.GetProperty("cashflowContext");
        AssertJsonDateOnly(cashflow.GetProperty("startDate"));
        AssertJsonDateOnly(cashflow.GetProperty("endDate"));
        AssertDateOnlyForArray(cashflow.GetProperty("events"), "eventDate");
        AssertDateOnlyForArray(cashflow.GetProperty("upcomingEvents"), "eventDate");

        var factors = root.GetProperty("externalFactorsContext");
        AssertJsonDateOnly(factors.GetProperty("startDate"));
        AssertJsonDateOnly(factors.GetProperty("endDate"));
        AssertJsonDateOnly(factors.GetProperty("insightStartDate"));
        AssertJsonDateOnly(factors.GetProperty("insightEndDate"));
        foreach (var series in factors.GetProperty("factorSeries").EnumerateArray())
        {
            AssertDateOnlyForArray(series.GetProperty("observations"), "tradeDate");
            if (series.GetProperty("latestObservation").ValueKind == JsonValueKind.Object)
            {
                AssertJsonDateOnly(series.GetProperty("latestObservation").GetProperty("tradeDate"));
            }
        }
        AssertDateOnlyForArray(factors.GetProperty("links"), "tradeDate");
        AssertDateOnlyForArray(factors.GetProperty("links"), "factorObservationDate");

        foreach (var metric in root.GetProperty("specialMetrics").EnumerateArray())
        {
            AssertJsonDateOnlyIfPresent(metric, "observedAt");
        }

        foreach (var finding in root.GetProperty("findings").EnumerateArray())
        {
            var evidence = finding.GetProperty("evidence");
            AssertJsonDateOnlyIfPresent(evidence, "tradeDate");
            AssertJsonDateOnlyIfPresent(evidence, "indexPreviousTradeDate");
            AssertJsonDateOnlyIfPresent(evidence, "cashflowEventDate");
            AssertJsonDateOnlyIfPresent(evidence, "externalFactorObservationDate");
        }
    }

    [Fact]
    public void FullSummary_KeepsExternalFactorFindingUnderDefaultLimit()
    {
        var summary = RepresentativeSummary();

        Assert.Equal(new OfzMarketSummaryOptions().MaxFindings, summary.Findings.Count);
        Assert.NotNull(summary.ExternalFactorsContext);
        Assert.True(summary.ExternalFactorsContext.Links.Count > 0, "fixture must include external factor links.");
        Assert.Contains(summary.Findings, finding =>
            finding.Kind == OfzSummaryFindingKind.ExternalFactorActivity &&
            finding.Scope == OfzSummaryScope.ExternalFactors &&
            finding.DrillDown?.Target == OfzSummaryDrillDownTarget.ExternalFactors);
    }

    [Fact]
    public void FullSummary_FindingsAndLimitationsAvoidRecommendationLanguage()
    {
        var summary = RepresentativeSummary();
        var texts = summary.Findings
            .SelectMany(finding => new[] { finding.Title, finding.Text }
                .Concat(finding.Limitations.Select(limitation => limitation.Text)))
            .Concat(summary.Limitations.Select(limitation => limitation.Text))
            .Concat(summary.CashflowContext?.Limitations.Select(limitation => limitation.Text) ?? [])
            .Concat(summary.SeasonalityContext?.Limitations.Select(limitation => limitation.Text) ?? [])
            .Concat(summary.ExternalFactorsContext?.Limitations.Select(limitation => limitation.Text) ?? []);

        foreach (var text in texts)
        {
            Assert.False(ContainsBlockedWording(text), text);
        }
    }

    [Fact]
    public void MissingData_DoesNotSerializeFakeZeroAndKeepsLimitations()
    {
        var summary = MissingDataSummary();
        using var document = Serialize(summary);
        var root = document.RootElement;

        Assert.All(summary.SpecialMetrics, metric =>
        {
            Assert.Null(metric.Value);
            Assert.Equal(OfzSpecialMetricAvailability.Missing, metric.Availability);
        });
        Assert.Contains(summary.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.MissingSpecialMetric);

        foreach (var metric in root.GetProperty("specialMetrics").EnumerateArray())
        {
            Assert.Equal(JsonValueKind.Null, metric.GetProperty("value").ValueKind);
            Assert.Equal("Missing", metric.GetProperty("availability").GetString());
        }

        var factors = root.GetProperty("externalFactorsContext");
        var cbrSeries = factors.GetProperty("factorSeries")
            .EnumerateArray()
            .Single(series => series.GetProperty("code").GetString() == "cbr_key_rate");
        Assert.Equal("Missing", cbrSeries.GetProperty("availability").GetString());
        Assert.Equal(JsonValueKind.Null, cbrSeries.GetProperty("latestObservation").ValueKind);
        Assert.Contains(summary.ExternalFactorsContext!.Limitations, limitation =>
            limitation.Kind == OfzDataLimitationKind.MissingExternalFactor);

        var provisionalSeries = factors.GetProperty("factorSeries")
            .EnumerateArray()
            .Single(series => series.GetProperty("code").GetString() == "RGBI");
        Assert.Equal("Provisional", provisionalSeries.GetProperty("availability").GetString());
        Assert.True(provisionalSeries.GetProperty("latestObservation").GetProperty("isProvisional").GetBoolean());
        Assert.True(provisionalSeries.GetProperty("latestObservation").GetProperty("isSnapshot").GetBoolean());
        Assert.Contains(summary.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.Provisional);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    private static OfzMarketSummary RepresentativeSummary()
    {
        var issue = Issue("SU29019RMFS0", "ОФЗ 29019", "ОФЗ-ПК");
        var activityDates = Enumerable.Range(0, 5)
            .Select(index => StartDate.AddDays(index * 7))
            .ToArray();

        var input = new OfzMarketSummaryInput
        {
            StartDate = StartDate,
            EndDate = EndDate,
            InsightStartDate = StartDate,
            InsightEndDate = EndDate,
            CouponTypeFilter = OfzCouponType.Floating,
            Issues = [issue],
            Trades =
            [
                Trade(issue.SecId, activityDates[0], 100_000_000, 100, 13.00, 650, impliedFloatingRate: 12.80),
                Trade(issue.SecId, activityDates[1], 110_000_000, 110, 13.05, 645, impliedFloatingRate: 12.90),
                Trade(issue.SecId, activityDates[2], 95_000_000, 95, 13.10, 642, impliedFloatingRate: 13.00),
                Trade(issue.SecId, activityDates[3], 100_000_000, 100, 13.20, 638, impliedFloatingRate: 13.10),
                Trade(issue.SecId, activityDates[4], 260_000_000, 260, 13.45, 630, impliedFloatingRate: 13.40)
            ],
            ActivityMetrics =
            [
                ActivityMetric(issue.SecId, activityDates[0], 1.0, 100_000_000, 100, yieldMove: 0.01),
                ActivityMetric(issue.SecId, activityDates[1], 1.1, 110_000_000, 110, yieldMove: 0.02),
                ActivityMetric(issue.SecId, activityDates[2], 0.95, 95_000_000, 95, yieldMove: -0.01),
                ActivityMetric(issue.SecId, activityDates[3], 1.0, 100_000_000, 100, yieldMove: 0.03),
                ActivityMetric(issue.SecId, activityDates[4], 4.0, 260_000_000, 260, yieldMove: -0.35)
            ],
            LiquidityMetrics =
            [
                LiquidityMetric(issue.SecId, activityDates[0], 0.10, OfzSpreadSource.Provided, OfzLiquidityBucket.Good, OfzLiquidityMetricStatus.Ready, 100_000_000, 100),
                LiquidityMetric(issue.SecId, activityDates[1], 0.12, OfzSpreadSource.Provided, OfzLiquidityBucket.Good, OfzLiquidityMetricStatus.Ready, 110_000_000, 110),
                LiquidityMetric(issue.SecId, activityDates[2], 0.11, OfzSpreadSource.Provided, OfzLiquidityBucket.Good, OfzLiquidityMetricStatus.Ready, 95_000_000, 95),
                LiquidityMetric(issue.SecId, activityDates[3], 0.13, OfzSpreadSource.Provided, OfzLiquidityBucket.Good, OfzLiquidityMetricStatus.Ready, 100_000_000, 100),
                LiquidityMetric(issue.SecId, activityDates[4], 0.72, OfzSpreadSource.CalculatedFromBidOffer, OfzLiquidityBucket.Problem, OfzLiquidityMetricStatus.Ready, 260_000_000, 260, isSnapshot: true, isProvisional: true)
            ],
            CbrKeyRates =
            [
                new CbrKeyRate { Date = activityDates[0], Rate = 7.50 },
                new CbrKeyRate { Date = activityDates[4], Rate = 7.75 }
            ],
            IndexPoints =
            [
                IndexPoint("RGBI", activityDates[3], 100.00, yield: 14.1),
                IndexPoint("RGBI", activityDates[4], 100.50, yield: 14.0),
                IndexPoint("RGBITR", activityDates[3], 200.00, yield: 14.1),
                IndexPoint("RGBITR", activityDates[4], 201.00, yield: 14.0)
            ],
            CashflowDataLoaded = true,
            CashflowEvents =
            [
                CashflowEvent(issue.SecId, OfzCashflowEventType.Coupon, EndDate.AddDays(1), value: 12.34, valueRub: 12.34, valuePercent: 7.1)
            ]
        };

        return BuildMarketSummary(input);
    }

    private static OfzMarketSummary MissingDataSummary()
    {
        var issue = Issue("SU29019RMFS0", "ОФЗ 29019", "ОФЗ-ПК");
        var previousDate = new DateTime(2026, 05, 03);
        var currentDate = new DateTime(2026, 05, 04);

        return BuildMarketSummary(new OfzMarketSummaryInput
        {
            StartDate = previousDate,
            EndDate = currentDate,
            InsightStartDate = currentDate,
            InsightEndDate = currentDate,
            CouponTypeFilter = OfzCouponType.Floating,
            Issues = [issue],
            Trades =
            [
                Trade(issue.SecId, previousDate, 90_000_000, 90, 13.00, 650),
                Trade(issue.SecId, currentDate, 260_000_000, 260, 13.45, 630)
            ],
            ActivityMetrics =
            [
                ActivityMetric(issue.SecId, currentDate, 4.0, 260_000_000, 260, yieldMove: -0.35)
            ],
            LiquidityMetrics =
            [
                LiquidityMetric(issue.SecId, currentDate, null, OfzSpreadSource.Missing, OfzLiquidityBucket.MissingData, OfzLiquidityMetricStatus.MissingQuotes, 260_000_000, 260, isSnapshot: true, isProvisional: true)
            ],
            IndexPoints =
            [
                IndexPoint("RGBI", previousDate, 100.00, sourceKind: OfzMarketIndexSourceKind.Snapshot, isProvisional: true),
                IndexPoint("RGBI", currentDate, 100.10, sourceKind: OfzMarketIndexSourceKind.Snapshot, isProvisional: true)
            ]
        }, new OfzMarketSummaryOptions { MaxFindings = 10 });
    }

    private static OfzMarketSummary BuildMarketSummary(
        OfzMarketSummaryInput input,
        OfzMarketSummaryOptions? options = null)
    {
        return OfzMarketSummaryBuilder.Build(input, options);
    }

    private static OfzIssue Issue(
        string secId,
        string shortName,
        string bondType)
    {
        return new OfzIssue
        {
            SecId = secId,
            ShortName = shortName,
            SecName = shortName,
            BondType = bondType,
            FaceUnit = "SUR",
            CurrencyId = "SUR"
        };
    }

    private static OfzDailyTrade Trade(
        string secId,
        DateTime tradeDate,
        double value,
        int numTrades,
        double yield,
        double duration,
        double? impliedFloatingRate = null)
    {
        return new OfzDailyTrade
        {
            SecId = secId,
            TradeDate = tradeDate.Date,
            Value = value,
            NumTrades = numTrades,
            YieldAtWeightedAveragePrice = yield,
            Duration = duration,
            ImpliedFloatingRate = impliedFloatingRate
        };
    }

    private static OfzActivityMetric ActivityMetric(
        string secId,
        DateTime tradeDate,
        double activityScore,
        double value,
        int numTrades,
        double? yieldMove)
    {
        return new OfzActivityMetric
        {
            SecId = secId,
            TradeDate = tradeDate.Date,
            Value = value,
            ActivityScore = activityScore,
            BaselineMedianValue = 100_000_000,
            BaselineDays = 10,
            NumTrades = numTrades,
            YieldValue = 13.45,
            PreviousYieldValue = yieldMove.HasValue ? 13.45 - yieldMove.Value : null,
            YieldMove = yieldMove,
            Duration = 630,
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
        bool isProvisional = false)
    {
        return new OfzLiquidityMetric
        {
            SecId = secId,
            TradeDate = tradeDate.Date,
            Spread = spread,
            SpreadSource = spreadSource,
            LiquidityScore = spread.HasValue ? spread.Value * 100 : null,
            LiquidityBucket = bucket,
            Status = status,
            Value = value,
            NumTrades = numTrades,
            IsSnapshot = isSnapshot,
            IsProvisional = isProvisional,
            ObservedAt = tradeDate.Date.AddHours(12)
        };
    }

    private static OfzMarketIndexPoint IndexPoint(
        string secId,
        DateTime tradeDate,
        double? close,
        double? yield = null,
        OfzMarketIndexSourceKind sourceKind = OfzMarketIndexSourceKind.History,
        bool isProvisional = false)
    {
        return new OfzMarketIndexPoint
        {
            SecId = secId,
            ShortName = secId,
            TradeDate = tradeDate.Date,
            Close = close,
            Yield = yield,
            SourceKind = sourceKind,
            IsProvisional = isProvisional,
            LoadedAt = new DateTime(2026, 05, 04, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    private static OfzCashflowEvent CashflowEvent(
        string secId,
        OfzCashflowEventType eventType,
        DateTime eventDate,
        double? value,
        double? valueRub,
        double? valuePercent)
    {
        return new OfzCashflowEvent
        {
            SecId = secId,
            SourceKey = $"{eventType}:{eventDate:yyyy-MM-dd}",
            ShortName = "ОФЗ 29019",
            EventType = eventType,
            EventDate = eventDate.Date,
            Value = value,
            ValueRub = valueRub,
            ValuePercent = valuePercent,
            SourceKind = OfzCashflowSourceKind.Schedule,
            LoadedAt = new DateTime(2026, 05, 01, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    private static JsonDocument Serialize(OfzMarketSummary summary)
    {
        return JsonDocument.Parse(JsonSerializer.Serialize(summary, JsonOptions));
    }

    private static JsonElement AssertSection(JsonElement root, string name)
    {
        Assert.True(root.TryGetProperty(name, out var section), $"Missing required summary JSON section '{name}'.");
        Assert.NotEqual(JsonValueKind.Undefined, section.ValueKind);
        return section;
    }

    private static void AssertDateOnlyForArray(JsonElement array, string propertyName)
    {
        foreach (var item in array.EnumerateArray())
        {
            AssertJsonDateOnlyIfPresent(item, propertyName);
        }
    }

    private static void AssertJsonDateOnlyIfPresent(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        AssertJsonDateOnly(value);
    }

    private static void AssertJsonDateOnly(JsonElement element)
    {
        var value = element.GetString();
        Assert.NotNull(value);
        Assert.Equal(10, value.Length);
        Assert.Equal('-', value[4]);
        Assert.Equal('-', value[7]);
        Assert.DoesNotContain("T", value);
    }

    private static bool ContainsBlockedWording(string text)
    {
        string[] blockedWords =
        [
            "купить",
            "покупать",
            "продать",
            "продавать",
            "держать",
            "рекоменд",
            "выгод",
            "из-за",
            "вызвал",
            "вызвала",
            "привел",
            "привела",
            "buy",
            "sell",
            "recommend",
            "caused"
        ];

        return blockedWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
