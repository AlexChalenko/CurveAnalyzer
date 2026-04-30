namespace CurveAnalyzer.Core;

public static class SpreadCalculator
{
    public static SpreadSeries Calculate(HistoricalSeries first, HistoricalSeries second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var secondByDate = second.Points.ToDictionary(point => point.TradingDate);
        var points = first.Points
            .Where(point => secondByDate.ContainsKey(point.TradingDate))
            .Select(point => new SpreadPoint(
                point.TradingDate,
                secondByDate[point.TradingDate].Value - point.Value));

        return new SpreadSeries(first.Period, second.Period, points);
    }
}
