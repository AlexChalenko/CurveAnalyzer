using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using CurveAnalyzer.Charts;
using CurveAnalyzer.Data;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using OxyPlot;

namespace CurveAnalyzer
{
    internal class MainViewModel : ObservableObject
    {
        private PlotModel performanceChart;
        private double period1;
        private double period2;
        private double periodForWeekChart;
        private string status;
        public DailyCurveChart CurveChart { get; set; }
        public WeeklyRateDynamicChart WeeklyChangesChart { get; set; }
        public DailySpreadChart SpreadChart { get; set; }

        public DataManager DataManager { get; set; }

        public PlotModel PerformanceChart
        {
            get { return performanceChart; }
            set { SetProperty(ref performanceChart, value); }
        }

        public double Period1
        {
            get { return period1; }
            set
            {
                if (SetProperty(ref period1, value))
                    PlotSpreadCommand.NotifyCanExecuteChanged();
            }
        }

        public double Period2
        {
            get { return period2; }
            set
            {
                if (SetProperty(ref period2, value))
                    PlotSpreadCommand.NotifyCanExecuteChanged();
            }
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

        public string Status
        {
            get => status;
            set
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SetProperty(ref status, value);
                });
            }
        }

        public AsyncRelayCommand PlotDailyChartCommand { get; }
        public RelayCommand PlotWeeklyChartCommand { get; }
        public RelayCommand ClearDailyChartCommand { get; }
        public RelayCommand PlotSpreadCommand { get; }

        public MainViewModel()
        {
            DataManager = new DataManager();
            CurveChart = new DailyCurveChart();
            WeeklyChangesChart = new WeeklyRateDynamicChart();
            SpreadChart = new DailySpreadChart();

            PlotDailyChartCommand = new AsyncRelayCommand(() => plotDailyChart(DataManager.SelectedDate), () => !CurveChart.IsBusy);
            ClearDailyChartCommand = new RelayCommand(clearDailyChart, () => !CurveChart.IsBusy);
            PlotWeeklyChartCommand = new RelayCommand(() => plotWeeklyChart(PeriodForWeekChart), () => !WeeklyChangesChart.IsBusy && DataManager.Periods.Count > 0 && PeriodForWeekChart > 0);
            PlotSpreadCommand = new RelayCommand(() => updateSpreadChart(Period1, Period2), () => SpreadChart.Validate((Period1, Period2)) && !SpreadChart.IsBusy);

            CurveChart.Setup(new IRelayCommand[] { PlotDailyChartCommand, ClearDailyChartCommand });
            WeeklyChangesChart.Setup(new IRelayCommand[] { PlotWeeklyChartCommand });
            SpreadChart.Setup(new IRelayCommand[] { PlotSpreadCommand });

            DataManager.PropertyChanged += DataManager_PropertyChanged;
            DataManager.Initialize();
        }

        private void DataManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(DataManager.IsBusy)))
            {
                CurveChart.IsBusy = DataManager.IsBusy;
                WeeklyChangesChart.IsBusy = DataManager.IsBusy;
                SpreadChart.IsBusy = DataManager.IsBusy;
            }
        }

        private void plotWeeklyChart(double period)
        {
            WeeklyChangesChart.Clear();
            WeeklyChangesChart.Plot(DataManager, period);
        }

        private void clearDailyChart()
        {
            CurveChart.Clear();
        }

        private Task plotDailyChart(DateTime dateTime)
        {
            CurveChart.Plot(DataManager, dateTime);
            return Task.CompletedTask;
        }

        private void updateSpreadChart(double period1, double period2)
        {
            SpreadChart.Clear();
            SpreadChart.Plot(DataManager, (period1, period2));
        }
    }
}
