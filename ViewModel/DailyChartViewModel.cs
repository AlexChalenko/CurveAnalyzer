using System;
using System.Threading.Tasks;
using CurveAnalyzer.Charts;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace CurveAnalyzer.ViewModel
{
    public class DailyChartViewModel : ObservableObject
    {
        public DailyCurveChart Chart { get; set; }
        public AsyncRelayCommand PlotDailyChartCommand { get; }
        public RelayCommand ClearDailyChartCommand { get; }

        public DailyChartViewModel() : base()
        {
            Chart = new DailyCurveChart();
            PlotDailyChartCommand = new AsyncRelayCommand(() => plotDailyChart(Chart.DataManager.SelectedDate), () => !Chart.IsBusy);
            ClearDailyChartCommand = new RelayCommand(clearDailyChart, () => !Chart.IsBusy);

            Chart.Setup(new IRelayCommand[] { PlotDailyChartCommand, ClearDailyChartCommand });
        }

        private void clearDailyChart()
        {
            Chart.Clear();
        }

        private Task plotDailyChart(DateTime dateTime)
        {
            return Chart.Plot(dateTime);
        }

        public string ButtonName { get; } = "Получить данные";
    }
}
