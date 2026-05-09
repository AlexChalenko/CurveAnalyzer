using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.Converters;

public sealed class OfzCashflowValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || value == DependencyProperty.UnsetValue)
        {
            return "n/a";
        }

        return (parameter as string) switch
        {
            "EventType" => FormatEventType(value),
            "Value" => FormatNumber(value, "N2", culture),
            "Percent" => FormatPercent(value, culture),
            "Source" => FormatSource(value),
            "Days" => FormatDays(value),
            _ => value.ToString() ?? "n/a"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string FormatEventType(object value)
    {
        return value is OfzCashflowEventType eventType
            ? eventType switch
            {
                OfzCashflowEventType.Coupon => "купон",
                OfzCashflowEventType.Amortization => "аморт.",
                OfzCashflowEventType.Maturity => "погаш.",
                OfzCashflowEventType.Offer => "оферта",
                OfzCashflowEventType.Buyback => "buyback",
                OfzCashflowEventType.CallOption => "call",
                OfzCashflowEventType.PutOption => "put",
                _ => eventType.ToString()
            }
            : "n/a";
    }

    private static string FormatSource(object value)
    {
        return value is OfzCashflowSourceKind source
            ? source switch
            {
                OfzCashflowSourceKind.Schedule => "schedule",
                OfzCashflowSourceKind.Snapshot => "snapshot",
                OfzCashflowSourceKind.DescriptionFallback => "fallback",
                _ => source.ToString()
            }
            : "n/a";
    }

    private static string FormatNumber(object value, string format, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? number.ToString(format, culture)
            : "n/a";
    }

    private static string FormatPercent(object value, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? number.ToString("N2", culture) + "%"
            : "n/a";
    }

    private static string FormatDays(object value)
    {
        return value is int days
            ? days switch
            {
                0 => "0",
                > 0 => "+" + days.ToString(CultureInfo.InvariantCulture),
                _ => days.ToString(CultureInfo.InvariantCulture)
            }
            : "n/a";
    }
}
