using System.Windows;
using System.Windows.Controls;
using CurveAnalyzer.Core;
using CurveAnalyzer.Presentation.WPF.ViewModels;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace CurveAnalyzer.Presentation.WPF.Views
{
    /// <summary>
    /// Interaction logic for RateChartControl.xaml
    /// </summary>
    public partial class RateChartControl : UserControl
    {
        private RateChartViewModel? _model;

        public RateChartControl(/*RateChartViewModel rateChartViewModel*/)
        {
            InitializeComponent();
            DataContextChanged += RateChartControl_DataContextChanged;
            //DataContext = _model = rateChartViewModel;
        }

        private async void RateChartControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && e.NewValue is RateChartViewModel model)
            {
                _model = model;
                _model.PropertyChanged += Model_PropertyChanged;
                await _model.Initialize();
            }
        }

        private void Model_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_model.ZcycArray))
            {
                if (_model?.ZcycArray == null || _model.SelectedPeriod <= 0)
                {
                    return;
                }

                var series = new HistoricalSeries(
                    new CurvePeriod(_model.SelectedPeriod),
                    _model.ZcycArray.Select(point => new HistoricalPoint(new TradingDate(point.Tradedate), point.Value)));

                var output = RateSeriesGrouper.GroupWeekly(series)
                    .Select(point => new OhlcPoint
                    {
                        Open = point.Open,
                        High = point.High,
                        Low = point.Low,
                        Close = point.Close
                    })
                    .ToList();

                var seriesCollection = new SeriesCollection
                {
                    new OhlcSeries()
                    {
                        Values = new ChartValues<OhlcPoint>(output)
                    }
                };

                RateChart.Series = seriesCollection;
            }
            ;
        }
    }
}
