using System;
using System.Collections.Generic;
using System.Linq;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
using CurveAnalyzer.Tools;
using Microsoft.Toolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using TALib;

namespace CurveAnalyzer.Charts
{
    public class WeeklyRateDynamicChart : ChartCreator<double>
    {
        public WeeklyRateDynamicChart()
        {
            MainChart = new PlotModel
            {
                LegendBorder = OxyColors.Black,
                LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
                LegendPosition = LegendPosition.BottomCenter,
                LegendPlacement = LegendPlacement.Inside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendItemAlignment = HorizontalAlignment.Left,
            };
        }

        public override void Plot(DataManager dataManager, double value)
        {
            IsBusy = true;

            var weeklyData = getOxyWeeklyOhlcs(dataManager, value);

            if (weeklyData == null)
            {
                IsBusy = false;
                return;
            }

            var lineSeries1 = new CandleStickSeries
            {
                XAxisKey = "X",
                YAxisKey = "Y1"
            };

            lineSeries1.Items.AddRange(weeklyData);

            var lineSeries2 = new LineSeries
            {
                XAxisKey = "X",
                YAxisKey = "Y2"
            };

            MainChart.Series.Add(lineSeries1);

            double[] outData = new double[weeklyData.Count];
            var res = Core.Roc(weeklyData.Select(d => d.Close).ToArray(), 0, weeklyData.Count - 1, outData, out int begIdx, out int element, 13);

            for (int i = 0; i < weeklyData.Count; i++)
            {
                double item = 0d;
                if (i >= begIdx)
                    item = outData[i - begIdx];
                lineSeries2.Points.Add(new DataPoint(weeklyData[i].X, item));
            }

            MainChart.Series.Add(lineSeries2);
            MainChart.InvalidatePlot(true);
            IsBusy = false;
        }

        private List<HighLowItem> getOxyWeeklyOhlcs(DataManager dataManager, double period)
        {
            var output = new List<HighLowItem>();
            var startDate = new DateTime(1900, 1, 1);

            var dailydata = dataManager.GetAllDataForPeriod(period).Result;

            if (dailydata == null)
                return null;

            var weeklyData = dailydata.GroupBy(g => g.Tradedate.YearAndWeekToNumber()).ToList();

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
                output.Add(new HighLowItem
                {
                    X = date.Subtract(startDate).TotalDays + 1,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close
                });
            });
            return output;
        }

        public override void Setup(IRelayCommand[] updateCommands)
        {
            base.Setup(updateCommands);

            var lineAxisY1 = new LinearAxis
            {
                Title = "График",
                Key = "Y1",
                StartPosition = 0.3,
                Position = AxisPosition.Right,
                MajorGridlineThickness = 1,
                MinorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
            };

            var lineAxisY2 = new LinearAxis
            {
                Title = "Индикатор",
                Position = AxisPosition.Right,
                Key = "Y2",
                EndPosition = 0.3
            };

            var LineAnnotation1 = new LineAnnotation
            {
                ClipByYAxis = false,
                Type = LineAnnotationType.Horizontal,
                Y = 0,
                Color = OxyColors.Green,
                YAxisKey = "Y2",
                TextOrientation = AnnotationTextOrientation.Horizontal,
                TextHorizontalAlignment = HorizontalAlignment.Right,
            };

            var dateTimeAxis1 = new DateTimeAxis
            {
                Key = "X",
                MajorGridlineThickness = 2,
                MinorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            MainChart.Axes.Add(dateTimeAxis1);
            MainChart.Axes.Add(lineAxisY1);
            MainChart.Axes.Add(lineAxisY2);
            MainChart.Annotations.Add(LineAnnotation1);
        }
    }
}
