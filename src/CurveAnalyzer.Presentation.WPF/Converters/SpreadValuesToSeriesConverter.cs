using System.Globalization;
using System.Windows.Data;
using CurveAnalyzer.Presentation.WPF.Data;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace CurveAnalyzer.Presentation.WPF.Converters;

public sealed class SpreadValuesToSeriesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<ZcycPoint> points)
        {
            return Array.Empty<ISeries>();
        }

        var values = points
            .Select(point => new DateTimePoint(point.date, point.Value))
            .ToArray();

        if (values.Length == 0)
        {
            return Array.Empty<ISeries>();
        }

        return new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = values
            }
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
