using System;
using System.Linq;
using System.Threading.Tasks;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace CurveAnalyzer.Charts
{
    public class DailyCurveChart : ChartCreator<DateTime>
    {
        private ZcycData data;
        private readonly IDataManager dataManager = App.Current.Services.GetService<IDataManager>();

        public DailyCurveChart()
        {
            MainChart = new PlotModel
            {
                LegendBorder = OxyColors.Black,
                LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
                LegendPosition = LegendPosition.BottomCenter,
                LegendPlacement = LegendPlacement.Inside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendItemAlignment = OxyPlot.HorizontalAlignment.Left,
            };
        }

        public override void Setup(IRelayCommand[] updateCommands)
        {
            base.Setup(updateCommands);

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

        public override void Plot(DateTime value)
        {

            if (MainChart.Series.Any(_ => (DateTime)_.Tag == value))
            {
                return;
            }
            IsBusy = true;

            Task.Run(async () =>
            {
                data = await dataManager.GetData(value);

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

                    data.DataRow.ForEach(item => { lineSeries1.Points.Add(new DataPoint(item.Period, item.Value)); });

                    MainChart.Series.Add(lineSeries1);
                    MainChart.InvalidatePlot(true);
                    dataManager.Status = string.Empty;
                }
                else
                {
                    dataManager.Status = $"No data for {value}";
                }
                IsBusy = false;
            });
        }
    }
}
