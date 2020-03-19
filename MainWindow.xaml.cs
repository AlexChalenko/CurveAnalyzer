using LiveCharts;
using LiveCharts.Wpf;
using MoexData;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace CurveAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly string DataUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?date={0}&iss.only=yearyields&iss.meta=off";
        //readonly string OldDataUrl = https://iss.moex.com/iss/engines/state/markets/zcyc.json

        public SeriesCollection SeriesCollection { get; private set; }
        public ObservableCollection<string> LabelsX { get; set; }

        public Func<double, string> XFormatter { get; set; }
        public IssData IssData { get; set; }
        private int daysBack;
        private string lastUpdateDate;
        private Double minValue = double.MaxValue;
        private double maxValue = double.MinValue;

        private DispatcherTimer timer;

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

        public MainWindow()
        {
            LabelsX = new ObservableCollection<string>();
            SeriesCollection = new SeriesCollection();

            XFormatter = x => x.ToString("000.00000");
            DataContext = this;
            InitializeComponent();

            MainChart.LegendLocation = LegendLocation.Right;
            CalendarControl.SelectedDate = DateTime.Now;
            LastUpdateDate = CalendarControl.SelectedDate.Value.ToShortDateString();
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 1, 0);
            timer.Tick += onTimerTick;
        }

        private void onTimerTick(object sender, EventArgs e)
        {
            updateChart(DateTime.Today);

        }

        public Task<bool> GetData(DateTime date)
        {
            var tcs = new TaskCompletionSource<bool>();

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
                    IssData = data;
                    tcs.TrySetResult(true);
                }
                else
                {
                    tcs.TrySetResult(false);
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void updateChart(DateTime dateTime)
        {

            LastUpdateDate = dateTime.ToShortDateString();

            GetDataButton.IsEnabled = false;
            GetData(dateTime).ContinueWith(task =>
            {

                LabelsX.Clear();
                //SeriesCollection[0].Values.Clear();

                if (task.IsCanceled || task.Exception != null)
                {
                }
                else
                {
                    var data = IssData.data.rows.Select(x => x.value).ToArray();
                    var labels = IssData.data.rows.Select(x => x.period).ToArray();

                    double max = data.Max();
                    double min = data.Min();

                    minValue = Math.Min(minValue, min);
                    maxValue = Math.Max(maxValue, max);
                    minValue = Math.Floor(minValue / 0.25d) * 0.25d;
                    maxValue = Math.Ceiling(maxValue / 0.25d) * 0.25d;

                    bool isDataNew = true;
                    int index = -1;

                    foreach (var item in SeriesCollection)
                    {
                        if (item.Title == LastUpdateDate)
                        {
                            isDataNew = false;
                            index = SeriesCollection.IndexOf(item);
                        }
                    }

                    if (isDataNew)
                    {
                        SeriesCollection.Add(new LineSeries
                        {
                            Values = new ChartValues<double>(),
                            Title = LastUpdateDate.ToString()
                        });
                        var currentIndex = SeriesCollection.CurrentSeriesIndex;
                        foreach (var item in data)
                        {
                            SeriesCollection[currentIndex].Values.Add(item);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            double item = data[i];
                            SeriesCollection[index].Values[i] = item;
                        }
                    }
                    LabelsX.Clear();
                    foreach (var item in labels)
                    {
                        LabelsX.Add(item.ToString());
                    }

                    //SeriesCollection[0].Values.Add(item.value);
                    //}

                    var axis = MainChart.AxisY[0];
                    axis.MaxValue = maxValue;
                    axis.MinValue = minValue;
                    //Separator separator = new Separator { Step = 0.25d, IsEnabled = false };
                    //axis.Separator = separator;
                    GetDataButton.IsEnabled = true;
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            if (CalendarControl.SelectedDate.HasValue)
                updateChart(CalendarControl.SelectedDate.Value);
        }

        private void CalendarControl_SelectedDatesChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ToggleButton.IsChecked = false;
        }

        private void MainChart_DataClick(object sender, ChartPoint chartPoint)
        {
            //MyPopsup.IsOpen = false;
            //MyPopsup.IsOpen = true;
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