using System;
using System.Linq;
using System.Threading.Tasks;
using CurveAnalyzer.Interfaces;
using Microsoft.Toolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace CurveAnalyzer.Charts
{
    public class DailyCurveChart : ChartCreator<DateTime>
    {
        public DailyCurveChart()
        {
            var l = new Legend()
            {
                LegendBorder = OxyColors.Black,
                LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
                LegendPosition = LegendPosition.BottomCenter,
                LegendPlacement = LegendPlacement.Inside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendItemAlignment = HorizontalAlignment.Left,
            };
            MainChart.Legends.Add(l);
        }

        public override void Setup(IRelayCommand[] commandsToUpdate)
        {
            base.Setup(commandsToUpdate);

            var linearAxis1 = new LinearAxis
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                TitleFont = "/Fonts/#Roboto",
                Title = "Доходность",
                TitleFontWeight = OxyPlot.FontWeights.Bold,
                MaximumPadding = 0.1,
                MinimumPadding = 0.1
            };
            MainChart.Axes.Add(linearAxis1);

            var linearAxis2 = new LinearAxis
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                Position = AxisPosition.Bottom,
                IsZoomEnabled = false,
                Title = "Дюрация",
                TitleFont = "/Fonts/#Roboto",
                TitleFontWeight = OxyPlot.FontWeights.Bold,
            };
            MainChart.Axes.Add(linearAxis2);
        }

        public override async Task Plot(DateTime value)
        {
            if (MainChart.Series.Any(_ => (DateTime)_.Tag == value))
            {
                return;
            }
            IsBusy = true;

            var data = await DataManager.GetData(value).ConfigureAwait(false);

            if (data.DataRow.Count > 0)
            {
                var lineSeries1 = new LineSeries
                {
                    MarkerType = MarkerType.Circle,
                    MarkerStrokeThickness = 2,
                    MarkerSize = 2,
                    LineStyle = LineStyle.Solid,
                    StrokeThickness = 2,
                    Title = value.ToShortDateString(),
                    InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline,
                    Tag = value
                };
                lineSeries1.MarkerStroke = lineSeries1.Color;

                data.DataRow.ForEach(item => lineSeries1.Points.Add(new DataPoint(item.Period, item.Value)));

                MainChart.Series.Add(lineSeries1);
                MainChart.InvalidatePlot(true);
                DataManager.Status = string.Empty;
            }
            else
            {
                DataManager.Status = $"No data for {value}";
            }
            IsBusy = false;
        }
    }
}
