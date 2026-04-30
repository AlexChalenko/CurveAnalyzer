using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Services;

public class SpreadCalculatorTests
{
    [Fact]
    public void Calculate_AlignsByTradingDateAndUsesSecondMinusFirst()
    {
        var first = new HistoricalSeries(
            new CurvePeriod(1),
            [
                new HistoricalPoint(new TradingDate(new DateTime(2026, 04, 30)), 7),
                new HistoricalPoint(new TradingDate(new DateTime(2026, 05, 02)), 9),
                new HistoricalPoint(new TradingDate(new DateTime(2026, 05, 01)), 8)
            ]);

        var second = new HistoricalSeries(
            new CurvePeriod(2),
            [
                new HistoricalPoint(new TradingDate(new DateTime(2026, 04, 30)), 10),
                new HistoricalPoint(new TradingDate(new DateTime(2026, 05, 01)), 12)
            ]);

        var spread = SpreadCalculator.Calculate(first, second);

        Assert.Equal([3, 4], spread.Points.Select(point => point.Value).ToArray());
        Assert.Equal(
            [new DateTime(2026, 04, 30), new DateTime(2026, 05, 01)],
            spread.Points.Select(point => point.TradingDate.Date).ToArray());
    }
}
