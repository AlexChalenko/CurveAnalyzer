using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;
using CurveAnalyzer.Core;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;

namespace CurveAnalyzer.Presentation.WPF.Behaviors;

public static class YieldCurveSeriesBehavior
{
    private static readonly IReadOnlyList<Color> BasePalette =
    [
        Colors.SteelBlue,
        Colors.OrangeRed,
        Colors.SeaGreen,
        Colors.MediumPurple,
        Colors.DarkGoldenrod,
        Colors.IndianRed,
        Colors.Teal,
        Colors.SlateBlue,
        Colors.OliveDrab
    ];

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.RegisterAttached(
            "ItemsSource",
            typeof(IEnumerable<ZcycData>),
            typeof(YieldCurveSeriesBehavior),
            new PropertyMetadata(null, OnItemsSourceChanged));

    private static readonly DependencyProperty CollectionChangedHandlerProperty =
        DependencyProperty.RegisterAttached(
            "CollectionChangedHandler",
            typeof(NotifyCollectionChangedEventHandler),
            typeof(YieldCurveSeriesBehavior),
            new PropertyMetadata(null));

    public static void SetItemsSource(DependencyObject element, IEnumerable<ZcycData>? value)
    {
        element.SetValue(ItemsSourceProperty, value);
    }

    public static IEnumerable<ZcycData>? GetItemsSource(DependencyObject element)
    {
        return (IEnumerable<ZcycData>?)element.GetValue(ItemsSourceProperty);
    }

    private static void OnItemsSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not CartesianChart chart)
        {
            return;
        }

        if (e.OldValue is INotifyCollectionChanged oldCollection
            && GetCollectionChangedHandler(chart) is { } oldHandler)
        {
            oldCollection.CollectionChanged -= oldHandler;
        }

        NotifyCollectionChangedEventHandler? newHandler = null;
        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newHandler = (_, _) => RebuildSeries(chart, GetItemsSource(chart));
            newCollection.CollectionChanged += newHandler;
        }

        SetCollectionChangedHandler(chart, newHandler);
        RebuildSeries(chart, e.NewValue as IEnumerable<ZcycData>);
    }

    private static void RebuildSeries(CartesianChart chart, IEnumerable<ZcycData>? data)
    {
        if (!chart.Dispatcher.CheckAccess())
        {
            _ = chart.Dispatcher.BeginInvoke(() => RebuildSeries(chart, data));
            return;
        }

        if (data == null)
        {
            chart.Series = Array.Empty<ISeries>();
            return;
        }

        var coloring = new SeriesColoring();
        var seriesCollection = new List<ISeries>();
        foreach (var zcyc in data)
        {
            var (stroke, point) = coloring.GetPaintsForDate(zcyc.Date);

            seriesCollection.Add(new LineSeries<ObservablePoint>
            {
                Name = zcyc.Date.ToShortDateString(),
                Values = zcyc.DataRow
                    .Select(dataPoint => new ObservablePoint(dataPoint.Period, dataPoint.Value))
                    .ToArray(),
                Stroke = stroke,
                Fill = null,
                GeometryFill = point,
                GeometryStroke = point,
                GeometrySize = 8,
                LineSmoothness = 0,
                AnimationsSpeed = TimeSpan.Zero
            });
        }

        chart.Series = seriesCollection.Count == 0
            ? Array.Empty<ISeries>()
            : seriesCollection;
    }

    private static void SetCollectionChangedHandler(
        DependencyObject element,
        NotifyCollectionChangedEventHandler? value)
    {
        element.SetValue(CollectionChangedHandlerProperty, value);
    }

    private static NotifyCollectionChangedEventHandler? GetCollectionChangedHandler(DependencyObject element)
    {
        return (NotifyCollectionChangedEventHandler?)element.GetValue(CollectionChangedHandlerProperty);
    }

    private sealed class SeriesColoring
    {
        private readonly List<ColorGroup> _groups = [];
        private int _paletteIndex;

        public (SolidColorPaint Stroke, SolidColorPaint Point) GetPaintsForDate(DateTime date)
        {
            var latestGroup = _groups.OrderByDescending(group => group.AnchorDate).FirstOrDefault();

            ColorGroup targetGroup;
            if (latestGroup == null || date.Date > latestGroup.AnchorDate)
            {
                var baseColor = BasePalette[_paletteIndex % BasePalette.Count];
                _paletteIndex++;
                targetGroup = new ColorGroup(date, baseColor);
                _groups.Add(targetGroup);
            }
            else
            {
                targetGroup = latestGroup;
            }

            int index = targetGroup.Count;
            targetGroup.Count++;

            var stroke = CreatePaint(targetGroup.BaseColor, CalcAlpha(index), 2);
            var point = CreatePaint(targetGroup.BaseColor, CalcAlpha(Math.Max(0, index - 1)), 1);

            return (stroke, point);
        }

        private static SolidColorPaint CreatePaint(Color baseColor, byte alpha, float strokeThickness)
        {
            return new SolidColorPaint(new SKColor(baseColor.R, baseColor.G, baseColor.B, alpha), strokeThickness);
        }

        private static byte CalcAlpha(int index)
        {
            const int step = 36;
            const int min = 60;

            int alpha = 255 - index * step;
            if (alpha < min)
            {
                alpha = min;
            }

            if (alpha > 255)
            {
                alpha = 255;
            }

            return (byte)alpha;
        }
    }

    private sealed class ColorGroup(DateTime anchorDate, Color baseColor)
    {
        public DateTime AnchorDate { get; } = anchorDate.Date;
        public Color BaseColor { get; } = baseColor;
        public int Count { get; set; }
    }
}
