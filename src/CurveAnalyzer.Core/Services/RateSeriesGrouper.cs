using System.Globalization;

namespace CurveAnalyzer.Core;

public readonly record struct RateOhlcPoint(
    int YearWeek,
    TradingDate StartDate,
    double Open,
    double High,
    double Low,
    double Close);

public static class RateSeriesGrouper
{
    public static IReadOnlyList<RateOhlcPoint> GroupWeekly(HistoricalSeries series)
    {
        ArgumentNullException.ThrowIfNull(series);

        return series.Points
            .GroupBy(point => GetYearWeek(point.TradingDate))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var points = group.OrderBy(point => point.TradingDate).ToList();
                return new RateOhlcPoint(
                    group.Key,
                    points[0].TradingDate,
                    points[0].Value,
                    points.Max(point => point.Value),
                    points.Min(point => point.Value),
                    points[^1].Value);
            })
            .ToList();
    }

    private static int GetYearWeek(TradingDate tradingDate)
    {
        var date = tradingDate.Date;
        return ISOWeek.GetYear(date) * 100 + ISOWeek.GetWeekOfYear(date);
    }
}
