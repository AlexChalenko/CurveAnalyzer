namespace CurveAnalyzer.Core;

public sealed class YieldCurve
{
    public TradingDate TradingDate { get; }
    public IReadOnlyList<YieldPoint> Points { get; }

    public YieldCurve(TradingDate tradingDate, IEnumerable<YieldPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var orderedPoints = points.OrderBy(point => point.Period).ToList();

        if (orderedPoints.Any(point => point.TradingDate != tradingDate))
        {
            throw new ArgumentException("All yield points must have the same trading date.", nameof(points));
        }

        if (orderedPoints.GroupBy(point => point.Period).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Yield curve periods must be unique.", nameof(points));
        }

        TradingDate = tradingDate;
        Points = orderedPoints;
    }
}

public readonly record struct HistoricalPoint(TradingDate TradingDate, double Value);

public sealed class HistoricalSeries
{
    public CurvePeriod Period { get; }
    public IReadOnlyList<HistoricalPoint> Points { get; }

    public HistoricalSeries(CurvePeriod period, IEnumerable<HistoricalPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var orderedPoints = points.OrderBy(point => point.TradingDate).ToList();

        if (orderedPoints.GroupBy(point => point.TradingDate).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Historical series trading dates must be unique.", nameof(points));
        }

        Period = period;
        Points = orderedPoints;
    }
}

public readonly record struct SpreadPoint(TradingDate TradingDate, double Value);

public sealed class SpreadSeries
{
    public CurvePeriod FirstPeriod { get; }
    public CurvePeriod SecondPeriod { get; }
    public IReadOnlyList<SpreadPoint> Points { get; }

    public SpreadSeries(CurvePeriod firstPeriod, CurvePeriod secondPeriod, IEnumerable<SpreadPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (firstPeriod == secondPeriod)
        {
            throw new ArgumentException("Spread periods must be different.", nameof(secondPeriod));
        }

        var orderedPoints = points.OrderBy(point => point.TradingDate).ToList();
        if (orderedPoints.GroupBy(point => point.TradingDate).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Spread series trading dates must be unique.", nameof(points));
        }

        FirstPeriod = firstPeriod;
        SecondPeriod = secondPeriod;
        Points = orderedPoints;
    }
}
