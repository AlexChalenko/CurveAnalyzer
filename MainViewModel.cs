using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CurveAnalyzer.Data;
using CurveAnalyzer.DataProviders;
using CurveAnalyzer.Interfaces;
using CurveAnalyzer.Tools;
using MoexData;
using MvvmHelpers;
using MvvmHelpers.Commands;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using TicTacTec.TA.Library;

namespace CurveAnalyzer
{
    internal class MainViewModel : BaseViewModel
    {
        private PlotModel dailyChart;
        public PlotModel DailyChart
        {
            get { return dailyChart; }
            set { SetProperty(ref dailyChart, value); }
        }

        private PlotModel performanceChart;
        public PlotModel PerformanceChart
        {
            get { return performanceChart; }
            set { SetProperty(ref performanceChart, value); }
        }

        private ZcycDataProvider realtimeDataProvider;
        private ZcycDataProvider historyDataProvider;

        private DateTime startDate;
        private DateTime endDate;

        public DateTime StartDate
        {
            get { return startDate; }
            set { SetProperty(ref startDate, value); }
        }

        public DateTime EndDate
        {
            get => endDate;
            set => SetProperty(ref endDate, value);
        }

        private DateTime selectedDate;

        public DateTime SelectedDate
        {
            get { return selectedDate; }
            set { SetProperty(ref selectedDate, value); }
        }

        SynchronizationContext _context;
        SynchronizationContext context
        {
            get
            {
                return _context = SynchronizationContext.Current;
            }
        }

        private string status;
        public string Status
        {
            get => status;
            set
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SetProperty(ref status, value);
                });
            }
        }

        public AsyncCommand PlotDailyChartCommand { get; }
        public Command PlotWeeklyChartCommand { get; }
        public Command ClearDailyChartCommand { get; }

        List<Zcyc> mainData;

        public MainViewModel()
        {
            DailyChart = new PlotModel
            {
                LegendBorder = OxyColors.Black,
                LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
                LegendPosition = LegendPosition.BottomCenter,
                LegendPlacement = LegendPlacement.Inside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendItemAlignment = OxyPlot.HorizontalAlignment.Left,
            };

            PerformanceChart = new PlotModel
            {
                LegendBorder = OxyColors.Black,
                LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
                LegendPosition = LegendPosition.BottomCenter,
                LegendPlacement = LegendPlacement.Inside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendItemAlignment = OxyPlot.HorizontalAlignment.Left,
            };

            setupDailyChart();

            setupPerformanceChart();

            realtimeDataProvider = new IssMoexDataProvider();
            historyDataProvider = new SQLiteDataProvider();

            PlotDailyChartCommand = new AsyncCommand(()=>plotDailyChart(SelectedDate), o => IsNotBusy);
            ClearDailyChartCommand = new Command(clearDailyChart, o => IsNotBusy);
            //PlotWeeklyChartCommand = new Command(o => plotWeeklyChart(o), o => IsNotBusy);

            updateHistory();
        }

        private void plotWeeklyChart(double period)
        {
            var weeklyData = getOxyWeeklyOhlcs(period);

            var lineSeries1 = new OxyPlot.Series.CandleStickSeries
            {
                XAxisKey = "X",
                YAxisKey = "Y1"
            };

            lineSeries1.Items.AddRange(weeklyData);

            var lineSeries2 = new OxyPlot.Series.LineSeries
            {
                XAxisKey = "X",
                YAxisKey = "Y2"
            };

            PerformanceChart.Series.Add(lineSeries1);

            double[] outData = new double[weeklyData.Count];
            var res = Core.Roc(0, weeklyData.Count - 1, weeklyData.Select(d => d.Close).ToArray(), 13, out int begIdx, out int element, outData);

            for (int i = 0; i < weeklyData.Count; i++)
            {
                double item = 0d;
                if (i >= begIdx)
                    item = outData[i - begIdx];
                lineSeries2.Points.Add(new DataPoint(weeklyData[i].X, item));
            }

            PerformanceChart.Series.Add(lineSeries2);
            PerformanceChart.InvalidatePlot(true);

        }

        private void clearDailyChart(object obj)
        {
            DailyChart.Series.Clear();
            DailyChart.InvalidatePlot(true);
        }

        private void updateHistory()
        {
            realtimeDataProvider.GetAvailableDates().ContinueWith(async t =>
            {
                //var startDateFromRealtime = t.Result.StartDate;
                //var endDateFromRealtime = t.Result.EndDate;
                //StartDate = startDateFromRealtime;
                //EndDate = endDateFromRealtime;

                var realtimeDates = t.Result;

                //var realtimeDates = Enumerable.Range(0, int.MaxValue)
                //                              .Select(index => StartDate.AddDays(index))
                //                              .TakeWhile(date => date <= EndDate);

                var historyDates = await historyDataProvider.GetAvailableDates().ConfigureAwait(false);

                //var historyDates = Enumerable.Range(0, int.MaxValue)
                //                             .Select(index => histotyDateRange.StartDate.AddDays(index))
                //                             .TakeWhile(date => date <= histotyDateRange.EndDate);

                var newDates = realtimeDates.Except(historyDates).Where(date => date > historyDates.Max());


                StartDate = historyDates.Min();
                EndDate = realtimeDates.Max();
                SelectedDate = EndDate;

                await Task.Run(() => downloadAndSaveData(newDates));

                //Task.Run(() => downloadData(startDay, endDay.AddDays(-1d)));
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task downloadAndSaveData(IEnumerable<DateTime> dates)
        {
            foreach (var item in from date in dates select realtimeDataProvider.GetDataForDate(date))
            {
                Task<ZcycData> firstFinishedTask = await Task.WhenAny(item);
                var res = await firstFinishedTask;
                if (res?.Date != null && res.DataRow.Count > 0)
                {
                    Status = $"Загрузка данных за {res.Date}";
                    var result = await historyDataProvider.SaveData(res);
                    Debug.WriteLine($"******************** added {result} with date {res.Date}");
                }
            }
            Status = "Загрузка данных завершена";
        }

        async Task plotDailyChart(DateTime dateTime)
        {
            if (DailyChart.Series.Any(_ => _.Title.Equals(dateTime.ToShortDateString())))
                return;


            //if (histotyDateRange.IsInRange(dateTime))
            ZcycData dataToPlot = await historyDataProvider.GetDataForDate(dateTime); //todo

            if (dataToPlot.DataRow.Count == 0)
                dataToPlot = await realtimeDataProvider.GetDataForDate(dateTime);

            //GetDataButton.IsEnabled = true;

            if (dataToPlot.DataRow.Count == 0)
                return;

            var lineSeries1 = new LineSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerStrokeThickness = 2,
                MarkerSize = 2,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 2,
                Title = dateTime.ToShortDateString(),
                InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline
            };

            lineSeries1.MarkerStroke = lineSeries1.Color;

            foreach (var item in dataToPlot.DataRow)
            {
                lineSeries1.Points.Add(new DataPoint(item.Period, item.Value));
            }

            lineSeries1.MouseDown += (s, e) =>
            {
                TrackerHitResult t = lineSeries1.GetNearestPoint(e.HitTestResult.NearestHitPoint, false);
                //var point = t.Item as OxyPlot.DataPoint;
                Debug.WriteLine($"{t.DataPoint.X} {t.DataPoint.Y}");
                plotWeeklyChart(t.DataPoint.X);
            };

            DailyChart.Series.Add(lineSeries1);
            DailyChart.InvalidatePlot(true);
            return;
        }

        private void setupDailyChart()
        {
            var linearAxis1 = new LinearAxis
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                TitleFont = "Segoe UI",
                Title = "Доходность",
                TitleFontWeight = OxyPlot.FontWeights.Bold,
                MaximumPadding = 0.1,
                MinimumPadding = 0.1
            };
            DailyChart.Axes.Add(linearAxis1);
            var linearAxis2 = new LinearAxis
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                Position = AxisPosition.Bottom,
                IsZoomEnabled = false,
                Title = "Дюрация",
                TitleFont = "Segoe UI",
                TitleFontWeight = OxyPlot.FontWeights.Bold,
            };
            DailyChart.Axes.Add(linearAxis2);
        }

        void setupPerformanceChart()
        {
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
                Text = "rer",
                TextLinePosition = 1,
                YAxisKey = "Y2",
                TextOrientation = AnnotationTextOrientation.Horizontal,
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right

            };

            var dateTimeAxis1 = new DateTimeAxis
            {
                Key = "X",
                MajorGridlineThickness = 2,
                MinorGridlineThickness = 1,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            PerformanceChart.Axes.Add(dateTimeAxis1);
            PerformanceChart.Axes.Add(lineAxisY1);
            PerformanceChart.Axes.Add(lineAxisY2);
            performanceChart.Annotations.Add(LineAnnotation1);
        }


        private List<HighLowItem> getOxyWeeklyOhlcs(double period)
        {
            var output = new List<HighLowItem>();

            var startDate = new DateTime(1900, 1, 1);

            using var db = new MoexContext();
            //db.Database.EnsureCreated();

            var weeklyData = db.Zcycs.Where(p => p.Period == period)
                             .AsEnumerable()
                             .GroupBy(g => g.Tradedate.YearAndWeekToNumber())
                             .ToList();

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
    }
}
