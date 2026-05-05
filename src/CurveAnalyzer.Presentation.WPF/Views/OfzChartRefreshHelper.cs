using System.Windows.Threading;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.WPF;

namespace CurveAnalyzer.Presentation.WPF.Views;

internal static class OfzChartRefreshHelper
{
    public static void Schedule(CartesianChart chart)
    {
        chart.Dispatcher.BeginInvoke(() => Refresh(chart), DispatcherPriority.Render);
        chart.Dispatcher.BeginInvoke(() => Refresh(chart), DispatcherPriority.ContextIdle);
    }

    private static void Refresh(CartesianChart chart)
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
