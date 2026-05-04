using System.Globalization;
using CurveAnalyzer.Core;
using System.Windows.Data;
using System.Windows.Media;

namespace CurveAnalyzer.Presentation.WPF.Converters;

public sealed class ActivityHeatmapColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            null => Brushes.Transparent,
            OfzActivityHeatmapCell cell => ConvertCell(cell),
            int bucket => ConvertBucket(bucket),
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static Brush ConvertCell(OfzActivityHeatmapCell cell)
    {
        return cell.Status switch
        {
            OfzActivityMetricStatus.NoData => Brushes.WhiteSmoke,
            OfzActivityMetricStatus.InsufficientBaseline => Brushes.LightSteelBlue,
            OfzActivityMetricStatus.MissingBaseline => Brushes.LightGray,
            OfzActivityMetricStatus.MissingValue => Brushes.Gainsboro,
            _ => ConvertBucket(cell.ScoreBucket)
        };
    }

    private static Brush ConvertBucket(int bucket)
    {
        return bucket switch
        {
            >= 5 => Brushes.Firebrick,
            4 => Brushes.Tomato,
            3 => Brushes.Gold,
            2 => Brushes.PaleGreen,
            1 => Brushes.Honeydew,
            _ => Brushes.WhiteSmoke
        };
    }
}
