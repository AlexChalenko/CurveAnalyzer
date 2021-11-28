using CurveAnalyzer.Charts;

namespace CurveAnalyzer.ViewModel
{
    public class WeeklyChartViewModel : ChartViewModelBase<double>
    {
        public WeeklyChartViewModel() : base(new WeeklyRateDynamicChart()) { }

        public override bool CanExecute() => base.CanExecute() && Chart.DataManager.Periods.Count > 0 && Parameter > 0;
    }
}
