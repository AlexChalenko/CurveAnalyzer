using System.ComponentModel;
using CurveAnalyzer.Charts;
using CurveAnalyzer.Data;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using OxyPlot;

namespace CurveAnalyzer.ViewModel
{
    public class WeeklyChartViewModel : ObservableObject
    {

        private double periodForWeekChart;
        private PlotModel performanceChart;

        public WeeklyRateDynamicChart WeeklyChangesChart { get; set; }

        public DataManager DataManager { get; set; }

        public RelayCommand PlotWeeklyChartCommand { get; }

        public PlotModel PerformanceChart
        {
            get { return performanceChart; }
            set { SetProperty(ref performanceChart, value); }
        }

        public double PeriodForWeekChart
        {
            get { return periodForWeekChart; }
            set
            {
                if (SetProperty(ref periodForWeekChart, value))
                    PlotWeeklyChartCommand.NotifyCanExecuteChanged();
            }
        }


        public WeeklyChartViewModel(DataManager dataManager)
        {
            DataManager = dataManager;

            WeeklyChangesChart = new WeeklyRateDynamicChart();
            PlotWeeklyChartCommand = new RelayCommand(() => plotWeeklyChart(PeriodForWeekChart), () => !WeeklyChangesChart.IsBusy && DataManager.Periods.Count > 0 && PeriodForWeekChart > 0);
            WeeklyChangesChart.Setup(new IRelayCommand[] { PlotWeeklyChartCommand });

            DataManager.PropertyChanged += DataManager_PropertyChanged;

        }
        private void DataManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(DataManager.IsBusy)))
            {
                WeeklyChangesChart.IsBusy = DataManager.IsBusy;
            }
        }

        private void plotWeeklyChart(double period)
        {
            WeeklyChangesChart.Clear();
            WeeklyChangesChart.Plot(DataManager, period);
        }
    }
}
