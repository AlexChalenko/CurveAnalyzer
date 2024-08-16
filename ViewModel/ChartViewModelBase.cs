using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurveAnalyzer.Interfaces;

namespace CurveAnalyzer.ViewModel
{
    public abstract partial class ChartViewModelBase<T> : ObservableObject
    {

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlotChartCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearChartCommand))]
        private T parameter = default;

        [ObservableProperty]
        private ChartBase<T> _chart;
        private readonly bool _clearChart;

        protected ChartViewModelBase(ChartBase<T> Chart, bool clearChart)
        {
            _chart = Chart;
            _clearChart = clearChart;
            Chart.Setup([PlotChartCommand, ClearChartCommand]);
        }

        [RelayCommand(CanExecute = nameof(CheckCanExecute))]
        private void PlotChart(T parameter)
        {
            if (_clearChart)
            {
                ClearChart();
            }

            Chart.Plot(parameter);
        }

        [RelayCommand(CanExecute = nameof(CheckCanExecute))]
        private void ClearChart()
        {
            Chart.Clear();
        }

        private bool CheckCanExecute()
        {
            return CanExecuteInternal;
        }
        private protected bool CanExecuteInternal => true;

        partial void OnParameterChanged(T value)
        {
            OnParameterChange(value);
        }

        public virtual void OnParameterChange(T value)
        {
            PlotChart(value);
        }

        public abstract Task Setup();
    }
}
