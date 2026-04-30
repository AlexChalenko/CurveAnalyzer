using CurveAnalyzer.Core;

namespace CurveAnalyzer.Core.Tests.Services;

public class RateSeriesGrouperTests
{
    [Fact]
    public void GroupWeekly_SortsInputAndBuildsOhlcPerWeek()
    {
        var series = new HistoricalSeries(
            new CurvePeriod(1),
            [
                new HistoricalPoint(new TradingDate(new DateTime(2026, 01, 07)), 8),
                new HistoricalPoint(new TradingDate(new DateTime(2026, 01, 05)), 10),
                new HistoricalPoint(new TradingDate(new DateTime(2026, 01, 06)), 7),
                new HistoricalPoint(new TradingDate(new DateTime(2026, 01, 12)), 11)
            ]);

        var groups = RateSeriesGrouper.GroupWeekly(series);

        Assert.Collection(
            groups,
            first =>
            {
                Assert.Equal(new DateTime(2026, 01, 05), first.StartDate.Date);
                Assert.Equal(10, first.Open);
                Assert.Equal(10, first.High);
                Assert.Equal(7, first.Low);
                Assert.Equal(8, first.Close);
            },
            second =>
            {
                Assert.Equal(new DateTime(2026, 01, 12), second.StartDate.Date);
                Assert.Equal(11, second.Open);
                Assert.Equal(11, second.High);
                Assert.Equal(11, second.Low);
                Assert.Equal(11, second.Close);
            });
    }
}
