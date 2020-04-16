using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using MoexData;
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
using System.Xml;
using System.Xml.Serialization;
using TicTacTec.TA.Library;

namespace CurveAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string DataUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?date={0}&iss.only=yearyields&iss.meta=off";
        //readonly string OldDataUrl = https://iss.moex.com/iss/engines/state/markets/zcyc.json

        public SeriesCollection SeriesCollection { get; private set; }
        public ObservableCollection<string> LabelsX { get; set; }

        public Func<double, string> XFormatter { get; set; }
        public IssData IssData { get; set; }
        private string lastUpdateDate;
        //private double minValue = double.MaxValue;
        //private double maxValue = double.MinValue;

        private readonly DispatcherTimer timer;

        public string LastUpdateDate
        {
            get
            {
                return lastUpdateDate;
            }

            set
            {
                lastUpdateDate = value;
                OnPropertyChanged();
            }
        }

        static int weekYear(DateTime date)
        {
            CultureInfo ciCurr = CultureInfo.CurrentCulture;
            return date.Year * 100 + ciCurr.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public MainWindow()
        {
            var mapper1 = Mappers.Xy<ObservablePoint>().X(point => point.X).Y(point => point.Y);

            LabelsX = new ObservableCollection<string>();
            SeriesCollection = new SeriesCollection(mapper1);

            XFormatter = x => x.ToString("0.00");
            DataContext = this;
            InitializeComponent();

            MainChart.LegendLocation = LegendLocation.Right;
            CalendarControl.SelectedDate = DateTime.Today;
            LastUpdateDate = CalendarControl.SelectedDate.Value.ToShortDateString();
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 1, 0);
            timer.Tick += onTimerTick;

            getDates().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                }
                else if (t.Exception != null)
                {
                }

                var startDay = t.Result.Item1;
                var endDay = t.Result.Item2;
                CalendarControl.DisplayDateStart = startDay;
                CalendarControl.DisplayDateEnd = endDay;

                using (var db = new MoexContext())
                {
                    long max = db.Zcycs.Select(r => r.Num).Max();
                    //var startDate = db.Zcycs.Where(r => r.Num == max).Select(r => r.Tradedate).FirstOrDefault().AddDays(1d);
                    var startDate = db.Zcycs.Single(r => r.Num == max).Tradedate.AddDays(1d);
                    var endDate = DateTime.Today.AddDays(-1d);

                    if ((endDate - startDate).TotalDays >= 0)
                    {
                        Task.Run(() => downloadData(startDate, endDate));
                    }
                }

                //Task.Run(() => downloadData(startDay, endDay.AddDays(-1d)));

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        async Task downloadData(DateTime startDay, DateTime endDay)
        {
            List<DateTime> dates = new List<DateTime>();

            while (startDay <= endDay)
            {
                dates.Add(startDay);
                startDay = startDay.AddDays(1d);
            }

            using (var db = new MoexContext())
            {
                long num = 0;

                IEnumerable<Task<IssData>> downloadTasksQuery = from date in dates select getData(date);

                foreach (var item in downloadTasksQuery)
                {
                    Task<IssData> firstFinishedTask = await Task.WhenAny(item);
                    var res = firstFinishedTask;
                    if (res.Result != null)
                    {
                        foreach (var row in res.Result.data.rows)
                        {
                            try
                            {
                                num++;
                                db.Zcycs.Add(new Zcyc
                                {
                                    //Num = num,
                                    Tradedate = row.tradedate,
                                    Period = row.period,
                                    Value = row.value
                                });
                                var index = db.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(" * ******************* ERROR" + ex.InnerException);
                                throw ex;
                            }
                        }
                        Debug.WriteLine("********************" + res.Result.data.rows[0].tradedate);
                    }
                }
            }
        }

        private void onTimerTick(object sender, EventArgs e)
        {
            updateChart(DateTime.Today);
        }

        private async Task<Tuple<DateTime, DateTime>> getDates()
        {
            var tcs = new TaskCompletionSource<Tuple<DateTime, DateTime>>();

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.Async = true;

            using (XmlReader reader = XmlReader.Create("https://iss.moex.com/iss/engines/stock/zcyc.xml?iss.only=yearyields.dates&iss.meta=off", settings))
            {
                while (await reader.ReadAsync())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            //Debug.WriteLine($"Start Element {reader.Name} {reader.Value}");
                            if (reader.Name.Equals("row"))
                            {
                                var date1 = DateTime.Parse(reader.GetAttribute(0));
                                var date2 = DateTime.Parse(reader.GetAttribute(1));
                                tcs.TrySetResult(Tuple.Create(date1, date2));
                            }
                            break;

                        case XmlNodeType.Text:
                            //Debug.WriteLine($"Text Node: {await reader.GetValueAsync()}");
                            break;

                        case XmlNodeType.EndElement:
                            //Debug.WriteLine($"End Element {reader.Name}");
                            break;

                        default:
                            //Debug.WriteLine($"Other node {reader.NodeType} with value {reader.Value}");
                            break;
                    }
                }
            }
            return await tcs.Task;
        }

        private Task<IssData> getData(DateTime date)
        {
            var tcs = new TaskCompletionSource<IssData>();

            using (var db = new MoexContext())
            {
                var dbData = db.Zcycs.Where(r => r.Tradedate.Equals(date)).ToList();
                if (dbData.Count > 0)
                {
                    IssData issData = new IssData();
                    issData.data = new documentData();
                    issData.data.rows = new documentDataRow[12];
                    for (int i = 0; i < dbData.Count; i++)
                    {
                        Zcyc item = dbData[i];
                        issData.data.rows[i] = new documentDataRow
                        {
                            tradedate = item.Tradedate,
                            period = item.Period,
                            value = item.Value
                        };
                    }
                    tcs.TrySetResult(issData);
                }
            }

            XmlSerializer serializer = new XmlSerializer(typeof(IssData));
            IssData data;
            string downloadingDate = date.ToString("yyyy-MM-dd");
            string url = string.Format(DataUrl, downloadingDate);

            try
            {
                using (XmlReader xmlReader = XmlReader.Create(url)) //500
                    data = (IssData)serializer.Deserialize(xmlReader);

                if (data.data.rows.Count() > 0)
                {
                    tcs.TrySetResult(data);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        ChartValues<OhlcPoint> getWeeklyOhlcs(double period)
        {
            var output = new ChartValues<OhlcPoint>();

            using (var db = new MoexContext())
            {
                var query = db.Zcycs.Where(p => p.Period == period)
                                 .AsEnumerable()
                                 .GroupBy(g => weekYear(g.Tradedate))
                                 .Select(w => w.ToList())
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
                    //Debug.WriteLine($"{date} {open} {high} {low} {close}");
                    output.Add(new OhlcPoint
                    {
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close
                    });
                }
            }

            return output;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void updateChart(DateTime dateTime)
        {
            LastUpdateDate = dateTime.ToShortDateString();
            GetDataButton.IsEnabled = false;

            //var data = GetData(dateTime).GetAwaiter().GetResult();

            getData(dateTime).ContinueWith(task =>
              {
                  GetDataButton.IsEnabled = true;

                  if (task.IsCanceled || task.Exception != null)
                  {
                  }
                  else
                  {
                      if (task.Result == null || task.Result.data.rows.Count() == 0) return;

                      ////var data = IssData.data.rows.Select(x => x.value).ToArray();
                      ////var labels = IssData.data.rows.Select(x => x.period).ToArray();

                      ////double max = data.Max();
                      ////double min = data.Min();

                      ////minValue = Math.Min(minValue, min);
                      ////maxValue = Math.Max(maxValue, max);
                      ////minValue = Math.Floor(minValue / 0.25d) * 0.25d;
                      ////maxValue = Math.Ceiling(maxValue / 0.25d) * 0.25d;

                      bool isDataNew = true;
                      int index = -1;

                      for (int i = 0; i < SeriesCollection.Count; i++)
                      {
                          LiveCharts.Definitions.Series.ISeriesView item = SeriesCollection[i];
                          if (item.Title == LastUpdateDate)
                          {
                              isDataNew = false;
                              index = i;
                          }
                      }

                      List<ObservablePoint> points = new List<ObservablePoint>();

                      foreach (var item in task.Result.data.rows)
                      {
                          ObservablePoint point = new ObservablePoint(item.period, item.value);
                          points.Add(point);
                      }

                      if (isDataNew)
                      {
                          SeriesCollection.Add(new LineSeries
                          {
                              Values = new ChartValues<ObservablePoint>(points),
                              Title = LastUpdateDate,
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

                          //for (int i = 0; i < data.Length; i++)
                          //{
                          //    double item = data[i];
                          //    SeriesCollection[index].Values[i] = item;
                          //}
                      }

                      //LabelsX.Clear();
                      //foreach (var item in labels)
                      //{
                      //    LabelsX.Add(item.ToString());
                      //}

                      //SeriesCollection[0].Values.Add(item.value);
                      //}

                      //var axis = MainChart.AxisY[0];
                      //axis.MaxValue = maxValue;
                      //axis.MinValue = minValue;
                      //Separator separator = new Separator { Step = 0.25d, IsEnabled = false };
                      //axis.Separator = separator;
                  }
              }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            if (CalendarControl.SelectedDate.HasValue)
                updateChart(CalendarControl.SelectedDate.Value);
        }

        private void CalendarControl_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            ToggleButton.IsChecked = false;
        }

        private void MainChart_DataClick(object sender, ChartPoint chartPoint)
        {
            //MyPopsup.IsOpen = false;
            //MyPopsup.IsOpen = true;

            var data2 = getWeeklyOhlcs(chartPoint.X);


            var chart = new CartesianChart();
            chart.DisableAnimations = true;
            var seriesCollection = new SeriesCollection
            {
                new OhlcSeries
                {
                    Values = data2
                }
            };


            int begIdx;
            int element;
            double[] outData = new double[data2.Count];

            var res = Core.Roc(0, data2.Count - 1, data2.Select(d => d.Close).ToArray(), 13, out begIdx, out element, outData);

            var rocData = new ChartValues<double>();

            for (int i = 0; i < data2.Count; i++)
            {
                double item = 0d;
                if (i >= begIdx)
                    item = outData[i- begIdx];
                rocData.Add(item);
            }

            if (res == Core.RetCode.Success)
            {
                Debug.WriteLine($"{begIdx} {element}");
                var lineSeries = new LineSeries
                {
                    Values = rocData,
                };
                seriesCollection.Add(lineSeries);
            }


            chart.Series = seriesCollection;
            DataTab.Content = chart;

            //using (var db = new MoexContext())
            //{
            //    var output = db.Zcycs
            //        .Where(d => d.Period == chartPoint.X && d.Tradedate >= DateTime.Today.AddYears(-1))
            //        .ToList();

            //    var chart = new CartesianChart();

            //    //var mapper1 = Mappers.Xy<ObservablePoint>().X(point => point.X).Y(point => point.Y);
            //    var seriesCollection = new SeriesCollection();


            //    var data = new ChartValues<double>();
            //    data.AddRange(output.Select(d => d.Value).ToList());

            //    seriesCollection.Add(new LineSeries
            //    {
            //        Values = data,
            //        PointGeometry = null
            //    });

            //    chart.Series = seriesCollection;

            //    DataTab.Content = chart;
            //    DataTab.IsSelected = true;
            //}



        }

        private void DeleteChart_Click(object sender, RoutedEventArgs e)
        {
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;

            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value)
            {
                timer.Start();
            }
            else if (timer.IsEnabled)
            {
                timer.Stop();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in SeriesCollection)
            {
                //item.Values.Clear();
                //item.Values.Remove();
            }
            SeriesCollection.Clear();
        }
    }
}