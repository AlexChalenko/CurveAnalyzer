using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CurveAnalyzer.Charts;
using CurveAnalyzer.Interfaces;

namespace CurveAnalyzer.ViewModel
{
    public partial class WeeklyChartViewModel : ChartViewModelBase<double>
    {
        private readonly IDataManager _dataManager;

        [ObservableProperty]
        private ObservableCollection<double> _periods = [];

        public WeeklyChartViewModel(WeeklyRateDynamicChart weeklyRateDynamicChart, IDataManager dataManager) : base(weeklyRateDynamicChart, true)
        {
            _dataManager = dataManager;
        }

        public override async Task Setup()
        {
            try
            {
                var periods = await _dataManager.GetPeriodsAsync();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Periods = new ObservableCollection<double>(periods);
                    Parameter = Periods[0];
                });

            }
            catch (System.Exception ex)
            {

                throw ex;
            }
        }
    }
}
