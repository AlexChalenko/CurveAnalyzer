using System.Globalization;
using System.Windows.Data;
using CurveAnalyzer.Core;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace CurveAnalyzer.Presentation.WPF.Converters;

public sealed class RateValuesToSeriesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<Zcyc> points)
        {
            return Array.Empty<ISeries>();
        }

        var pointList = points.ToList();
        if (pointList.Count == 0)
        {
            return Array.Empty<ISeries>();
        }

        var series = new HistoricalSeries(
            new CurvePeriod(pointList[0].Period),
            pointList.Select(point => new HistoricalPoint(new TradingDate(point.Tradedate), point.Value)));

        var output = RateSeriesGrouper.GroupWeekly(series)
            .Select(point => new FinancialPoint(
                point.StartDate.Date,
                point.High,
                point.Open,
                point.Close,
                point.Low))
            .ToArray();

        return new ISeries[]
        {
            new CandlesticksSeries<FinancialPoint>
            {
                Values = output
            }
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
