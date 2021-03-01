using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CurveAnalyzer.Data;
using CurveAnalyzer.DataProviders;
using CurveAnalyzer.Interfaces;
using CurveAnalyzer.Tools;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Scripting.Utils;
using MoexData;
using OxyPlot;
using OxyPlot.Axes;
using TicTacTec.TA.Library;
using AxisPosition = OxyPlot.Axes.AxisPosition;
using FontWeights = OxyPlot.FontWeights;

namespace CurveAnalyzer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        MainViewModel mainViewModel;

        private readonly DispatcherTimer timer;
        private string lastUpdateDate;

        private ZcycDataProvider realtimeDataProvider;
        private ZcycDataProvider historyDataProvider;
        private DateRange histotyDateRange;

        public MainWindow()
        {
            //realtimeDataProvider = new IssMoexDataProvider();
            //historyDataProvider = new SQLiteDataProvider();

            //var mapper1 = Mappers.Xy<ObservablePoint>().X(point => point.X).Y(point => point.Y);

            //LabelsX = new ObservableCollection<string>();
            //SeriesCollection = new SeriesCollection(mapper1);

            //MyModel = new OxyPlot.PlotModel();

            //var linearAxis1 = new LinearAxis
            //{
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Dot,
            //    Font = "Tahoma",
            //    Title = "Доходность",
            //    MaximumPadding = 0.1,
            //    MinimumPadding = 0.1
            //};
            //MyModel.Axes.Add(linearAxis1);
            //var linearAxis2 = new LinearAxis
            //{
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MinorGridlineStyle = LineStyle.Dot,
            //    Position = AxisPosition.Bottom,
            //    IsZoomEnabled = false,
            //    Title = "Дюрация",
            //    TitleFont = "Segoe UI",
            //    TitleFontWeight = FontWeights.Bold,
            //};

            //MyModel.Axes.Add(linearAxis2);

            //DataModel = new PlotModel();
            //var lineAxisY1 = new LinearAxis
            //{
            //    Title = "График",
            //    Key = "Y1",
            //    StartPosition = 0.3,
            //    Position = AxisPosition.Right,
            //};
            //var lineAxisY2 = new LinearAxis
            //{
            //    Title = "Индикатор",
            //    Position = AxisPosition.Right,
            //    Key = "Y2",
            //    EndPosition = 0.3
            //};

            //var dateTimeAxis1 = new DateTimeAxis
            //{
            //    Key = "X"
            //};

            //DataModel.Axes.Add(dateTimeAxis1);
            //DataModel.Axes.Add(lineAxisY1);
            //DataModel.Axes.Add(lineAxisY2);

            ////MyModel.Axes.Add(lineAxisX);

            //XFormatter = x => x.ToString("0.00");
            DataContext = mainViewModel = new MainViewModel();
            InitializeComponent();

            //MainChart.LegendLocation = LegendLocation.Right;
            //CalendarControl.SelectedDate = DateTime.Today;
            //LastUpdateDate = CalendarControl.SelectedDate.Value.ToShortDateString();
            //timer = new DispatcherTimer
            //{
            //    Interval = new TimeSpan(0, 1, 0)
            //};
            //timer.Tick += onTimerTick;

            //updateHistory();

            //AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        //private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    var ex = e.ExceptionObject as Exception;
        //    if (ex != null)
        //    {
        //        ExceptionUtils.LogException(ex,Events.Log.UnhandledException,
        //        true);
        //    }
        //    else if (e.ExceptionObject != null)
        //    {
        //        var type = e.ExceptionObject.GetType().ToString();
        //        Console.WriteLine(
        //        $"Non-exception object: {type} - {e.ExceptionObject}");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Unknown object");
        //    }
        //}

        
        public event PropertyChangedEventHandler PropertyChanged;

        public OxyPlot.PlotModel DataModel { get; private set; }
        public ObservableCollection<string> LabelsX { get; set; }

        //private double minValue = double.MaxValue;
        //private double maxValue = double.MinValue;
        public string LastUpdateDate
        {
            get => lastUpdateDate;

            set
            {
                lastUpdateDate = value;
                OnPropertyChanged();
            }
        }

        public OxyPlot.PlotModel MyModel { get; private set; }
        public SeriesCollection SeriesCollection { get; private set; }
        public Func<double, string> XFormatter { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string name = null!) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;

            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                timer.Start();
            }
            else if (timer.IsEnabled)
            {
                timer.Stop();
            }
        }

        private void DeleteChart_Click(object sender, RoutedEventArgs e)
        {
        }

        private async Task downloadAndSaveData(DateRange dateRange)
        {
            var dates = new List<DateTime>();

            while (dateRange.StartDate <= dateRange.EndDate)
            {
                dates.Add(dateRange.StartDate);
                dateRange.StartDate = dateRange.StartDate.AddDays(1d);
            }

            if (dates.Count > 0)
            {
                foreach (var item in from date in dates select realtimeDataProvider.GetDataForDate(date))
                {
                    Task<ZcycData> firstFinishedTask = await Task.WhenAny(item);
                    var res = await firstFinishedTask;
                    if (res?.Date != null && res.DataRow.Count > 0)
                    {
                        StatusText.Dispatcher.Invoke(() => StatusText.Content = $"Загрузка данных за {res.Date}");
                        var result = await historyDataProvider.SaveData(res);
                        Debug.WriteLine($"******************** added {result} with date {res.Date}");
                        if (res.Date > histotyDateRange.EndDate)
                        {
                            histotyDateRange.EndDate = res.Date;
                        }
                    }
                }
                StatusText.Dispatcher.Invoke(() => StatusText.Content = "Загрузка данных завершена");
            }
        }

        private List<OxyPlot.Series.HighLowItem> getOxyWeeklyOhlcs(double period)
        {
            var output = new List<OxyPlot.Series.HighLowItem>();

            var startDate = new DateTime(1900, 1, 1);

            using var db = new MoexContext();
            //db.Database.EnsureCreated();

            var query = db.Zcycs.Where(p => p.Period == period)
                             .AsEnumerable()
                             .GroupBy(g => g.Tradedate.YearAndWeekToNumber())
                             .ToList();

            foreach (var item in query)
            {
                var date = item.Select(d => d.Tradedate).First();
                var open = item.Select(o => o.Value).First();
                var high = double.MinValue;
                var low = double.MaxValue;
                var close = item.Select(c => c.Value).Last();
                foreach (var item2 in item)
                {
                    high = Math.Max(high, item2.Value);
                    low = Math.Min(low, item2.Value);
                }

                //Debug.WriteLine($"{ date} {open} {high} {low} {close}");
                output.Add(new OxyPlot.Series.HighLowItem
                {
                    X = date.Subtract(startDate).TotalDays,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close
                });
            }

            return output;
        }

        private ChartValues<OhlcPoint> getWeeklyOhlcs(double period)
        {
            var output = new ChartValues<OhlcPoint>();

            using var db = new MoexContext();
            //db.Database.EnsureCreated();

            var query = db.Zcycs.Where(p => p.Period == period)
                             .AsEnumerable()
                             .GroupBy(g => g.Tradedate.YearAndWeekToNumber())
                             .ToList();

            foreach (var item in query)
            {
                var date = item.Select(d => d.Tradedate).First();
                var open = item.Select(o => o.Value).First();
                var high = double.MinValue;
                var low = double.MaxValue;
                var close = item.Select(c => c.Value).Last();
                foreach (var item2 in item)
                {
                    high = Math.Max(high, item2.Value);
                    low = Math.Min(low, item2.Value);
                }
                Debug.WriteLine($"{date} {open} {high} {low} {close}");
                output.Add(new OhlcPoint
                {
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close
                });
            }
            return output;
        }

        private void MainChart_DataClick(object sender, ChartPoint chartPoint)
        {
            //MyPopsup.IsOpen = false;
            //MyPopsup.IsOpen = true;

            //var data2 = getWeeklyOhlcs(chartPoint.X);
            var data2 = getOxyWeeklyOhlcs(chartPoint.X);

            //var chart = new CartesianChart
            //{
            //    DisableAnimations = true
            //};
            //var seriesCollection = new SeriesCollection
            //{
            //    new OhlcSeries
            //    {
            //        Values = data2
            //    }
            //};

            var lineSeries1 = new OxyPlot.Series.CandleStickSeries
            {
                XAxisKey = "X",
                YAxisKey = "Y1"
            };

            //foreach (var item in data2)
            //{
            //lineSeries1.Items.Add(item);
            //}
            lineSeries1.Items.AddRange(data2);

            var lineSeries2 = new OxyPlot.Series.LineSeries
            {
                XAxisKey = "X",
                YAxisKey = "Y2"
            };

            DataModel.Series.Add(lineSeries1);

            double[] outData = new double[data2.Count];
            var res = Core.Roc(0, data2.Count - 1, data2.Select(d => d.Close).ToArray(), 13, out int begIdx, out int element, outData);

            for (int i = 0; i < data2.Count; i++)
            {
                double item = 0d;
                if (i >= begIdx)
                    item = outData[i - begIdx];
                lineSeries2.Points.Add(new DataPoint(data2[i].X, item));
            }

            DataModel.Series.Add(lineSeries2);

            //var rocData = new ChartValues<double>();

            //for (int i = 0; i < data2.Count; i++)
            //{
            //    double item = 0d;
            //    if (i >= begIdx)
            //        item = outData[i - begIdx];
            //    rocData.Add(item);
            //}

            //if (res == Core.RetCode.Success)
            //{
            //    Debug.WriteLine($"{begIdx} {element}");
            //    var lineSeries = new LineSeries
            //    {
            //        Values = rocData,
            //    };
            //    seriesCollection.Add(lineSeries);
            //}

            //chart.Series = seriesCollection;
            //DataTab.Content = chart;
        }

        private void onTimerTick(object sender, EventArgs e)
        {
            updateChart(DateTime.Today);
        }

        private void updateChart(DateTime dateTime)
        {
            LastUpdateDate = dateTime.ToShortDateString();
            GetDataButton.IsEnabled = false;

            //var data = GetData(dateTime).GetAwaiter().GetResult();
            ZcycData dataToPlot;

            if (histotyDateRange.IsInRange(dateTime))
                dataToPlot = historyDataProvider.GetDataForDate(dateTime).Result; //todo
            else
                dataToPlot = realtimeDataProvider.GetDataForDate(dateTime).Result;

            GetDataButton.IsEnabled = true;

            if (dataToPlot.DataRow.Count == 0)
                return;

            bool isDataNew = true;
            int index = -1;

            //var ind = SeriesCollection.ToList().FindIndex(r=> r.Title == lastUpdateDate);
            //var ind2 = SeriesCollection.Select((v, i) => new { v, i }).Single(p => p.v.Title == lastUpdateDate); // returns error

            for (int i = 0; i < SeriesCollection.Count; i++)
            {
                LiveCharts.Definitions.Series.ISeriesView item = SeriesCollection[i];
                if (item.Title == this.LastUpdateDate)
                {
                    isDataNew = false;
                    index = i;
                }
            }

            var points = new List<ObservablePoint>();

            foreach (var item in dataToPlot.DataRow)
            {
                var point = new ObservablePoint(item.Period, item.Value);
                points.Add(point);
            }

            if (isDataNew)
            {
                SeriesCollection.Add(new LineSeries
                {
                    Values = new ChartValues<ObservablePoint>(points),
                    Title = this.LastUpdateDate,
                });
            }
            else
            {
                foreach (ObservablePoint item in SeriesCollection[index].Values)
                {
                    foreach (var point in points)
                    {
                        if (point.X == item.X)
                        {
                            item.Y = point.Y;
                            break;
                        }
                    }
                }
            }

            var lineSeries1 = new OxyPlot.Series.LineSeries
            {
                Color = OxyColor.FromRgb(33, 149, 242),
                MarkerType = MarkerType.Circle,
                MarkerStrokeThickness = 2,
                MarkerFill = OxyColors.White,
                MarkerSize = 3,
                StrokeThickness = 2,
                //lineSeries1.MarkerFill = OxyColors.Transparent;
                InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline
            };

            lineSeries1.MarkerStroke = lineSeries1.Color;

            foreach (var item in dataToPlot.DataRow)
            {
                lineSeries1.Points.Add(new DataPoint(item.Period, item.Value));
            }

            MyModel.Series.Add(lineSeries1);
        }
    }
}
