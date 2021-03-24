using System;
using System.Linq;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
using Microsoft.Toolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace CurveAnalyzer.Charts
{
    public class DailySpreadChart : ChartCreator<(double period1, double period2)>
    {
        public override void Plot(DataManager dataManager, (double period1, double period2) value)
        {
            IsBusy = true;
            var startDate = new DateTime(1900, 1, 1);

            var data1 = dataManager.GetAllDataForPeriod(value.period1).Result;
            var data2 = dataManager.GetAllDataForPeriod(value.period2).Result;

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
        }

        public override void Setup(IRelayCommand[] updateCommands)
        {
            base.Setup(updateCommands);

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

        public override bool Validate((double period1, double period2) value)
        {
            return value.period1 > 0d && value.period2 > 0d && !value.period1.Equals(value.period2);
        }
    }
}
