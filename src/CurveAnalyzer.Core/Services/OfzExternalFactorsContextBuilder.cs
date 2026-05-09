namespace CurveAnalyzer.Core;

public sealed class OfzExternalFactorsContextOptions
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime? InsightStartDate { get; init; }
    public DateTime? InsightEndDate { get; init; }
    public OfzSummarySignalScope SignalScope { get; init; } = OfzSummarySignalScope.AllDays;
    public double MeaningfulPolicyRateChange { get; init; } = 0.25;
}

public static class OfzExternalFactorsContextBuilder
{
    private const string CbrKeyRateCode = "cbr_key_rate";

    public static OfzExternalFactorsContext Build(
        IEnumerable<OfzActivityMetric> metrics,
        IEnumerable<CbrKeyRate> cbrKeyRates,
        IEnumerable<OfzIndexContextDay> indexContextDays,
        IEnumerable<OfzSpecialSummaryMetric> specialMetrics,
        OfzExternalFactorsContextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(cbrKeyRates);
        ArgumentNullException.ThrowIfNull(indexContextDays);
        ArgumentNullException.ThrowIfNull(specialMetrics);
        options ??= new OfzExternalFactorsContextOptions();
        Validate(options);

        var metricList = metrics
            .Where(HasActivityEvidence)
            .OrderBy(metric => metric.TradeDate)
            .ToList();
        var indexDays = indexContextDays
            .OrderBy(day => day.TradeDate)
            .ToList();
        var keyRates = cbrKeyRates
            .Where(rate => double.IsFinite(rate.Rate))
            .GroupBy(rate => rate.Date.Date)
            .Select(group => group.OrderByDescending(rate => rate.LoadedAt).First())
            .OrderBy(rate => rate.Date)
            .ToList();
        var startDate = options.StartDate?.Date ??
            metricList.Select(metric => metric.TradeDate.Date)
                .Concat(indexDays.Select(day => day.TradeDate.Date))
                .Concat(keyRates.Select(rate => rate.Date.Date))
                .DefaultIfEmpty(DateTime.MinValue)
                .Min();
        var endDate = options.EndDate?.Date ??
            metricList.Select(metric => metric.TradeDate.Date)
                .Concat(indexDays.Select(day => day.TradeDate.Date))
                .Concat(keyRates.Select(rate => rate.Date.Date))
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();
        var insightStartDate = options.InsightStartDate?.Date ?? startDate;
        var insightEndDate = options.InsightEndDate?.Date ?? endDate;
        if (insightStartDate > insightEndDate)
        {
            (insightStartDate, insightEndDate) = (insightEndDate, insightStartDate);
        }

        var cbrSeries = BuildCbrKeyRateSeries(keyRates, metricList, startDate, endDate, options);
        var indexSeries = BuildIndexSeries(indexDays).ToList();
        var specialSeries = BuildSpecialMetricSeries(specialMetrics).ToList();
        var series = new[] { cbrSeries }
            .Concat(indexSeries)
            .Concat(specialSeries)
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
        var links = BuildLinks(metricList, cbrSeries, indexSeries, insightStartDate, insightEndDate, options).ToList();
        var limitations = BuildLimitations(cbrSeries, indexSeries, specialSeries).ToList();

        return new OfzExternalFactorsContext
        {
            StartDate = startDate,
            EndDate = endDate,
            InsightStartDate = insightStartDate,
            InsightEndDate = insightEndDate,
            ObservationCount = series.Sum(item => item.Observations.Count(observation => observation.Value.HasValue)),
            MissingFactorCount = series.Count(item => item.Availability == OfzExternalFactorAvailability.Missing),
            FactorSeries = series,
            Links = links,
            Limitations = limitations
        };
    }

    private static OfzExternalFactorSeries BuildCbrKeyRateSeries(
        IReadOnlyList<CbrKeyRate> keyRates,
        IReadOnlyCollection<OfzActivityMetric> metrics,
        DateTime startDate,
        DateTime endDate,
        OfzExternalFactorsContextOptions options)
    {
        if (keyRates.Count == 0)
        {
            var missingKeyRateLimitation = CreateLimitation(
                OfzDataLimitationKind.MissingExternalFactor,
                "Ключевая ставка ЦБ недоступна для факторного контекста.",
                CbrKeyRateCode,
                null);

            return new OfzExternalFactorSeries
            {
                Kind = OfzExternalFactorKind.PolicyRate,
                Code = CbrKeyRateCode,
                Label = "Ключевая ставка ЦБ",
                Unit = "%",
                Scope = OfzExternalFactorScope.Market,
                Source = OfzExternalFactorSource.CbrKeyRate,
                Availability = OfzExternalFactorAvailability.Missing,
                Limitations = [missingKeyRateLimitation]
            };
        }

        var requiredDates = metrics
            .Select(metric => metric.TradeDate.Date)
            .Where(date => date >= startDate && date <= endDate)
            .Distinct()
            .ToList();
        var usedRateDates = requiredDates
            .Select(date => GetLatestOnOrBefore(keyRates, date)?.Date.Date)
            .Where(date => date.HasValue)
            .Select(date => date!.Value)
            .Distinct()
            .ToHashSet();
        var inRangeDates = keyRates
            .Where(rate => rate.Date.Date >= startDate && rate.Date.Date <= endDate)
            .Select(rate => rate.Date.Date);
        foreach (var date in inRangeDates)
        {
            usedRateDates.Add(date);
        }

        var observations = keyRates
            .Where(rate => usedRateDates.Contains(rate.Date.Date))
            .Select(rate => CreateCbrObservation(rate, keyRates, options))
            .OrderBy(observation => observation.TradeDate)
            .ToList();
        if (observations.Count == 0)
        {
            var latest = GetLatestOnOrBefore(keyRates, endDate);
            if (latest is not null)
            {
                observations.Add(CreateCbrObservation(latest, keyRates, options));
            }
        }

        var limitation = observations.Count == 0
            ? CreateLimitation(
                OfzDataLimitationKind.MissingExternalFactor,
                "Ключевая ставка ЦБ не найдена на дату или до даты периода.",
                CbrKeyRateCode,
                null)
            : null;

        return new OfzExternalFactorSeries
        {
            Kind = OfzExternalFactorKind.PolicyRate,
            Code = CbrKeyRateCode,
            Label = "Ключевая ставка ЦБ",
            Unit = "%",
            Scope = OfzExternalFactorScope.Market,
            Source = OfzExternalFactorSource.CbrKeyRate,
            Availability = observations.Count > 0
                ? OfzExternalFactorAvailability.Historical
                : OfzExternalFactorAvailability.Missing,
            Observations = observations,
            LatestObservation = observations.LastOrDefault(),
            Limitations = limitation is null ? [] : [limitation]
        };
    }

    private static OfzExternalFactorObservation CreateCbrObservation(
        CbrKeyRate rate,
        IReadOnlyList<CbrKeyRate> keyRates,
        OfzExternalFactorsContextOptions options)
    {
        var previous = keyRates
            .Where(item => item.Date.Date < rate.Date.Date)
            .OrderByDescending(item => item.Date)
            .FirstOrDefault();
        var change = previous is null ? (double?)null : rate.Rate - previous.Rate;
        var changePercent = change.HasValue && previous?.Rate > 0
            ? change.Value / previous.Rate
            : (double?)null;
        var isMeaningful = change.HasValue && Math.Abs(change.Value) >= options.MeaningfulPolicyRateChange;

        return new OfzExternalFactorObservation
        {
            TradeDate = rate.Date.Date,
            Value = rate.Rate,
            DailyChange = change,
            DailyChangePercent = changePercent,
            Direction = change.HasValue
                ? isMeaningful
                    ? change.Value > 0 ? OfzMarketIndexDirection.Up : OfzMarketIndexDirection.Down
                    : OfzMarketIndexDirection.Flat
                : null,
            IsMeaningful = isMeaningful,
            PreviousTradeDate = previous?.Date.Date,
            SourceLabel = "ЦБ"
        };
    }

    private static IEnumerable<OfzExternalFactorSeries> BuildIndexSeries(IReadOnlyCollection<OfzIndexContextDay> indexContextDays)
    {
        foreach (var group in indexContextDays.SelectMany(day => day.Points).GroupBy(point => point.SecId, StringComparer.Ordinal))
        {
            var points = group.OrderBy(point => point.TradeDate).ToList();
            var observations = points.Select(point => new OfzExternalFactorObservation
            {
                TradeDate = point.TradeDate,
                Value = point.Close,
                DailyChange = point.DailyChange,
                DailyChangePercent = point.DailyChangePercent,
                Direction = point.Direction,
                IsMeaningful = point.IsMeaningful,
                PreviousTradeDate = point.PreviousTradeDate,
                SourceLabel = point.SourceLabel,
                IsSnapshot = point.SourceKind == OfzMarketIndexSourceKind.Snapshot,
                IsProvisional = point.IsProvisional
            }).ToList();
            var latest = observations.LastOrDefault();

            yield return new OfzExternalFactorSeries
            {
                Kind = OfzExternalFactorKind.MarketIndex,
                Code = group.Key,
                Label = points.LastOrDefault()?.DisplayName ?? group.Key,
                Unit = "index",
                Scope = points.Any(point => point.Role == OfzMarketIndexRole.DurationSegment)
                    ? OfzExternalFactorScope.Segment
                    : OfzExternalFactorScope.Market,
                Source = OfzExternalFactorSource.MoexIndex,
                Availability = observations.Any(observation => observation.IsProvisional)
                    ? OfzExternalFactorAvailability.Provisional
                    : observations.Any(observation => observation.IsSnapshot)
                        ? OfzExternalFactorAvailability.SnapshotOnly
                        : OfzExternalFactorAvailability.Historical,
                Observations = observations,
                LatestObservation = latest,
                Limitations = points.SelectMany(point => point.Limitations).ToList()
            };
        }
    }

    private static IEnumerable<OfzExternalFactorSeries> BuildSpecialMetricSeries(IEnumerable<OfzSpecialSummaryMetric> specialMetrics)
    {
        foreach (var metric in specialMetrics.OrderBy(metric => metric.Kind))
        {
            var observation = metric.ObservedAt.HasValue
                ? new OfzExternalFactorObservation
                {
                    TradeDate = metric.ObservedAt.Value.Date,
                    Value = metric.Value,
                    SourceLabel = FormatSpecialSource(metric.Source),
                    IsSnapshot = metric.Source == OfzSpecialMetricSource.Snapshot,
                    IsProvisional = metric.IsProvisional
                }
                : null;
            var limitations = metric.Availability == OfzSpecialMetricAvailability.Missing
                ? [CreateLimitation(OfzDataLimitationKind.MissingExternalFactor, $"{metric.Label}: фактор недоступен.", metric.Code, metric.ObservedAt)]
                : Array.Empty<OfzDataLimitation>();

            yield return new OfzExternalFactorSeries
            {
                Kind = OfzExternalFactorKind.DerivedOfzMetric,
                Code = metric.Code,
                Label = metric.Label,
                Unit = metric.Unit,
                Scope = OfzExternalFactorScope.Derived,
                Source = OfzExternalFactorSource.OfzDerived,
                Availability = MapAvailability(metric.Availability),
                Observations = observation is null ? [] : [observation],
                LatestObservation = observation,
                Limitations = limitations
            };
        }
    }

    private static IEnumerable<OfzExternalFactorActivityLink> BuildLinks(
        IReadOnlyCollection<OfzActivityMetric> metrics,
        OfzExternalFactorSeries cbrSeries,
        IReadOnlyCollection<OfzExternalFactorSeries> indexSeries,
        DateTime insightStartDate,
        DateTime insightEndDate,
        OfzExternalFactorsContextOptions options)
    {
        var candidates = metrics
            .Where(metric => metric.TradeDate.Date >= insightStartDate && metric.TradeDate.Date <= insightEndDate)
            .GroupBy(metric => metric.TradeDate.Date)
            .Select(group => new ActivityDay(
                group.Key,
                group.Sum(metric => metric.Value ?? 0),
                group.Where(metric => metric.ActivityScore.HasValue).Select(metric => metric.ActivityScore!.Value).DefaultIfEmpty().Max(),
                group.Where(metric => metric.YieldMove.HasValue)
                    .OrderByDescending(metric => Math.Abs(metric.YieldMove!.Value))
                    .Select(metric => metric.YieldMove)
                    .FirstOrDefault()))
            .OrderBy(item => item.TradeDate)
            .ToList();
        if (options.SignalScope == OfzSummarySignalScope.LastAvailableDay && candidates.Count > 0)
        {
            var lastDate = candidates.Max(item => item.TradeDate);
            candidates = candidates.Where(item => item.TradeDate == lastDate).ToList();
        }

        var rgbi = indexSeries.FirstOrDefault(series => string.Equals(series.Code, "RGBI", StringComparison.Ordinal));
        foreach (var day in candidates)
        {
            var indexObservation = rgbi?.Observations.FirstOrDefault(observation => observation.TradeDate == day.TradeDate);
            if (indexObservation is not null)
            {
                yield return CreateLink(day, rgbi!, indexObservation);
            }

            var cbrObservation = cbrSeries.Observations
                .Where(observation => observation.TradeDate <= day.TradeDate)
                .OrderByDescending(observation => observation.TradeDate)
                .FirstOrDefault();
            if (cbrObservation is not null)
            {
                yield return CreateLink(day, cbrSeries, cbrObservation);
            }
            else if (cbrSeries.Availability == OfzExternalFactorAvailability.Missing)
            {
                yield return new OfzExternalFactorActivityLink
                {
                    TradeDate = day.TradeDate,
                    FactorCode = cbrSeries.Code,
                    LinkKind = OfzExternalFactorLinkKind.MissingFactor,
                    ActivityValue = day.ActivityValue,
                    ActivityScore = day.ActivityScore,
                    YieldMove = day.YieldMove,
                    Text = $"{day.TradeDate:dd.MM.yyyy}: активность есть, но ключевая ставка ЦБ недоступна.",
                    Limitations = cbrSeries.Limitations
                };
            }
        }
    }

    private static OfzExternalFactorActivityLink CreateLink(
        ActivityDay day,
        OfzExternalFactorSeries series,
        OfzExternalFactorObservation observation)
    {
        var linkKind = observation.IsMeaningful && day.YieldMove.HasValue
            ? OfzExternalFactorLinkKind.YieldMoveWithFactorMove
            : observation.IsMeaningful
                ? OfzExternalFactorLinkKind.ActivityWithFactorMove
                : OfzExternalFactorLinkKind.ActivityWithoutFactorMove;
        var text = linkKind switch
        {
            OfzExternalFactorLinkKind.YieldMoveWithFactorMove => $"{day.TradeDate:dd.MM.yyyy}: движение доходности совпало с движением {series.Code}.",
            OfzExternalFactorLinkKind.ActivityWithFactorMove => $"{day.TradeDate:dd.MM.yyyy}: активность совпала с движением {series.Code}.",
            _ => $"{day.TradeDate:dd.MM.yyyy}: активность прошла на фоне спокойного {series.Code}."
        };

        return new OfzExternalFactorActivityLink
        {
            TradeDate = day.TradeDate,
            FactorCode = series.Code,
            FactorObservationDate = observation.TradeDate,
            LinkKind = linkKind,
            ActivityValue = day.ActivityValue,
            ActivityScore = day.ActivityScore,
            YieldMove = day.YieldMove,
            Text = text,
            Limitations = series.Limitations
        };
    }

    private static IEnumerable<OfzDataLimitation> BuildLimitations(
        OfzExternalFactorSeries cbrSeries,
        IReadOnlyCollection<OfzExternalFactorSeries> indexSeries,
        IReadOnlyCollection<OfzExternalFactorSeries> specialSeries)
    {
        foreach (var limitation in cbrSeries.Limitations)
        {
            yield return limitation;
        }

        if (indexSeries.Count == 0)
        {
            yield return CreateLimitation(
                OfzDataLimitationKind.MissingExternalFactor,
                "MOEX index context недоступен для факторного слоя.",
                "moex_index",
                null);
        }

        foreach (var limitation in indexSeries.SelectMany(series => series.Limitations))
        {
            yield return limitation;
        }

        foreach (var limitation in specialSeries.SelectMany(series => series.Limitations))
        {
            yield return limitation;
        }
    }

    private static bool HasActivityEvidence(OfzActivityMetric metric)
    {
        return metric.Value is > 0 ||
            metric.NumTrades is > 0 ||
            metric.ActivityScore.HasValue ||
            metric.YieldMove.HasValue;
    }

    private static CbrKeyRate? GetLatestOnOrBefore(IReadOnlyList<CbrKeyRate> keyRates, DateTime date)
    {
        return keyRates.LastOrDefault(rate => rate.Date.Date <= date.Date);
    }

    private static OfzExternalFactorAvailability MapAvailability(OfzSpecialMetricAvailability availability)
    {
        return availability switch
        {
            OfzSpecialMetricAvailability.Historical => OfzExternalFactorAvailability.Historical,
            OfzSpecialMetricAvailability.SnapshotOnly => OfzExternalFactorAvailability.SnapshotOnly,
            OfzSpecialMetricAvailability.Provisional => OfzExternalFactorAvailability.Provisional,
            OfzSpecialMetricAvailability.InsufficientHistory => OfzExternalFactorAvailability.InsufficientHistory,
            _ => OfzExternalFactorAvailability.Missing
        };
    }

    private static string FormatSpecialSource(OfzSpecialMetricSource source)
    {
        return source switch
        {
            OfzSpecialMetricSource.History => "history",
            OfzSpecialMetricSource.Snapshot => "snapshot",
            OfzSpecialMetricSource.Derived => "derived",
            OfzSpecialMetricSource.CbrKeyRate => "ЦБ",
            _ => "missing"
        };
    }

    private static OfzDataLimitation CreateLimitation(
        OfzDataLimitationKind kind,
        string text,
        string? secId,
        DateTime? tradeDate)
    {
        return new OfzDataLimitation
        {
            Kind = kind,
            Scope = OfzSummaryScope.ExternalFactors,
            Text = text,
            SecId = secId,
            TradeDate = tradeDate
        };
    }

    private static void Validate(OfzExternalFactorsContextOptions options)
    {
        if (options.StartDate.HasValue &&
            options.EndDate.HasValue &&
            options.StartDate.Value.Date > options.EndDate.Value.Date)
        {
            throw new ArgumentException("StartDate must be less than or equal to EndDate.", nameof(options));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(options.MeaningfulPolicyRateChange);
    }

    private readonly record struct ActivityDay(
        DateTime TradeDate,
        double ActivityValue,
        double ActivityScore,
        double? YieldMove);
}
