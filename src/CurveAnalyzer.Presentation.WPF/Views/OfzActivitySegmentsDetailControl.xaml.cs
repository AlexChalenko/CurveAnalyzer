using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LiveChartsCore.SkiaSharpView.WPF;

namespace CurveAnalyzer.Presentation.WPF.Views;

public partial class OfzActivitySegmentsDetailControl : UserControl
{
    public OfzActivitySegmentsDetailControl()
    {
        InitializeComponent();
    }

    private void DetailChart_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is CartesianChart chart)
        {
            chart.SizeChanged -= DetailChart_SizeChanged;
            chart.SizeChanged += DetailChart_SizeChanged;
            OfzChartRefreshHelper.Schedule(chart);
        }
    }

    private void DetailChart_TargetUpdated(object sender, DataTransferEventArgs e)
    {
        if (sender is CartesianChart chart)
        {
            OfzChartRefreshHelper.Schedule(chart);
        }
    }

    private void DetailChart_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is CartesianChart chart && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            OfzChartRefreshHelper.Schedule(chart);
        }
    }
}
