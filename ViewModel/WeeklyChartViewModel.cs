using CurveAnalyzer.Charts;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using OxyPlot;

namespace CurveAnalyzer.ViewModel
{
    public class WeeklyChartViewModel : ObservableObject
    {
        private double period;
        public WeeklyRateDynamicChart Chart { get; set; }
        public RelayCommand PlotWeeklyChartCommand { get; }

        public double Period
        {
            get { return period; }
            set
            {
                if (SetProperty(ref period, value))
                    PlotWeeklyChartCommand.NotifyCanExecuteChanged();
            }
        }

        public WeeklyChartViewModel()
        {
            Chart = new WeeklyRateDynamicChart();
            PlotWeeklyChartCommand = new RelayCommand(() => plotWeeklyChart(Period), () => !Chart.IsBusy && Chart.DataManager.Periods.Count > 0 && Period > 0);
            Chart.Setup(new IRelayCommand[] { PlotWeeklyChartCommand });
        }

        private void plotWeeklyChart(double period)
        {
            Chart.Clear();
            Chart.Plot(period);
        }
    }
}
