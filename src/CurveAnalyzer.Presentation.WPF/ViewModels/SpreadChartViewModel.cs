using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CurveAnalyzer.Application;
using CurveAnalyzer.Presentation.WPF.Data;

namespace CurveAnalyzer.Presentation.WPF.ViewModels
{
    public partial class SpreadChartViewModel : ObservableObject
    {
        private readonly DataSyncService _dataService;

        [ObservableProperty]
        private ObservableCollection<double> _periodsList = [];

        [ObservableProperty]
        private Periods _periods = new();

        [ObservableProperty]
        private double _periodFirst;

        [ObservableProperty]
        private double _periodSecond;

        [ObservableProperty]
        private List<ZcycPoint> _values = [];

        public SpreadChartViewModel(DataSyncService dataService)
        {
            _dataService = dataService;
        }

        public async Task Initialize()
        {
            var periods = await _dataService.GetAvailablePeriodsAsync();

            foreach (var period in periods)
            {
                PeriodsList.Add(period);
            }
        }

        private async Task GetData()
        {
            var data1 = await _dataService.GetZcycForPeriodAsync(PeriodFirst);
            var data2 = await _dataService.GetZcycForPeriodAsync(PeriodSecond);

            var query = data1.Join(data2,
                                  d1 => d1.Tradedate,
                                  d2 => d2.Tradedate,
                                  (d1, d2) => new ZcycPoint(d1.Tradedate, d2.Value - d1.Value)).ToList();

            Values = query;
        }

        partial void OnPeriodFirstChanged(double value)
        {
            Periods.Period1 = value;

            if (!Periods.IsEmpty)
            {
                GetData().ConfigureAwait(false);
            }
        }

        partial void OnPeriodSecondChanged(double value)
        {
            Periods.Period2 = value;

            if (!Periods.IsEmpty)
            {
                GetData().ConfigureAwait(false);
            }
        }
    }
}
