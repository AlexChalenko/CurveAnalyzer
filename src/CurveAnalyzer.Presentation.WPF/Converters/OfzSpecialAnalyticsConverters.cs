using System.Globalization;
using System.Windows.Data;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.Converters;

public sealed class OfzSpecialMetricValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not OfzSpecialMetricValue metric)
        {
            return "n/a";
        }

        return (parameter as string) switch
        {
            "Value" => FormatValue(metric, culture),
            "Availability" => FormatAvailability(metric),
            "ObservedAt" => FormatObservedAt(metric.ObservedAt, culture),
            _ => FormatValue(metric, culture)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string FormatValue(OfzSpecialMetricValue metric, CultureInfo culture)
    {
        if (!metric.Value.HasValue)
        {
            return "n/a";
        }

        var formattedValue = metric.Value.Value.ToString("N2", culture);
        return string.IsNullOrWhiteSpace(metric.Unit)
            ? formattedValue
            : $"{formattedValue} {metric.Unit}";
    }

    private static string FormatAvailability(OfzSpecialMetricValue metric)
    {
        var availabilityText = metric.Availability switch
        {
            OfzSpecialMetricAvailability.Historical => "история",
            OfzSpecialMetricAvailability.SnapshotOnly => "snapshot",
            OfzSpecialMetricAvailability.Provisional => "предварительно",
            OfzSpecialMetricAvailability.InsufficientHistory => "мало истории",
            _ => "нет данных"
        };

        return metric.Source == OfzSpecialMetricSource.CbrKeyRate
            ? $"{availabilityText}, ЦБ"
            : availabilityText;
    }

    private static string FormatObservedAt(DateTime? observedAt, CultureInfo culture)
    {
        return observedAt.HasValue
            ? observedAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", culture)
            : "n/a";
    }
}
