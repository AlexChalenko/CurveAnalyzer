using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CurveAnalyzer.Charts;
using CurveAnalyzer.Data;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace CurveAnalyzer.ViewModel
{
    public class DailyChartViewModel : ObservableObject
    {
        public DailyCurveChart CurveChart { get; set; }
        public AsyncRelayCommand PlotDailyChartCommand { get; }
        public RelayCommand ClearDailyChartCommand { get; }

        public DataManager DataManager { get; set; }

        public DailyChartViewModel(DataManager dataManager)
        {
            DataManager = dataManager;

            DataManager.PropertyChanged += DataManager_PropertyChanged;

            CurveChart = new DailyCurveChart();

            PlotDailyChartCommand = new AsyncRelayCommand(() => plotDailyChart(DataManager.SelectedDate), () => !CurveChart.IsBusy);
            ClearDailyChartCommand = new RelayCommand(clearDailyChart, () => !CurveChart.IsBusy);



            CurveChart.Setup(new IRelayCommand[] { PlotDailyChartCommand, ClearDailyChartCommand });
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
        private void DataManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(DataManager.IsBusy)))
            {
                CurveChart.IsBusy = DataManager.IsBusy;
                //WeeklyChangesChart.IsBusy = DataManager.IsBusy;
                //SpreadChart.IsBusy = DataManager.IsBusy;
            }
        }

        private string buttonName = "Получить данные";

        public string ButtonName { get => buttonName; set => SetProperty(ref buttonName, value); }
    }
}
