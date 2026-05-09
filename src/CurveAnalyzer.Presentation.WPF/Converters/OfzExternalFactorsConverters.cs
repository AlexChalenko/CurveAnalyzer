using System.Globalization;
using System.Windows.Data;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.Converters;

public sealed class OfzExternalFactorsValueConverter : IValueConverter
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
            "Change" => FormatSignedDouble(value, culture),
            "ChangePercent" => FormatSignedPercent(value, culture),
            "Kind" => FormatKind(value),
            "Scope" => FormatScope(value),
            "Source" => FormatSource(value),
            "Availability" => FormatAvailability(value),
            "Direction" => FormatDirection(value),
            "LinkKind" => FormatLinkKind(value),
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

    private static string FormatSignedDouble(object value, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? number.ToString("+0.00;-0.00;0.00", culture)
            : "n/a";
    }

    private static string FormatSignedPercent(object value, CultureInfo culture)
    {
        return value is double number && double.IsFinite(number)
            ? number.ToString("+0.00%;-0.00%;0.00%", culture)
            : "n/a";
    }

    private static string FormatKind(object value)
    {
        return value is OfzExternalFactorKind kind
            ? kind switch
            {
                OfzExternalFactorKind.PolicyRate => "ставка",
                OfzExternalFactorKind.MarketIndex => "индекс",
                OfzExternalFactorKind.DerivedOfzMetric => "расчет",
                OfzExternalFactorKind.Currency => "валюта",
                OfzExternalFactorKind.Commodity => "товар",
                _ => kind.ToString()
            }
            : "n/a";
    }

    private static string FormatScope(object value)
    {
        return value is OfzExternalFactorScope scope
            ? scope switch
            {
                OfzExternalFactorScope.Market => "рынок",
                OfzExternalFactorScope.Segment => "сегмент",
                OfzExternalFactorScope.Issue => "выпуск",
                OfzExternalFactorScope.Derived => "derived",
                _ => scope.ToString()
            }
            : "n/a";
    }

    private static string FormatSource(object value)
    {
        return value is OfzExternalFactorSource source
            ? source switch
            {
                OfzExternalFactorSource.CbrKeyRate => "ЦБ",
                OfzExternalFactorSource.MoexIndex => "MOEX",
                OfzExternalFactorSource.OfzDerived => "расчет",
                OfzExternalFactorSource.Missing => "n/a",
                _ => source.ToString()
            }
            : "n/a";
    }

    private static string FormatAvailability(object value)
    {
        return value is OfzExternalFactorAvailability availability
            ? availability switch
            {
                OfzExternalFactorAvailability.Historical => "история",
                OfzExternalFactorAvailability.SnapshotOnly => "snapshot",
                OfzExternalFactorAvailability.Provisional => "предварит.",
                OfzExternalFactorAvailability.InsufficientHistory => "мало истории",
                OfzExternalFactorAvailability.Missing => "нет данных",
                _ => availability.ToString()
            }
            : "n/a";
    }

    private static string FormatDirection(object value)
    {
        return value is OfzMarketIndexDirection direction
            ? direction switch
            {
                OfzMarketIndexDirection.Up => "рост",
                OfzMarketIndexDirection.Down => "сниж.",
                OfzMarketIndexDirection.Flat => "flat",
                OfzMarketIndexDirection.Mixed => "смеш.",
                OfzMarketIndexDirection.Unknown => "n/a",
                _ => direction.ToString()
            }
            : "n/a";
    }

    private static string FormatLinkKind(object value)
    {
        return value is OfzExternalFactorLinkKind kind
            ? kind switch
            {
                OfzExternalFactorLinkKind.ActivityWithFactorMove => "активность + фактор",
                OfzExternalFactorLinkKind.ActivityWithoutFactorMove => "активность без фактора",
                OfzExternalFactorLinkKind.YieldMoveWithFactorMove => "yield + фактор",
                OfzExternalFactorLinkKind.MissingFactor => "нет фактора",
                _ => kind.ToString()
            }
            : "n/a";
    }
}
