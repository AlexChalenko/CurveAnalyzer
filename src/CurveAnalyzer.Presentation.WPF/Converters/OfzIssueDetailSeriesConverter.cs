using System.Globalization;
using System.Windows.Data;
using CurveAnalyzer.Core;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;

namespace CurveAnalyzer.Presentation.WPF.Converters;

public sealed class OfzIssueDetailSeriesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<OfzIssueDetailPoint> points)
        {
            return Array.Empty<ISeries>();
        }

        var mode = parameter as string ?? "Value";
        var orderedPoints = points
            .OrderBy(point => point.TradeDate)
            .ToArray();
        var dates = orderedPoints
            .Select(point => point.TradeDate.Date)
            .ToArray();
        var values = orderedPoints
            .Select((point, index) => ToChartPoint(point, mode, index))
            .Where(point => point is not null)
            .Cast<ObservablePoint>()
            .ToArray();

        if (values.Length == 0)
        {
            return Array.Empty<ISeries>();
        }

        var seriesName = GetSeriesName(mode);
        if (mode is "Value" or "NumTrades")
        {
            return new ISeries[]
            {
                new ColumnSeries<ObservablePoint>
                {
                    Name = seriesName,
                    Values = values,
                    MaxBarWidth = 10,
                    YToolTipLabelFormatter = point => FormatTooltipValue(point, dates, mode)
                }
            };
        }

        return new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Name = seriesName,
                Values = values,
                GeometrySize = 3,
                Fill = null,
                YToolTipLabelFormatter = point => FormatTooltipValue(point, dates, mode)
            }
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static ObservablePoint? ToChartPoint(OfzIssueDetailPoint point, string mode, int index)
    {
        double? value = mode switch
        {
            "NumTrades" => point.NumTrades,
            "Price" => point.Price,
            "Yield" => point.Yield,
            _ => point.Value.HasValue ? point.Value.Value / 1_000_000 : null
        };

        return value.HasValue
            ? new ObservablePoint(index, value.Value)
            : null;
    }

    private static string GetSeriesName(string mode)
    {
        return mode switch
        {
            "NumTrades" => "Сделки",
            "Price" => "Цена",
            "Yield" => "Доходность",
            _ => "Оборот, млн RUB"
        };
    }

    private static string FormatTooltipValue(ChartPoint point, IReadOnlyList<DateTime> dates, string mode)
    {
        var date = FormatDate(point, dates);
        var value = point.Coordinate.PrimaryValue.ToString(GetValueFormat(mode), CultureInfo.CurrentCulture);

        return string.IsNullOrWhiteSpace(date)
            ? value
            : $"{date}: {value}";
    }

    private static string FormatDate(ChartPoint point, IReadOnlyList<DateTime> dates)
    {
        if (!double.IsFinite(point.Coordinate.SecondaryValue))
        {
            return string.Empty;
        }

        var index = (int)Math.Round(point.Coordinate.SecondaryValue);
        return index >= 0 && index < dates.Count
            ? dates[index].ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)
            : string.Empty;
    }

    private static string GetValueFormat(string mode)
    {
        return mode switch
        {
            "NumTrades" => "N0",
            "Price" or "Yield" => "N2",
            _ => "N2"
        };
    }
}

public sealed class OfzIssueDetailXAxisConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<OfzIssueDetailPoint> points)
        {
            return CreateAxes([]);
        }

        var labels = points
            .OrderBy(point => point.TradeDate)
            .Select(point => point.TradeDate.ToString("dd.MM", culture))
            .ToArray();

        return CreateAxes(labels);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static Axis[] CreateAxes(IReadOnlyList<string> labels)
    {
        var step = Math.Max(1, (int)Math.Ceiling(labels.Count / 5d));

        return
        [
            new Axis
            {
                Labeler = value => FormatLabel(value, labels, step),
                MinLimit = labels.Count == 0 ? 0 : -0.5,
                MaxLimit = labels.Count == 0 ? 1 : labels.Count - 0.5,
                MinStep = 1,
                UnitWidth = 1,
                TextSize = 9,
                LabelsRotation = 0
            }
        ];
    }

    private static string FormatLabel(double value, IReadOnlyList<string> labels, int step)
    {
        if (!double.IsFinite(value))
        {
            return string.Empty;
        }

        var index = (int)Math.Round(value);
        if (index < 0 || index >= labels.Count || Math.Abs(value - index) > 0.001)
        {
            return string.Empty;
        }

        return index % step == 0 || index == labels.Count - 1
            ? labels[index]
            : string.Empty;
    }
}
