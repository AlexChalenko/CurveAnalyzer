using CurveAnalyzer.Interfaces;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace CurveAnalyzer.ViewModel
{
    public abstract class ChartViewModelBase<T> : ObservableObject
    {
        private T parameter = default;

        public ChartBase<T> Chart { get; set; }
        public IRelayCommand PlotChartCommand { get; }
        public IRelayCommand ClearChartCommand { get; }

        public virtual T Parameter
        {
            get { return parameter; }
            set
            {
                if (SetProperty(ref parameter, value))
                {
                    PlotChartCommand.NotifyCanExecuteChanged();
                }
            }
        }
        protected ChartViewModelBase(ChartBase<T> Chart)
        {
            PlotChartCommand = new RelayCommand(() => PlotChart(Parameter), CanExecute);
            ClearChartCommand = new RelayCommand(Chart.Clear);
            this.Chart = Chart;
            Chart.Setup(new IRelayCommand[] { PlotChartCommand, ClearChartCommand });
        }

        public virtual void PlotChart(T parameter)
        {
            Chart.Clear();
            Chart.Plot(parameter);
        }

        public virtual bool CanExecute() => !Chart.IsBusy;
    }
}
