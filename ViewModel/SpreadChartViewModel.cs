using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CurveAnalyzer.Charts;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;

namespace CurveAnalyzer.ViewModel
{
    public partial class SpreadChartViewModel : ChartViewModelBase<Periods>
    {
        public SpreadChartViewModel(DailySpreadChart dailySpreadChart, IDataManager dataManager) : base(dailySpreadChart, true)
        {
            _dataManager = dataManager;
        }

        [ObservableProperty]
        private ObservableCollection<double> _periods = [];

        [ObservableProperty]
        private double _periodFirst;

        [ObservableProperty]
        private double _periodSecond;

        private readonly IDataManager _dataManager;

        private new bool CanExecuteInternal => Chart.Validate(Parameter);

        public override async Task Setup()
        {
            var rrr = await _dataManager.GetPeriodsAsync();

            App.Current.Dispatcher.Invoke(() =>
            {
                Periods = new(rrr);
            });
        }

        partial void OnPeriodFirstChanged(double value)
        {
            Parameter = new Periods()
            {
                Period1 = value,
                Period2 = PeriodSecond
            };
        }

        partial void OnPeriodSecondChanged(double value)
        {
            Parameter = new Periods()
            {
                Period1 = PeriodFirst,
                Period2 = value
            };
        }

        //public double PeriodFirst
        //{
        //    get
        //    {
        //        return Parameter.Period1;
        //    }
        //    set
        //    {
        //        Parameter = new Periods()
        //        {
        //            Period1 = value,
        //            Period2 = PeriodSecond
        //        };
        //        //OnPropertyChanged();
        //    }
        //}

        //public double PeriodSecond
        //{
        //    get { return Parameter.Period2; }
        //    set
        //    {
        //        Parameter = new Periods()
        //        {
        //            Period1 = PeriodFirst,
        //            Period2 = value
        //        };
        //        //OnPropertyChanged();
        //    }
        //}


    }
}
