using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.WPF;

namespace CurveAnalyzer.Presentation.WPF.Views;

public partial class OfzActivityControl : UserControl
{
    public OfzActivityControl()
    {
        InitializeComponent();
    }

    private void DetailChart_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is CartesianChart chart)
        {
            ScheduleChartRefresh(chart);
        }
    }

    private void DetailChart_TargetUpdated(object sender, DataTransferEventArgs e)
    {
        if (sender is CartesianChart chart)
        {
            ScheduleChartRefresh(chart);
        }
    }

    private void ScheduleChartRefresh(CartesianChart chart)
    {
        Dispatcher.BeginInvoke(() => RefreshChart(chart), DispatcherPriority.Render);
        Dispatcher.BeginInvoke(() => RefreshChart(chart), DispatcherPriority.ContextIdle);
    }

    private static void RefreshChart(CartesianChart chart)
    {
        if (chart.ActualWidth <= 0 || chart.ActualHeight <= 0)
        {
            return;
        }

        chart.UpdateLayout();
        chart.CoreChart.Update(new ChartUpdateParams
        {
            IsAutomaticUpdate = true,
            Throttling = false
        });
    }
}
