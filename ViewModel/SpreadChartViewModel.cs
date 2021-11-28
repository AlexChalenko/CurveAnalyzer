using CurveAnalyzer.Charts;

namespace CurveAnalyzer.ViewModel
{
    public class SpreadChartViewModel : ChartViewModelBase<Periods>
    {
        public SpreadChartViewModel() : base(new DailySpreadChart())
        {
            Parameter = new Periods();
            Parameter.PropertyChanged += (s, e) =>
            {
                PlotChartCommand.NotifyCanExecuteChanged();
            };
        }

        public override bool CanExecute()
        {
            return Chart.Validate(Parameter) && base.CanExecute();
        }
    }
}
