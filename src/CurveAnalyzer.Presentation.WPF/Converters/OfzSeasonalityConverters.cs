using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.Converters;

public sealed class OfzSeasonalityValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || value == DependencyProperty.UnsetValue)
        {
            return "n/a";
        }

        return (parameter as string) switch
        {
            "MoneyMillion" => FormatMillions(value, culture),
            "Ratio" => FormatRatio(value, culture),
            "Percent" => FormatPercent(value, culture),
            "Number" => FormatNumber(value, "N0", culture),
            "Decimal" => FormatNumber(value, "N2", culture),
            "BucketKind" => FormatBucketKind(value),
            "BaselineQuality" => FormatBaselineQuality(value),
            "FindingKind" => FormatFindingKind(value),
            _ => value.ToString() ?? "n/a"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string FormatMillions(object value, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? (number / 1_000_000).ToString("N1", culture)
            : "n/a";
    }

    private static string FormatRatio(object value, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? number.ToString("0.00", culture) + "x"
            : "n/a";
    }

    private static string FormatPercent(object value, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? number.ToString("P0", culture)
            : "n/a";
    }

    private static string FormatNumber(object value, string format, CultureInfo culture)
    {
        return value switch
        {
            double number when double.IsFinite(number) => number.ToString(format, culture),
            int number => number.ToString("N0", culture),
            _ => "n/a"
        };
    }

    private static string FormatBucketKind(object value)
    {
        return value is OfzSeasonalityBucketKind kind
            ? kind switch
            {
                OfzSeasonalityBucketKind.Weekday => "день недели",
                OfzSeasonalityBucketKind.Month => "месяц",
                _ => kind.ToString()
            }
            : "n/a";
    }

    private static string FormatBaselineQuality(object value)
    {
        return value is OfzSeasonalityBaselineQuality quality
            ? quality switch
            {
                OfzSeasonalityBaselineQuality.Strong => "сильная",
                OfzSeasonalityBaselineQuality.Weak => "слабая",
                OfzSeasonalityBaselineQuality.Insufficient => "мало данных",
                _ => quality.ToString()
            }
            : "n/a";
    }

    private static string FormatFindingKind(object value)
    {
        return value is OfzSeasonalityFindingKind kind
            ? kind switch
            {
                OfzSeasonalityFindingKind.HighSeasonalActivity => "выше базы",
                OfzSeasonalityFindingKind.LowSeasonalActivity => "ниже базы",
                OfzSeasonalityFindingKind.SeasonalityDataLimitation => "ограничение",
                _ => kind.ToString()
            }
            : "n/a";
    }
}
