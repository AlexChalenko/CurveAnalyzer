using System.Globalization;
using System.Windows.Data;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.Converters;

public sealed class OfzIndexContextValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || value == System.Windows.DependencyProperty.UnsetValue)
        {
            return "n/a";
        }

        return (parameter as string) switch
        {
            "Value" => FormatDouble(value, "N2", culture),
            "ChangePercent" => FormatSignedPercent(value, culture),
            "ChangePoints" => FormatSignedPoints(value, culture),
            "Yield" => FormatDouble(value, "N2", culture),
            "Duration" => FormatDouble(value, "N2", culture),
            "ValueMillion" => FormatMillions(value, culture),
            "Source" => FormatSource(value),
            "Direction" => FormatDirection(value),
            "ReturnKind" => FormatReturnKind(value),
            "Bucket" => FormatBucket(value),
            _ => value.ToString() ?? "n/a"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string FormatDouble(object value, string format, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? number.ToString(format, culture)
            : "n/a";
    }

    private static string FormatSignedPercent(object value, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? number.ToString("+0.00%;-0.00%;0.00%", culture)
            : "n/a";
    }

    private static string FormatSignedPoints(object value, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? number.ToString("+0.00;-0.00;0.00", culture) + " п.п."
            : "n/a";
    }

    private static string FormatMillions(object value, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? (number / 1_000_000).ToString("N0", culture)
            : "n/a";
    }

    private static string FormatSource(object value)
    {
        return value is OfzMarketIndexSourceKind source
            ? source switch
            {
                OfzMarketIndexSourceKind.History => "история",
                OfzMarketIndexSourceKind.Snapshot => "snapshot",
                _ => source.ToString()
            }
            : "n/a";
    }

    private static string FormatDirection(object value)
    {
        return value is OfzMarketIndexDirection direction
            ? direction switch
            {
                OfzMarketIndexDirection.Up => "рост",
                OfzMarketIndexDirection.Down => "снижение",
                OfzMarketIndexDirection.Flat => "без изм.",
                OfzMarketIndexDirection.Mixed => "смешано",
                OfzMarketIndexDirection.Unknown => "n/a",
                _ => direction.ToString()
            }
            : "n/a";
    }

    private static string FormatReturnKind(object value)
    {
        return value is OfzMarketIndexReturnKind kind
            ? kind switch
            {
                OfzMarketIndexReturnKind.Price => "ценовой",
                OfzMarketIndexReturnKind.TotalReturn => "полной доходности",
                _ => kind.ToString()
            }
            : "n/a";
    }

    private static string FormatBucket(object value)
    {
        return value is OfzIndexDurationBucket bucket
            ? bucket switch
            {
                OfzIndexDurationBucket.All => "рынок",
                OfzIndexDurationBucket.UpTo1Y => "до 1Y",
                OfzIndexDurationBucket.OneToThreeY => "1-3Y",
                OfzIndexDurationBucket.ThreeToFiveY => "3-5Y",
                OfzIndexDurationBucket.SevenYPlus => "7Y+",
                OfzIndexDurationBucket.Unknown => "n/a",
                _ => bucket.ToString()
            }
            : "n/a";
    }
}
