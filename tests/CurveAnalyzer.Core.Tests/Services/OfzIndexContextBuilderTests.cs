using System.Diagnostics;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Services;

public sealed class OfzIndexContextBuilderTests
{
    private static readonly DateTime StartDate = new(2026, 04, 01);
    private static readonly DateTime EndDate = new(2026, 04, 05);

    [Fact]
    public void Build_CalculatesDailyMoveFromPreviousValidPoint()
    {
        var context = Build(
        [
            Point("RGBI", StartDate.AddDays(-1), close: 100, yield: 14.0),
            Point("RGBI", StartDate, close: 100.5, yield: 14.12),
            Point("RGBITR", StartDate.AddDays(-1), close: 200),
            Point("RGBITR", StartDate, close: 200.1)
        ]);

        var point = context.Days.Single().PriceIndexPoint;

        Assert.NotNull(point);
        Assert.Equal(0.5, point.DailyChange);
        Assert.Equal(0.005, point.DailyChangePercent);
        Assert.Equal(0.12, point.YieldChange!.Value, precision: 6);
        Assert.Equal(OfzMarketIndexDirection.Up, point.Direction);
        Assert.True(point.IsMeaningful);
    }

    [Fact]
    public void Build_MarksMissingPreviousPointAsLimitationInsteadOfZeroMove()
    {
        var context = Build([Point("RGBI", StartDate, close: 100), Point("RGBITR", StartDate, close: 200)]);
        var point = context.Days.Single().PriceIndexPoint;

        Assert.NotNull(point);
        Assert.Null(point.DailyChange);
        Assert.Null(point.DailyChangePercent);
        Assert.Contains(point.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.MissingPreviousIndexPoint);
    }

    [Fact]
    public void Build_KeepsMissingCloseAsNullAndDoesNotForwardFillZero()
    {
        var context = Build(
        [
            Point("RGBI", StartDate.AddDays(-1), close: 100),
            Point("RGBI", StartDate, close: null),
            Point("RGBITR", StartDate, close: 200)
        ]);
        var point = context.Days.Single().PriceIndexPoint;

        Assert.NotNull(point);
        Assert.Null(point.Close);
        Assert.Null(point.DailyChange);
        Assert.Contains(point.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.MissingIndexField);
    }

    [Fact]
    public void Build_AddsRequiredWholeMarketSeriesAndMissingRequiredLimitations()
    {
        var context = Build([Point("RGBI", StartDate, close: 100)], activityDates: [StartDate]);
        var day = context.Days.Single();

        Assert.NotNull(day.PriceIndexPoint);
        Assert.Null(day.TotalReturnIndexPoint);
        Assert.Contains(day.Limitations, limitation =>
            limitation.Kind == OfzDataLimitationKind.MissingRequiredIndexSeries &&
            limitation.SecId == "RGBITR");
    }

    [Fact]
    public void Build_UsesPreviousValidPointAfterMissingClose()
    {
        var context = Build(
        [
            Point("RGBI", StartDate.AddDays(-2), close: 100),
            Point("RGBI", StartDate.AddDays(-1), close: null),
            Point("RGBI", StartDate, close: 101),
            Point("RGBITR", StartDate, close: 200)
        ]);
        var point = context.Days.Single().PriceIndexPoint;

        Assert.NotNull(point);
        Assert.Equal(StartDate.AddDays(-2), point.PreviousTradeDate);
        Assert.Equal(1, point.DailyChange);
        Assert.Equal(0.01, point.DailyChangePercent);
    }

    [Fact]
    public void Build_PropagatesProvisionalSnapshotMarker()
    {
        var context = Build(
        [
            Point("RGBI", StartDate.AddDays(-1), close: 100),
            Point("RGBI", StartDate, close: 100.1, sourceKind: OfzMarketIndexSourceKind.Snapshot, isProvisional: true),
            Point("RGBITR", StartDate, close: 200)
        ]);
        var point = context.Days.Single().PriceIndexPoint;

        Assert.NotNull(point);
        Assert.Equal(OfzMarketIndexSourceKind.Snapshot, point.SourceKind);
        Assert.True(point.IsProvisional);
        Assert.True(context.Days.Single().IsProvisional);
        Assert.Contains(point.Limitations, limitation => limitation.Kind == OfzDataLimitationKind.Provisional);
    }

    [Fact]
    public void Build_MapsDurationBucketsAndPartialSegmentAvailability()
    {
        var context = Build(
        [
            Point("RGBI", StartDate, close: 100),
            Point("RGBITR", StartDate, close: 200),
            Point("RUGBICP1Y", StartDate, close: 90),
            Point("RUGBICP1Y", StartDate.AddDays(1), close: 91),
            Point("RUGBITR1Y", StartDate.AddDays(1), close: 191)
        ]);

        var segmentPoint = Assert.Single(
            context.Days.SelectMany(day => day.SegmentPoints),
            point => point.SecId == "RUGBICP1Y" && point.TradeDate == StartDate.AddDays(1));
        Assert.Equal(OfzIndexDurationBucket.UpTo1Y, segmentPoint.DurationBucket);

        var segment = Assert.Single(context.Segments, item => item.Bucket == OfzIndexDurationBucket.UpTo1Y);
        Assert.Equal("RUGBICP1Y", segment.PriceSeriesSecId);
        Assert.Equal("RUGBITR1Y", segment.TotalReturnSeriesSecId);
        Assert.Equal(1, segment.PeriodChange);
        Assert.Empty(segment.Limitations);

        Assert.Contains(context.Segments, item =>
            item.Bucket == OfzIndexDurationBucket.OneToThreeY &&
            item.Limitations.Any(limitation => limitation.Kind == OfzDataLimitationKind.MissingSegmentIndexSeries));
    }

    [Fact]
    public void Build_ReturnsCompleteDayDrillDownData()
    {
        var context = Build(
        [
            Point("RGBI", StartDate.AddDays(-1), close: 100, yield: 14.1, duration: 900, value: 1_000_000),
            Point("RGBI", StartDate, close: 101, yield: 14.0, duration: 901, value: 2_000_000),
            Point("RGBITR", StartDate, close: 201)
        ]);
        var point = context.Days.Single().PriceIndexPoint;

        Assert.NotNull(point);
        Assert.Equal("RGBI", point.SecId);
        Assert.Equal(StartDate, point.TradeDate);
        Assert.Equal(101, point.Close);
        Assert.Equal(-0.1, point.YieldChange!.Value, precision: 6);
        Assert.Equal(901, point.Duration);
        Assert.Equal(2_000_000, point.Value);
        Assert.Equal(StartDate.AddDays(-1), point.PreviousTradeDate);
    }

    [Fact]
    public void Build_PerformanceSmokeForTradingYearAndTenSeries()
    {
        var dates = Enumerable.Range(0, 252)
            .Select(offset => StartDate.AddDays(offset))
            .ToArray();
        var secIds = OfzIndexContextBuilder.DefaultSeries.Select(series => series.SecId).ToArray();
        var points = dates
            .SelectMany((date, dateIndex) => secIds.Select((secId, secIndex) =>
                Point(secId, date, 100 + dateIndex * 0.1 + secIndex, yield: 14 + secIndex * 0.01)))
            .ToArray();

        var stopwatch = Stopwatch.StartNew();
        var context = OfzIndexContextBuilder.Build(points, dates.First(), dates.Last());
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), stopwatch.Elapsed.ToString());
        Assert.Equal(252, context.Days.Count);
        Assert.True(context.Days.Sum(day => day.Points.Count) >= 2520);
    }

    private static OfzIndexContext Build(
        IReadOnlyCollection<OfzMarketIndexPoint> points,
        IReadOnlyCollection<DateTime>? activityDates = null)
    {
        return OfzIndexContextBuilder.Build(points, StartDate, EndDate, activityDates);
    }

    private static OfzMarketIndexPoint Point(
        string secId,
        DateTime tradeDate,
        double? close,
        double? yield = null,
        double? duration = null,
        double? value = null,
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
            Duration = duration,
            Value = value,
            SourceKind = sourceKind,
            IsProvisional = isProvisional,
            LoadedAt = DateTime.UtcNow
        };
    }
}
