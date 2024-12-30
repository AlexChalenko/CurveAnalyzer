using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace CurveAnalyzer.Charts
{
    public class DailySpreadChart : ChartBase<Periods>
    {
        public DailySpreadChart(IDataManager DataManager) : base(DataManager)
        {
            var lineAxisY1 = new LinearAxis
            {
                Title = "Спреды",
                Position = AxisPosition.Right,
                MajorGridlineThickness = 1,
                MinorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
            };
            var dateTimeAxis1 = new DateTimeAxis
            {
                MajorGridlineThickness = 2,
                MinorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            MainChart.Axes.Add(dateTimeAxis1);
            MainChart.Axes.Add(lineAxisY1);
        }

        public override async Task Plot(Periods periods)
        {
            IsBusy = true;
            var startDate = new DateTime(1900, 1, 1);

            var data1 = await _dataManager.GetZcycForPeriodAsync(periods.Period1);
            var data2 = await _dataManager.GetZcycForPeriodAsync(periods.Period2);

            var query = data1.Join(data2,
                                  d1 => d1.Tradedate,
                                  d2 => d2.Tradedate,
                                  (d1, d2) => new { Date = d1.Tradedate, Value = d2.Value - d1.Value });

            var lineSeries1 = new LineSeries
            {
                //MarkerType = MarkerType.Circle,
                //MarkerStrokeThickness = 2,
                //MarkerSize = 2,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1,
                //InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline
            };

            lineSeries1.MarkerStroke = lineSeries1.Color;

            foreach (var item in query)
            {
                lineSeries1.Points.Add(new DataPoint(item.Date.Subtract(startDate).TotalDays, item.Value));
            }

            MainChart.Series.Add(lineSeries1);
            MainChart.InvalidatePlot(true);
            IsBusy = false;

            //return Task.CompletedTask;
        }

        public override bool Validate(Periods periods)
        {
            return periods.Period1 > 0d && periods.Period2 > 0d && !periods.Period1.Equals(periods.Period2);
        }
    }
}
