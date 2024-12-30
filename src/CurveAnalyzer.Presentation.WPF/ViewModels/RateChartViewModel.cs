using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CurveAnalyzer.Application;
using CurveAnalyzer.Core;

namespace CurveAnalyzer.Presentation.WPF.ViewModels
{
    public partial class RateChartViewModel : ObservableObject
    {
        private readonly DataSyncService _dataService;

        [ObservableProperty]
        private double _selectedPeriod;

        [ObservableProperty]
        private IEnumerable<Zcyc> _zcycArray;


        [ObservableProperty]
        private ObservableCollection<double> _periods = [];

        public RateChartViewModel(DataSyncService dataService)
        {
            _dataService = dataService;
        }

        public async Task Initialize()
        {
            var periods = await _dataService.GetAvailablePeriodsAsync();

            foreach (var period in periods)
            {
                Periods.Add(period);
            }
        }

        partial void OnSelectedPeriodChanged(double value)
        {
            _dataService.GetZcycForPeriodAsync(value).ContinueWith(t =>
            {
                ZcycArray = new List<Zcyc>(t.Result);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
