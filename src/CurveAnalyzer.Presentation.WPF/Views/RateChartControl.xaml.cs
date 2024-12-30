using System.Windows;
using System.Windows.Controls;
using CurveAnalyzer.Application.Tools;
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
        private RateChartViewModel _model;

        public RateChartControl()
        {
            InitializeComponent();

            DataContextChanged += RateChartControl_DataContextChanged;
        }

        private void RateChartControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && e.NewValue is RateChartViewModel model)
            {
                _model = model;
                _model.PropertyChanged += Model_PropertyChanged;
                _model.Initialize();
            }
        }

        private void Model_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_model.ZcycArray))
            {
                var output = new List<OhlcPoint>();
                var weeklyData = _model.ZcycArray.GroupBy(g => g.Tradedate.YearAndWeekToNumber()).ToList();

                weeklyData.ForEach(w =>
                {
                    DateTime date = DateTime.MinValue;
                    double open = double.MinValue;
                    double high = double.MinValue;
                    double low = double.MaxValue;
                    double close = 0d;
                    foreach (var d in w)
                    {
                        if (date == DateTime.MinValue)
                        {
                            date = d.Tradedate;
                            open = d.Value;
                        }
                        high = Math.Max(high, d.Value);
                        low = Math.Min(low, d.Value);
                        close = d.Value;
                    }
                    output.Add(new OhlcPoint
                    {
                        //X = date.Subtract(startDate).TotalDays + 1,
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close
                    });
                });

                var seriesCollection = new SeriesCollection
                {
                    new OhlcSeries()
                    {
                        Values = new ChartValues<OhlcPoint>(output)
                    }
                };

                RateChart.Series = seriesCollection;
            };
        }
    }
}
