namespace CurveAnalyzer.Core;

public static class OfzSpecialAnalyticsBuilder
{
    public static OfzSpecialIssueContext Build(
        OfzCouponType couponType,
        IEnumerable<OfzIssueDetailPoint> historicalPoints,
        OfzLiquiditySnapshot? currentSnapshot,
        IEnumerable<CbrKeyRate>? cbrKeyRates = null)
    {
        var points = historicalPoints
            .OrderBy(point => point.TradeDate)
            .ToArray();
        var cbrKeyRateLookup = CbrKeyRateLookup.Create(cbrKeyRates);

        return couponType switch
        {
            OfzCouponType.Floating => BuildFloating(points, currentSnapshot, cbrKeyRateLookup),
            OfzCouponType.InflationLinked => BuildInflationLinked(points, currentSnapshot),
            _ => OfzSpecialIssueContext.None
        };
    }

    private static OfzSpecialIssueContext BuildFloating(
        IReadOnlyList<OfzIssueDetailPoint> points,
        OfzLiquiditySnapshot? currentSnapshot,
        CbrKeyRateLookup cbrKeyRateLookup)
    {
        var floatingSeries = BuildSeries(
            OfzSpecialMetricKind.ImpliedFloatingRate,
            "implied_floating_rate",
            "Ожидаемая ставка купона",
            "%",
            points,
            point => FromHistory(point.TradeDate, point.ImpliedFloatingRate));
        var cbrSeries = BuildSeries(
            OfzSpecialMetricKind.ImpliedCbrRate,
            "implied_cbr_rate",
            "Ключевая ставка",
            "%",
            points,
            point => GetCbrRateObservation(point.TradeDate, point.ImpliedCbrRate, cbrKeyRateLookup));
        var spreadSeries = BuildDerivedSeries(
            OfzSpecialMetricKind.ImpliedFloatingRateSpread,
            "implied_floating_rate_spread",
            "Спред к ключевой ставке",
            "п.п.",
            points,
            point => point.ImpliedFloatingRate,
            point => GetCbrRateValue(point.TradeDate, point.ImpliedCbrRate, cbrKeyRateLookup));

        var metrics = new[]
        {
            BuildMetric(
                floatingSeries,
                FromSnapshot(currentSnapshot?.ImpliedFloatingRate, currentSnapshot, OfzSpecialMetricSource.Snapshot)),
            BuildMetric(
                cbrSeries,
                GetCurrentCbrObservation(currentSnapshot, cbrKeyRateLookup)),
            BuildMetric(
                spreadSeries,
                GetCurrentSpreadObservation(currentSnapshot, cbrKeyRateLookup))
        };

        return new OfzSpecialIssueContext
        {
            CouponType = OfzCouponType.Floating,
            Title = "Плавающий купон",
            Description = "ISS поле IRICPICLOSE и ключевая ставка: CBRCLOSE или официальный KeyRate ЦБ.",
            Metrics = metrics,
            Series = [floatingSeries, cbrSeries, spreadSeries],
            Limitations = BuildLimitations(metrics, [floatingSeries, cbrSeries, spreadSeries])
        };
    }

    private static OfzSpecialIssueContext BuildInflationLinked(
        IReadOnlyList<OfzIssueDetailPoint> points,
        OfzLiquiditySnapshot? currentSnapshot)
    {
        var inflationSeries = BuildSeries(
            OfzSpecialMetricKind.ImpliedInflation,
            "implied_inflation",
            "Ожидаемая инфляция",
            "%",
            points,
            point => FromHistory(point.TradeDate, point.ImpliedInflation));
        var metric = BuildMetric(
            inflationSeries,
            FromSnapshot(currentSnapshot?.ImpliedInflation, currentSnapshot, OfzSpecialMetricSource.Snapshot));

        return new OfzSpecialIssueContext
        {
            CouponType = OfzCouponType.InflationLinked,
            Title = "Индексация номинала",
            Description = "ISS поле BEICLOSE: ожидаемая инфляция для линкеров.",
            Metrics = [metric],
            Series = [inflationSeries],
            Limitations = BuildLimitations([metric], [inflationSeries])
        };
    }

    private static OfzSpecialMetricSeries BuildSeries(
        OfzSpecialMetricKind kind,
        string code,
        string label,
        string unit,
        IEnumerable<OfzIssueDetailPoint> points,
        Func<OfzIssueDetailPoint, SpecialMetricObservation?> selector)
    {
        var seriesPoints = points
            .Select(selector)
            .Where(observation => observation.HasValue && double.IsFinite(observation.Value.Value))
            .Select(observation => observation!.Value)
            .Select(observation => new OfzSpecialMetricSeriesPoint
            {
                TradeDate = observation.TradeDate,
                Value = observation.Value,
                Source = observation.Source,
                IsProvisional = observation.IsProvisional
            })
            .ToArray();

        return new OfzSpecialMetricSeries
        {
            Kind = kind,
            Code = code,
            Label = label,
            Unit = unit,
            Points = seriesPoints,
            Availability = GetSeriesAvailability(seriesPoints.Length)
        };
    }

    private static OfzSpecialMetricSeries BuildDerivedSeries(
        OfzSpecialMetricKind kind,
        string code,
        string label,
        string unit,
        IEnumerable<OfzIssueDetailPoint> points,
        Func<OfzIssueDetailPoint, double?> leftSelector,
        Func<OfzIssueDetailPoint, double?> rightSelector)
    {
        var seriesPoints = points
            .Select(point => (point.TradeDate, Left: leftSelector(point), Right: rightSelector(point)))
            .Where(point => point.Left.HasValue && point.Right.HasValue)
            .Select(point => new OfzSpecialMetricSeriesPoint
            {
                TradeDate = point.TradeDate,
                Value = point.Left!.Value - point.Right!.Value,
                Source = OfzSpecialMetricSource.Derived
            })
            .ToArray();

        return new OfzSpecialMetricSeries
        {
            Kind = kind,
            Code = code,
            Label = label,
            Unit = unit,
            Points = seriesPoints,
            Availability = GetSeriesAvailability(seriesPoints.Length)
        };
    }

    private static OfzSpecialMetricValue BuildMetric(
        OfzSpecialMetricSeries series,
        CurrentSpecialMetricObservation? currentObservation)
    {
        if (currentObservation.HasValue)
        {
            var current = currentObservation.Value;
            return new OfzSpecialMetricValue
            {
                Kind = series.Kind,
                Code = series.Code,
                Label = series.Label,
                Unit = series.Unit,
                Value = current.Value,
                ObservedAt = current.ObservedAt,
                Source = current.Source,
                Availability = current.Availability,
                IsProvisional = current.IsProvisional
            };
        }

        var latest = series.Points
            .OrderByDescending(point => point.TradeDate)
            .FirstOrDefault();
        if (latest is not null)
        {
            return new OfzSpecialMetricValue
            {
                Kind = series.Kind,
                Code = series.Code,
                Label = series.Label,
                Unit = series.Unit,
                Value = latest.Value,
                ObservedAt = latest.TradeDate,
                Source = latest.Source,
                Availability = series.Availability == OfzSpecialMetricAvailability.InsufficientHistory
                    ? OfzSpecialMetricAvailability.InsufficientHistory
                    : OfzSpecialMetricAvailability.Historical
            };
        }

        return new OfzSpecialMetricValue
        {
            Kind = series.Kind,
            Code = series.Code,
            Label = series.Label,
            Unit = series.Unit,
            Source = OfzSpecialMetricSource.Missing,
            Availability = OfzSpecialMetricAvailability.Missing
        };
    }

    private static IReadOnlyList<string> BuildLimitations(
        IReadOnlyList<OfzSpecialMetricValue> metrics,
        IReadOnlyList<OfzSpecialMetricSeries> series)
    {
        var limitations = new List<string>();

        if (metrics.Any(metric => metric.Source == OfzSpecialMetricSource.CbrKeyRate) ||
            series.Any(item => item.Points.Any(point => point.Source == OfzSpecialMetricSource.CbrKeyRate)))
        {
            limitations.Add("Ключевая ставка для части дат взята из официального сервиса ЦБ, потому что CBRCLOSE отсутствует.");
        }

        if (metrics.Any(metric => metric.Availability == OfzSpecialMetricAvailability.Missing))
        {
            limitations.Add("Часть специальных ISS-полей отсутствует и не заменяется нулем.");
        }

        if (series.Any(item => item.Availability == OfzSpecialMetricAvailability.InsufficientHistory))
        {
            limitations.Add("Для части рядов меньше двух исторических точек.");
        }

        if (metrics.Any(metric => metric.Availability == OfzSpecialMetricAvailability.SnapshotOnly))
        {
            limitations.Add("Часть значений доступна только из текущего snapshot.");
        }

        if (metrics.Any(metric => metric.Availability == OfzSpecialMetricAvailability.Provisional))
        {
            limitations.Add("Текущие значения предварительные: торговый день может быть не завершен.");
        }

        return limitations;
    }

    private static OfzSpecialMetricAvailability GetSeriesAvailability(int pointCount)
    {
        return pointCount switch
        {
            0 => OfzSpecialMetricAvailability.Missing,
            1 => OfzSpecialMetricAvailability.InsufficientHistory,
            _ => OfzSpecialMetricAvailability.Historical
        };
    }

    private static SpecialMetricObservation? FromHistory(DateTime tradeDate, double? value)
    {
        return value.HasValue && double.IsFinite(value.Value)
            ? new SpecialMetricObservation(tradeDate.Date, value.Value, OfzSpecialMetricSource.History, false)
            : null;
    }

    private static CurrentSpecialMetricObservation? FromSnapshot(
        double? value,
        OfzLiquiditySnapshot? currentSnapshot,
        OfzSpecialMetricSource source)
    {
        if (!value.HasValue || !double.IsFinite(value.Value) || currentSnapshot is null)
        {
            return null;
        }

        return new CurrentSpecialMetricObservation(
            value.Value,
            currentSnapshot.ObservedAt,
            source,
            currentSnapshot.IsProvisional,
            currentSnapshot.IsProvisional
                ? OfzSpecialMetricAvailability.Provisional
                : OfzSpecialMetricAvailability.SnapshotOnly);
    }

    private static SpecialMetricObservation? GetCbrRateObservation(
        DateTime tradeDate,
        double? moexCbrRate,
        CbrKeyRateLookup cbrKeyRateLookup)
    {
        if (moexCbrRate.HasValue && double.IsFinite(moexCbrRate.Value))
        {
            return new SpecialMetricObservation(
                tradeDate.Date,
                moexCbrRate.Value,
                OfzSpecialMetricSource.History,
                false);
        }

        var cbrRate = cbrKeyRateLookup.GetLatestOnOrBefore(tradeDate);
        return cbrRate is null
            ? null
            : new SpecialMetricObservation(
                tradeDate.Date,
                cbrRate.Rate,
                OfzSpecialMetricSource.CbrKeyRate,
                false);
    }

    private static double? GetCbrRateValue(
        DateTime tradeDate,
        double? moexCbrRate,
        CbrKeyRateLookup cbrKeyRateLookup)
    {
        return moexCbrRate.HasValue && double.IsFinite(moexCbrRate.Value)
            ? moexCbrRate.Value
            : cbrKeyRateLookup.GetLatestOnOrBefore(tradeDate)?.Rate;
    }

    private static CurrentSpecialMetricObservation? GetCurrentCbrObservation(
        OfzLiquiditySnapshot? currentSnapshot,
        CbrKeyRateLookup cbrKeyRateLookup)
    {
        var snapshotObservation = FromSnapshot(
            currentSnapshot?.ImpliedCbrRate,
            currentSnapshot,
            OfzSpecialMetricSource.Snapshot);
        if (snapshotObservation.HasValue)
        {
            return snapshotObservation;
        }

        if (currentSnapshot is null)
        {
            return null;
        }

        var cbrRate = cbrKeyRateLookup.GetLatestOnOrBefore(currentSnapshot.TradeDate);
        return cbrRate is null
            ? null
            : new CurrentSpecialMetricObservation(
                cbrRate.Rate,
                cbrRate.Date,
                OfzSpecialMetricSource.CbrKeyRate,
                false,
                OfzSpecialMetricAvailability.Historical);
    }

    private static CurrentSpecialMetricObservation? GetCurrentSpreadObservation(
        OfzLiquiditySnapshot? currentSnapshot,
        CbrKeyRateLookup cbrKeyRateLookup)
    {
        if (currentSnapshot?.ImpliedFloatingRate is not { } floatingRate ||
            !double.IsFinite(floatingRate))
        {
            return null;
        }

        var cbrRate = GetCbrRateValue(
            currentSnapshot.TradeDate,
            currentSnapshot.ImpliedCbrRate,
            cbrKeyRateLookup);
        if (!cbrRate.HasValue || !double.IsFinite(cbrRate.Value))
        {
            return null;
        }

        return new CurrentSpecialMetricObservation(
            floatingRate - cbrRate.Value,
            currentSnapshot.ObservedAt,
            OfzSpecialMetricSource.Derived,
            currentSnapshot.IsProvisional,
            currentSnapshot.IsProvisional
                ? OfzSpecialMetricAvailability.Provisional
                : OfzSpecialMetricAvailability.SnapshotOnly);
    }

    private readonly record struct SpecialMetricObservation(
        DateTime TradeDate,
        double Value,
        OfzSpecialMetricSource Source,
        bool IsProvisional);

    private readonly record struct CurrentSpecialMetricObservation(
        double Value,
        DateTime ObservedAt,
        OfzSpecialMetricSource Source,
        bool IsProvisional,
        OfzSpecialMetricAvailability Availability);
}
