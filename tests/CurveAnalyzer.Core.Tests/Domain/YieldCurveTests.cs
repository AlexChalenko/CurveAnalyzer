using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Domain;

public class YieldCurveTests
{
    [Fact]
    public void Constructor_OrdersPointsByPeriod()
    {
        var date = new TradingDate(new DateTime(2026, 04, 30));

        var curve = new YieldCurve(
            date,
            [
                new YieldPoint(date, new CurvePeriod(2.0), 10),
                new YieldPoint(date, new CurvePeriod(0.5), 8),
                new YieldPoint(date, new CurvePeriod(1.0), 9)
            ]);

        Assert.Equal([0.5, 1.0, 2.0], curve.Points.Select(point => point.Period.Value).ToArray());
    }

    [Fact]
    public void Constructor_RejectsDuplicatePeriods()
    {
        var date = new TradingDate(new DateTime(2026, 04, 30));

        Assert.Throws<ArgumentException>(() => new YieldCurve(
            date,
            [
                new YieldPoint(date, new CurvePeriod(1.0), 10),
                new YieldPoint(date, new CurvePeriod(1.0), 11)
            ]));
    }
}
