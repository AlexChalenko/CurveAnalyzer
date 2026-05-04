using System.Globalization;
using System.Windows.Data;
using CurveAnalyzer.Core;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace CurveAnalyzer.Presentation.WPF.Converters;

public sealed class OfzActivityIndexSeriesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<OfzActivityIndexPoint> points)
        {
            return Array.Empty<ISeries>();
        }

        var orderedPoints = points
            .OrderBy(point => point.TradeDate)
            .ToArray();
        var dates = orderedPoints
            .Select(point => point.TradeDate.Date)
            .ToArray();
        var values = orderedPoints
            .Select((point, index) => new ObservablePoint(index, point.ActivityIndex))
            .ToArray();

        return values.Length == 0
            ? Array.Empty<ISeries>()
            :
            [
                new LineSeries<ObservablePoint>
                {
                    Name = string.Empty,
                    Values = values,
                    GeometrySize = 3,
                    Fill = null,
                    LineSmoothness = 0.35,
                    YToolTipLabelFormatter = point => FormatIndexTooltip(point, orderedPoints, dates)
                }
            ];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string FormatIndexTooltip(
        ChartPoint point,
        IReadOnlyList<OfzActivityIndexPoint> points,
        IReadOnlyList<DateTime> dates)
    {
        var index = GetPointIndex(point, dates.Count);
        if (index < 0)
        {
            return point.Coordinate.PrimaryValue.ToString("N2", CultureInfo.CurrentCulture);
        }

        var indexPoint = points[index];
        return $"{dates[index]:dd.MM}: {indexPoint.ActivityIndex:N2}; выпусков {indexPoint.ActiveIssueCount:N0}; score {indexPoint.MedianActivityScore:N2}";
    }

    private static int GetPointIndex(ChartPoint point, int count)
    {
        if (!double.IsFinite(point.Coordinate.SecondaryValue))
        {
            return -1;
        }

        var index = (int)Math.Round(point.Coordinate.SecondaryValue);
        return index >= 0 && index < count ? index : -1;
    }
}

public sealed class OfzActivityIndexXAxisConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<OfzActivityIndexPoint> points)
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

public sealed class OfzActivityIndexYAxisConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<OfzActivityIndexPoint> points)
        {
            return CreateAxes([]);
        }

        return CreateAxes(points
            .Select(point => point.ActivityIndex)
            .Where(double.IsFinite)
            .ToArray());
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static Axis[] CreateAxes(IReadOnlyList<double> values)
    {
        var limits = GetLimits(values);

        return
        [
            new Axis
            {
                MinLimit = limits.Min,
                MaxLimit = limits.Max,
                TextSize = 8
            }
        ];
    }

    private static (double Min, double Max) GetLimits(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return (0, 1);
        }

        var min = values.Min();
        var max = values.Max();
        var span = Math.Max(max - min, 1d);
        var padding = Math.Max(1d, span * 0.12);

        return (Math.Max(0, min - padding), max + padding);
    }
}

public sealed class OfzDurationYieldScatterXAxisConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<OfzDurationYieldScatterPoint> points)
        {
            return CreateAxes([]);
        }

        return CreateAxes(points
            .Where(point => point.DurationYears > 0)
            .Select(point => point.DurationYears)
            .ToArray());
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static Axis[] CreateAxes(IReadOnlyList<double> values)
    {
        var limits = GetLimits(values, minPadding: 0.25, relativePadding: 0.08, minimumLimit: 0);

        return
        [
            new Axis
            {
                Labeler = value => value.ToString("N1", CultureInfo.CurrentCulture),
                MinLimit = limits.Min,
                MaxLimit = limits.Max,
                TextSize = 8
            }
        ];
    }

    private static (double Min, double Max) GetLimits(
        IReadOnlyList<double> values,
        double minPadding,
        double relativePadding,
        double minimumLimit)
    {
        if (values.Count == 0)
        {
            return (minimumLimit, minimumLimit + 1);
        }

        var min = values.Min();
        var max = values.Max();
        var span = Math.Max(max - min, 1d);
        var padding = Math.Max(minPadding, span * relativePadding);

        return (Math.Max(minimumLimit, min - padding), max + padding);
    }
}

public sealed class OfzDurationYieldScatterYAxisConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<OfzDurationYieldScatterPoint> points)
        {
            return CreateAxes([]);
        }

        return CreateAxes(points
            .Where(point => point.Yield > 0)
            .Select(point => point.Yield)
            .ToArray());
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static Axis[] CreateAxes(IReadOnlyList<double> values)
    {
        var limits = GetLimits(values, minPadding: 0.1, relativePadding: 0.12);

        return
        [
            new Axis
            {
                Labeler = value => value.ToString("N1", CultureInfo.CurrentCulture),
                MinLimit = limits.Min,
                MaxLimit = limits.Max,
                TextSize = 8
            }
        ];
    }

    private static (double Min, double Max) GetLimits(
        IReadOnlyList<double> values,
        double minPadding,
        double relativePadding)
    {
        if (values.Count == 0)
        {
            return (0, 1);
        }

        var min = values.Min();
        var max = values.Max();
        var span = Math.Max(max - min, 1d);
        var padding = Math.Max(minPadding, span * relativePadding);

        return (min - padding, max + padding);
    }
}

public sealed class OfzDurationYieldScatterSeriesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<OfzDurationYieldScatterPoint> points)
        {
            return Array.Empty<ISeries>();
        }

        var orderedPoints = points
            .OrderBy(point => point.DurationYears)
            .ThenBy(point => point.Yield)
            .ToArray();

        return orderedPoints.Length == 0
            ? Array.Empty<ISeries>()
            : orderedPoints
                .GroupBy(point => point.ScoreBucket)
                .OrderBy(group => group.Key)
                .Select(group => new ScatterSeries<ObservablePoint>
                {
                    Name = string.Empty,
                    Values = group
                        .Select(point => new ObservablePoint(point.DurationYears, point.Yield))
                        .ToArray(),
                    Fill = new SolidColorPaint(GetBucketFillColor(group.Key)),
                    GeometrySize = GetGeometrySize(group.Key),
                    Stroke = new SolidColorPaint(GetBucketStrokeColor(group.Key))
                    {
                        StrokeThickness = 1
                    },
                    YToolTipLabelFormatter = chartPoint => FormatScatterTooltip(chartPoint, orderedPoints)
                })
                .Cast<ISeries>()
                .ToArray();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string FormatScatterTooltip(
        ChartPoint chartPoint,
        IReadOnlyList<OfzDurationYieldScatterPoint> points)
    {
        var durationYears = chartPoint.Coordinate.SecondaryValue;
        var yield = chartPoint.Coordinate.PrimaryValue;
        var point = points
            .OrderBy(item => Math.Abs(item.DurationYears - durationYears) + Math.Abs(item.Yield - yield))
            .FirstOrDefault();

        return point is null
            ? $"Дюр. {durationYears:N2}; дох. {yield:N2}"
            : $"{point.ShortName}: дюр. {point.DurationYears:N2}; дох. {point.Yield:N2}; score {point.ActivityScore:N2}";
    }

    private static double GetGeometrySize(int scoreBucket)
    {
        return scoreBucket switch
        {
            >= 5 => 16,
            4 => 13,
            3 => 10,
            2 => 8,
            _ => 6
        };
    }

    private static SKColor GetBucketFillColor(int scoreBucket)
    {
        return scoreBucket switch
        {
            >= 5 => new SKColor(229, 115, 115),
            4 => new SKColor(255, 183, 77),
            3 => new SKColor(255, 213, 79),
            2 => new SKColor(129, 199, 132),
            _ => new SKColor(100, 181, 246)
        };
    }

    private static SKColor GetBucketStrokeColor(int scoreBucket)
    {
        return scoreBucket switch
        {
            >= 5 => new SKColor(198, 40, 40),
            4 => new SKColor(239, 108, 0),
            3 => new SKColor(245, 124, 0),
            2 => new SKColor(67, 160, 71),
            _ => new SKColor(30, 136, 229)
        };
    }
}
