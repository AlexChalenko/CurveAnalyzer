namespace CurveAnalyzer.Core;

public sealed class OfzIndexContextOptions
{
    public double MeaningfulCloseChangePercent { get; init; } = 0.0025;
    public double MeaningfulYieldChange { get; init; } = 0.10;
    public IReadOnlyList<OfzMarketIndexSeries> Series { get; init; } = OfzIndexContextBuilder.DefaultSeries;
}

public static class OfzIndexContextBuilder
{
    public static IReadOnlyList<OfzMarketIndexSeries> DefaultSeries { get; } =
    [
        new()
        {
            SecId = "RGBI",
            ShortName = "RGBI",
            Name = "Индекс Мосбиржи государственных облигаций ценовой",
            Role = OfzMarketIndexRole.WholeMarket,
            ReturnKind = OfzMarketIndexReturnKind.Price,
            DurationBucket = OfzIndexDurationBucket.All,
            CurrencyId = "RUB",
            IsRequired = true
        },
        new()
        {
            SecId = "RGBITR",
            ShortName = "RGBITR",
            Name = "Индекс Мосбиржи государственных облигаций полной доходности",
            Role = OfzMarketIndexRole.WholeMarket,
            ReturnKind = OfzMarketIndexReturnKind.TotalReturn,
            DurationBucket = OfzIndexDurationBucket.All,
            CurrencyId = "RUB",
            IsRequired = true
        },
        Segment("RUGBICP1Y", OfzMarketIndexReturnKind.Price, OfzIndexDurationBucket.UpTo1Y),
        Segment("RUGBICP3Y", OfzMarketIndexReturnKind.Price, OfzIndexDurationBucket.OneToThreeY),
        Segment("RUGBICP5Y", OfzMarketIndexReturnKind.Price, OfzIndexDurationBucket.ThreeToFiveY),
        Segment("RUGBICP7Y+", OfzMarketIndexReturnKind.Price, OfzIndexDurationBucket.SevenYPlus),
        Segment("RUGBITR1Y", OfzMarketIndexReturnKind.TotalReturn, OfzIndexDurationBucket.UpTo1Y),
        Segment("RUGBITR3Y", OfzMarketIndexReturnKind.TotalReturn, OfzIndexDurationBucket.OneToThreeY),
        Segment("RUGBITR5Y", OfzMarketIndexReturnKind.TotalReturn, OfzIndexDurationBucket.ThreeToFiveY),
        Segment("RUGBITR7Y+", OfzMarketIndexReturnKind.TotalReturn, OfzIndexDurationBucket.SevenYPlus)
    ];

    public static OfzIndexContext Build(
        IEnumerable<OfzMarketIndexPoint> points,
        DateTime startDate,
        DateTime endDate,
        IEnumerable<DateTime>? activityDates = null,
        OfzIndexContextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(points);
        options ??= new OfzIndexContextOptions();
        Validate(startDate, endDate, options);

        var rangeStart = startDate.Date;
        var rangeEnd = endDate.Date;
        var seriesBySecId = options.Series
            .Where(series => !string.IsNullOrWhiteSpace(series.SecId))
            .GroupBy(series => series.SecId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var orderedPoints = points
            .Where(point => !string.IsNullOrWhiteSpace(point.SecId))
            .Where(point => seriesBySecId.ContainsKey(point.SecId))
            .OrderBy(point => point.SecId, StringComparer.Ordinal)
            .ThenBy(point => point.TradeDate)
            .ThenBy(point => point.SourceKind)
            .ToList();
        var movesByKey = BuildMoves(orderedPoints, options);
        var pointsByDate = orderedPoints
            .Where(point => point.TradeDate.Date >= rangeStart && point.TradeDate.Date <= rangeEnd)
            .GroupBy(point => point.TradeDate.Date)
            .ToDictionary(group => group.Key, group => group.ToList());

        var dates = activityDates?.Select(date => date.Date)
            .Where(date => date >= rangeStart && date <= rangeEnd)
            .Distinct()
            .OrderBy(date => date)
            .ToList();
        if (dates is null || dates.Count == 0)
        {
            dates = pointsByDate.Keys.OrderBy(date => date).ToList();
        }

        var days = dates
            .Select(date => BuildContextDay(date, pointsByDate.GetValueOrDefault(date) ?? [], seriesBySecId, movesByKey, options))
            .ToList();
        var segments = BuildSegments(days, options.Series);
        var limitations = BuildGlobalLimitations(days, options.Series, rangeStart, rangeEnd);

        return new OfzIndexContext
        {
            Series = options.Series,
            Days = days,
            Segments = segments,
            Limitations = limitations
        };
    }

    private static OfzMarketIndexSeries Segment(
        string secId,
        OfzMarketIndexReturnKind returnKind,
        OfzIndexDurationBucket bucket)
    {
        return new OfzMarketIndexSeries
        {
            SecId = secId,
            ShortName = secId,
            Name = $"Индекс МосБиржи государственных облигаций {secId}",
            Role = OfzMarketIndexRole.DurationSegment,
            ReturnKind = returnKind,
            DurationBucket = bucket,
            CurrencyId = "RUB"
        };
    }

    private static Dictionary<(string SecId, DateTime TradeDate, OfzMarketIndexSourceKind SourceKind), OfzIndexDailyMove> BuildMoves(
        IReadOnlyList<OfzMarketIndexPoint> points,
        OfzIndexContextOptions options)
    {
        var result = new Dictionary<(string, DateTime, OfzMarketIndexSourceKind), OfzIndexDailyMove>();
        foreach (var group in points.GroupBy(point => point.SecId, StringComparer.Ordinal))
        {
            OfzMarketIndexPoint? previousValid = null;
            foreach (var point in group.OrderBy(point => point.TradeDate).ThenBy(point => point.SourceKind))
            {
                var key = (point.SecId, point.TradeDate.Date, point.SourceKind);
                var limitation = point.Close.HasValue && double.IsFinite(point.Close.Value)
                    ? previousValid is null
                        ? CreateLimitation(
                            OfzDataLimitationKind.MissingPreviousIndexPoint,
                            OfzSummaryScope.IndexContext,
                            $"Для {point.SecId} нет предыдущего значения индекса.",
                            point.SecId,
                            point.TradeDate)
                        : null
                    : CreateLimitation(
                        OfzDataLimitationKind.MissingIndexField,
                        OfzSummaryScope.IndexContext,
                        $"{point.SecId}: значение индекса отсутствует и не заменяется нулем.",
                        point.SecId,
                        point.TradeDate);
                var change = point.Close.HasValue && previousValid?.Close.HasValue == true
                    ? point.Close.Value - previousValid.Close.Value
                    : (double?)null;
                var changePercent = change.HasValue && previousValid?.Close is > 0
                    ? change.Value / previousValid.Close.Value
                    : (double?)null;
                var yieldChange = point.Yield.HasValue && previousValid?.Yield.HasValue == true
                    ? point.Yield.Value - previousValid.Yield.Value
                    : (double?)null;
                var isMeaningful = IsMeaningful(changePercent, yieldChange, options);

                result[key] = new OfzIndexDailyMove
                {
                    SecId = point.SecId,
                    TradeDate = point.TradeDate.Date,
                    PreviousTradeDate = previousValid?.TradeDate.Date,
                    Close = point.Close,
                    PreviousClose = previousValid?.Close,
                    Change = change,
                    ChangePercent = changePercent,
                    Yield = point.Yield,
                    YieldChange = yieldChange,
                    Direction = GetDirection(changePercent, isMeaningful),
                    IsMeaningful = isMeaningful,
                    Limitation = limitation
                };

                if (point.Close.HasValue && double.IsFinite(point.Close.Value))
                {
                    previousValid = point;
                }
            }
        }

        return result;
    }

    private static OfzIndexContextDay BuildContextDay(
        DateTime tradeDate,
        IReadOnlyList<OfzMarketIndexPoint> points,
        IReadOnlyDictionary<string, OfzMarketIndexSeries> seriesBySecId,
        IReadOnlyDictionary<(string SecId, DateTime TradeDate, OfzMarketIndexSourceKind SourceKind), OfzIndexDailyMove> movesByKey,
        OfzIndexContextOptions options)
    {
        var contextPoints = points
            .GroupBy(point => new { point.SecId, point.SourceKind })
            .Select(group => group.OrderByDescending(point => point.LoadedAt).First())
            .Select(point => BuildContextPoint(point, seriesBySecId[point.SecId], movesByKey))
            .OrderBy(point => point.Role)
            .ThenBy(point => point.ReturnKind)
            .ThenBy(point => point.DurationBucket)
            .ThenBy(point => point.SecId, StringComparer.Ordinal)
            .ToList();
        var limitations = new List<OfzDataLimitation>();

        foreach (var requiredSeries in options.Series.Where(series => series.IsRequired))
        {
            if (contextPoints.All(point => !string.Equals(point.SecId, requiredSeries.SecId, StringComparison.Ordinal)))
            {
                limitations.Add(CreateLimitation(
                    OfzDataLimitationKind.MissingRequiredIndexSeries,
                    OfzSummaryScope.IndexContext,
                    $"{requiredSeries.SecId}: нет индексного значения за дату {tradeDate:dd.MM.yyyy}.",
                    requiredSeries.SecId,
                    tradeDate));
            }
        }

        limitations.AddRange(contextPoints.SelectMany(point => point.Limitations));

        var wholeMarketDirections = contextPoints
            .Where(point => point.Role == OfzMarketIndexRole.WholeMarket)
            .Select(point => point.Direction)
            .Where(direction => direction != OfzMarketIndexDirection.Unknown)
            .Distinct()
            .ToArray();
        var marketDirection = wholeMarketDirections.Length switch
        {
            0 => OfzMarketIndexDirection.Unknown,
            1 => wholeMarketDirections[0],
            _ => OfzMarketIndexDirection.Mixed
        };

        return new OfzIndexContextDay
        {
            TradeDate = tradeDate.Date,
            Points = contextPoints,
            MarketDirection = marketDirection,
            HasMeaningfulMove = contextPoints
                .Where(point => point.Role == OfzMarketIndexRole.WholeMarket)
                .Any(point => point.IsMeaningful),
            IsProvisional = contextPoints.Any(point => point.IsProvisional),
            Limitations = limitations
        };
    }

    private static OfzIndexContextPoint BuildContextPoint(
        OfzMarketIndexPoint point,
        OfzMarketIndexSeries series,
        IReadOnlyDictionary<(string SecId, DateTime TradeDate, OfzMarketIndexSourceKind SourceKind), OfzIndexDailyMove> movesByKey)
    {
        var move = movesByKey.GetValueOrDefault((point.SecId, point.TradeDate.Date, point.SourceKind));
        List<OfzDataLimitation> limitations = [];
        if (move?.Limitation is not null)
        {
            limitations.Add(move.Limitation);
        }

        if (point.IsProvisional)
        {
            limitations.Add(CreateLimitation(
                OfzDataLimitationKind.Provisional,
                OfzSummaryScope.IndexContext,
                $"{point.SecId}: индексное значение предварительное.",
                point.SecId,
                point.TradeDate));
        }

        return new OfzIndexContextPoint
        {
            SecId = point.SecId,
            DisplayName = !string.IsNullOrWhiteSpace(point.ShortName)
                ? point.ShortName
                : !string.IsNullOrWhiteSpace(series.ShortName) ? series.ShortName : point.SecId,
            Role = series.Role,
            ReturnKind = series.ReturnKind,
            DurationBucket = series.DurationBucket,
            TradeDate = point.TradeDate.Date,
            Close = point.Close,
            DailyChange = move?.Change,
            DailyChangePercent = move?.ChangePercent,
            Yield = point.Yield,
            YieldChange = move?.YieldChange,
            Duration = point.Duration,
            Value = point.Value,
            PreviousTradeDate = move?.PreviousTradeDate,
            Direction = move?.Direction ?? OfzMarketIndexDirection.Unknown,
            IsMeaningful = move?.IsMeaningful ?? false,
            SourceKind = point.SourceKind,
            IsProvisional = point.IsProvisional,
            Limitations = limitations
        };
    }

    private static IReadOnlyList<OfzIndexSegmentContext> BuildSegments(
        IReadOnlyCollection<OfzIndexContextDay> days,
        IReadOnlyList<OfzMarketIndexSeries> series)
    {
        return Enum.GetValues<OfzIndexDurationBucket>()
            .Where(bucket => bucket is not OfzIndexDurationBucket.All and not OfzIndexDurationBucket.Unknown)
            .Select(bucket => BuildSegment(bucket, days, series))
            .Where(segment => segment.PointCount > 0 || segment.Limitations.Count > 0)
            .ToList();
    }

    private static OfzIndexSegmentContext BuildSegment(
        OfzIndexDurationBucket bucket,
        IReadOnlyCollection<OfzIndexContextDay> days,
        IReadOnlyList<OfzMarketIndexSeries> series)
    {
        var priceSeries = series.FirstOrDefault(item =>
            item.DurationBucket == bucket && item.ReturnKind == OfzMarketIndexReturnKind.Price);
        var totalReturnSeries = series.FirstOrDefault(item =>
            item.DurationBucket == bucket && item.ReturnKind == OfzMarketIndexReturnKind.TotalReturn);
        var points = days
            .SelectMany(day => day.Points)
            .Where(point => point.DurationBucket == bucket)
            .OrderBy(point => point.TradeDate)
            .ToList();
        var pricePoints = points
            .Where(point => point.ReturnKind == OfzMarketIndexReturnKind.Price)
            .Where(point => point.Close.HasValue && double.IsFinite(point.Close.Value))
            .ToList();
        var firstPrice = pricePoints.FirstOrDefault();
        var latestPrice = pricePoints.LastOrDefault();
        var latestTotalReturn = points
            .Where(point => point.ReturnKind == OfzMarketIndexReturnKind.TotalReturn)
            .Where(point => point.Close.HasValue && double.IsFinite(point.Close.Value))
            .LastOrDefault();
        var limitations = new List<OfzDataLimitation>();

        if (priceSeries is not null && points.All(point => point.SecId != priceSeries.SecId))
        {
            limitations.Add(CreateLimitation(
                OfzDataLimitationKind.MissingSegmentIndexSeries,
                OfzSummaryScope.IndexContext,
                $"{priceSeries.SecId}: segment series недоступен.",
                priceSeries.SecId,
                null));
        }

        if (totalReturnSeries is not null && points.All(point => point.SecId != totalReturnSeries.SecId))
        {
            limitations.Add(CreateLimitation(
                OfzDataLimitationKind.MissingSegmentIndexSeries,
                OfzSummaryScope.IndexContext,
                $"{totalReturnSeries.SecId}: segment series недоступен.",
                totalReturnSeries.SecId,
                null));
        }

        var periodChange = firstPrice?.Close.HasValue == true && latestPrice?.Close.HasValue == true
            ? latestPrice.Close.Value - firstPrice.Close.Value
            : (double?)null;
        var periodChangePercent = periodChange.HasValue && firstPrice?.Close is > 0
            ? periodChange.Value / firstPrice.Close.Value
            : (double?)null;
        var latestPoint = latestPrice ?? latestTotalReturn;

        return new OfzIndexSegmentContext
        {
            Bucket = bucket,
            PriceSeriesSecId = priceSeries?.SecId,
            TotalReturnSeriesSecId = totalReturnSeries?.SecId,
            PointCount = points.Count,
            LatestPricePoint = latestPrice,
            LatestTotalReturnPoint = latestTotalReturn,
            PeriodChange = periodChange,
            PeriodChangePercent = periodChangePercent,
            LatestYield = latestPoint?.Yield,
            LatestDuration = latestPoint?.Duration,
            Limitations = limitations
        };
    }

    private static IReadOnlyList<OfzDataLimitation> BuildGlobalLimitations(
        IReadOnlyCollection<OfzIndexContextDay> days,
        IReadOnlyList<OfzMarketIndexSeries> series,
        DateTime startDate,
        DateTime endDate)
    {
        List<OfzDataLimitation> limitations = [];
        if (days.Count == 0 || days.All(day => day.Points.Count == 0))
        {
            limitations.Add(CreateLimitation(
                OfzDataLimitationKind.NoIndexData,
                OfzSummaryScope.IndexContext,
                $"Нет индексных данных MOEX за период {startDate:dd.MM.yyyy}-{endDate:dd.MM.yyyy}.",
                null,
                null));
            return limitations;
        }

        foreach (var requiredSeries in series.Where(item => item.IsRequired))
        {
            if (days.SelectMany(day => day.Points).All(point => point.SecId != requiredSeries.SecId))
            {
                limitations.Add(CreateLimitation(
                    OfzDataLimitationKind.MissingRequiredIndexSeries,
                    OfzSummaryScope.IndexContext,
                    $"{requiredSeries.SecId}: required index series отсутствует в выбранном периоде.",
                    requiredSeries.SecId,
                    null));
            }
        }

        if (days.Any(day => day.IsProvisional))
        {
            limitations.Add(CreateLimitation(
                OfzDataLimitationKind.Provisional,
                OfzSummaryScope.IndexContext,
                "Часть индексных значений предварительная для текущей даты.",
                null,
                null));
        }

        return limitations;
    }

    private static OfzDataLimitation CreateLimitation(
        OfzDataLimitationKind kind,
        OfzSummaryScope scope,
        string text,
        string? secId,
        DateTime? tradeDate)
    {
        return new OfzDataLimitation
        {
            Kind = kind,
            Scope = scope,
            Text = text,
            SecId = secId,
            TradeDate = tradeDate
        };
    }

    private static bool IsMeaningful(double? changePercent, double? yieldChange, OfzIndexContextOptions options)
    {
        return changePercent.HasValue && Math.Abs(changePercent.Value) >= options.MeaningfulCloseChangePercent ||
            yieldChange.HasValue && Math.Abs(yieldChange.Value) >= options.MeaningfulYieldChange;
    }

    private static OfzMarketIndexDirection GetDirection(double? changePercent, bool isMeaningful)
    {
        if (!changePercent.HasValue)
        {
            return OfzMarketIndexDirection.Unknown;
        }

        if (!isMeaningful)
        {
            return OfzMarketIndexDirection.Flat;
        }

        return changePercent.Value > 0
            ? OfzMarketIndexDirection.Up
            : OfzMarketIndexDirection.Down;
    }

    private static void Validate(DateTime startDate, DateTime endDate, OfzIndexContextOptions options)
    {
        if (startDate.Date > endDate.Date)
        {
            throw new ArgumentException("StartDate must be less than or equal to EndDate.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(options.MeaningfulCloseChangePercent);
        ArgumentOutOfRangeException.ThrowIfNegative(options.MeaningfulYieldChange);
    }
}
