using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CurveAnalyzer.Core;
using CurveAnalyzer.Presentation.WPF.ViewModels;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace CurveAnalyzer.Presentation.WPF.Views;

public partial class YieldCurveControl : UserControl
{
    private YieldCurveViewModel? _viewModel;
    private readonly List<Color> _basePalette = new()
    {
        Colors.SteelBlue,
        Colors.OrangeRed,
        Colors.SeaGreen,
        Colors.MediumPurple,
        Colors.DarkGoldenrod,
        Colors.IndianRed,
        Colors.Teal,
        Colors.SlateBlue,
        Colors.OliveDrab
    };

    private int _paletteIndex = 0;

    private class ColorGroup
    {
        public DateTime AnchorDate { get; }
        public Color BaseColor { get; }
        public int Count { get; set; }

        public ColorGroup(DateTime anchorDate, Color baseColor)
        {
            AnchorDate = anchorDate.Date;
            BaseColor = baseColor;
            Count = 0;
        }
    }

    private readonly List<ColorGroup> _groups = new();

    public YieldCurveControl(/*YieldCurveViewModel wdw*/)
    {
        InitializeComponent();
        //DataContext = _viewModel= wdw;
        DataContextChanged += YieldCurveControl_DataContextChanged;
    }

    private void YieldCurveControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is YieldCurveViewModel model)
        {
            _viewModel = model;
            _viewModel.BlackoutDates.CollectionChanged += BlackoutDates_CollectionChanged;
            _viewModel.ZcycDatas.CollectionChanged += ZcycDatas_CollectionChanged;
            //_viewModel.Initialize();
            UpdateChart(_viewModel.ZcycDatas);
        }
    }

    private void UpdateChart(IEnumerable<ZcycData> data)
    {
        if (data == null)
        {
            YieldChart.Series.Clear();
            ResetColoring();
            return;
        }

        foreach (var zcyc in data)
        {
            var (stroke, point) = GetBrushesForDate(zcyc.Date);

            var series = new LineSeries()
            {
                Title = zcyc.Date.ToShortDateString(),
                Values = new ChartValues<ObservablePoint>(zcyc.DataRow.Select(d => new ObservablePoint(d.Period, d.Value))),
                Stroke = stroke,
                Fill = Brushes.Transparent,
                PointForeground = point,
                LineSmoothness = 0,
                StrokeThickness = 2
            };
            YieldChart.Series.Add(series);
        }
    }

    private void ZcycDatas_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null)
        {
            YieldChart.Series.Clear();
            ResetColoring();
        }
        else
        {
            var newList = new List<ZcycData>();

            for (int i = 0; i < e.NewItems.Count; i++)
            {
                if (e.NewItems[i] is ZcycData zcyc)
                {
                    newList.Add(zcyc);
                    //var series = new LineSeries()
                    //{
                    //    Title = zcyc.Date.ToShortDateString(),
                    //    Values = new ChartValues<ObservablePoint>(zcyc.DataRow.Select(d => new ObservablePoint(d.Period, d.Value)))
                    //};
                    //YieldChart.Series.Add(series);
                }
            }

            UpdateChart(newList);
        }
    }

    private void BlackoutDates_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null)
        {
            return;
        }

        foreach (var item in e.NewItems)
        {
            if (item is DateTime date)
            {
                App.Current.Dispatcher.BeginInvoke(() =>
                {
                    MainDatePicker.BlackoutDates.Add(new CalendarDateRange(date));
                });
            }
        }
    }

    private void ResetColoring()
    {
        _groups.Clear();
        _paletteIndex = 0;
    }

    private (Brush stroke, Brush point) GetBrushesForDate(DateTime date)
    {
        // Determine target group: latest anchor group unless incoming date is newer than all anchors
        var latestGroup = _groups.OrderByDescending(g => g.AnchorDate).FirstOrDefault();

        ColorGroup targetGroup;
        if (latestGroup == null || date.Date > latestGroup.AnchorDate)
        {
            // Start a new group with a fresh base color
            var baseColor = _basePalette[_paletteIndex % _basePalette.Count];
            _paletteIndex++;
            targetGroup = new ColorGroup(date, baseColor);
            _groups.Add(targetGroup);
        }
        else
        {
            targetGroup = latestGroup;
        }

        // Compute alpha based on how many lines already drawn in this group
        // First (anchor) line uses full opacity, then fade by steps
        int index = targetGroup.Count; // 0 for first, 1 for next, etc.
        targetGroup.Count++;

        byte alpha = CalcAlpha(index);
        var strokeColor = Color.FromArgb(alpha, targetGroup.BaseColor.R, targetGroup.BaseColor.G, targetGroup.BaseColor.B);
        var stroke = new SolidColorBrush(strokeColor);
        stroke.Freeze();

        // Use slightly stronger alpha for points to remain visible
        byte pointAlpha = CalcAlpha(Math.Max(0, index - 1));
        var pointColor = Color.FromArgb(pointAlpha, targetGroup.BaseColor.R, targetGroup.BaseColor.G, targetGroup.BaseColor.B);
        var point = new SolidColorBrush(pointColor);
        point.Freeze();

        return (stroke, point);
    }

    private static byte CalcAlpha(int index)
    {
        // 0 -> 255, then subtract step; clamp to a minimum for visibility
        const int step = 36; // ~7 steps until minimum
        const int min = 60;
        int a = 255 - index * step;
        if (a < min) a = min;
        if (a > 255) a = 255;
        return (byte)a;
    }
}
