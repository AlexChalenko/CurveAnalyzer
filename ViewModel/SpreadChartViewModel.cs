using System.ComponentModel;
using CurveAnalyzer.Charts;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
using CurveAnalyzer.View;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace CurveAnalyzer.ViewModel
{
    public class SpreadChartViewModel : ObservableObject, IDataErrorInfo
    {
        private double period1;
        private double period2;

        private Periods periods;

        public DailySpreadChart Chart { get; set; }
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
            Chart = new DailySpreadChart();
            PlotSpreadCommand = new RelayCommand(() => plotSpreadChart(new Periods { Period1 = Period1, Period2 = Period2 }) , () => Chart.Validate(new Periods { Period1= Period1, Period2= Period2 }) && !Chart.IsBusy);
            Chart.Setup(new IRelayCommand[] { PlotSpreadCommand });
        }

        private void plotSpreadChart(Periods periods)
        {
            Chart.Clear();
            Chart.Plot(periods);
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
