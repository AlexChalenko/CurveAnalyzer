using System.ComponentModel;
using CurveAnalyzer.Charts;
using CurveAnalyzer.Data;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using CurveAnalyzer.Interfaces;

namespace CurveAnalyzer.ViewModel
{
    public class SpreadChartViewModel : ObservableObject, IDataErrorInfo
    {
        public DataManager DataManager { get; }

        private double period1;
        private double period2;

        public DailySpreadChart SpreadChart { get; set; }
        public RelayCommand PlotSpreadCommand { get; }

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

        public SpreadChartViewModel()
        {
            DataManager = (DataManager)App.Current.Services.GetService<IDataManager>();
            DataManager.PropertyChanged += DataManager_PropertyChanged;

            SpreadChart = new DailySpreadChart();
            PlotSpreadCommand = new RelayCommand(() => updateSpreadChart(Period1, Period2), () => SpreadChart.Validate((Period1, Period2)) && !SpreadChart.IsBusy);
            SpreadChart.Setup(new IRelayCommand[] { PlotSpreadCommand });

        }

        private void DataManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(DataManager.IsBusy)))
            {
                //CurveChart.IsBusy = DataManager.IsBusy;
                ////WeeklyChangesChart.IsBusy = DataManager.IsBusy;
                SpreadChart.IsBusy = DataManager.IsBusy;
            }
        }

        private void updateSpreadChart(double period1, double period2)
        {
            SpreadChart.Clear();
            SpreadChart.Plot(DataManager, (period1, period2));
        }

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Period1):
                    case nameof(Period2):
                        if (Period1.Equals(Period2))
                            return "Period should be defferent";
                        break;

                    default:
                        break;
                }
                return string.Empty;
            }
        }
    }
}
