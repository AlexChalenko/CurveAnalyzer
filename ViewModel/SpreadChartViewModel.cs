using CurveAnalyzer.Charts;

namespace CurveAnalyzer.ViewModel
{
    public class SpreadChartViewModel : ChartViewModelBase<Periods>
    {
        public SpreadChartViewModel() : base(new DailySpreadChart()) { }

        public override bool CanExecute()
        {
            return Chart.Validate(Parameter) && base.CanExecute();
        }

        public double PeriodFirst
        {
            get
            {
                return Parameter.Period1;
            }
            set
            {
                Parameter = new Periods()
                {
                    Period1 = value,
                    Period2 = PeriodSecond
                };
                //OnPropertyChanged();
            }
        }

        public double PeriodSecond
        {
            get { return Parameter.Period2; }
            set
            {
                Parameter = new Periods()
                {
                    Period1 = PeriodFirst,
                    Period2 = value
                };
                //OnPropertyChanged();
            }
        }


    }
}
