using LiveCharts;
using LiveCharts.Wpf;
using MoexData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;

namespace CurveAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        readonly string DataUrl = "https://iss.moex.com/iss/engines/stock/zcyc.xml?date={0}&iss.only=yearyields&iss.meta=off";
        //readonly string OldDataUrl = https://iss.moex.com/iss/engines/state/markets/zcyc.json

        public SeriesCollection SeriesCollection { get; private set; }
        public ObservableCollection<string> LabelsX { get; set; }

        public IssData IssData { get; set; }
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
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Curve",
                    Values = new ChartValues<double>(),
                }
            };

            DataContext = this;
            InitializeComponent();
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
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        int daysBack;
        private string lastUpdateDate;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            DateTime calcDay;

            do
            {
                daysBack--;
                calcDay = DateTime.Now.AddDays(daysBack);

            } while (calcDay.DayOfWeek == DayOfWeek.Saturday || calcDay.DayOfWeek == DayOfWeek.Sunday);

            LastUpdateDate = calcDay.ToShortDateString();

            GetData(calcDay).ContinueWith(task =>
            {
                LabelsX.Clear();
                SeriesCollection[0].Values.Clear();

                if (task.IsCanceled || task.Exception != null)
                {

                }
                else
                {
                    double max = IssData.data.rows.Select(x => x.value).Max() * 1.01;
                    double min = IssData.data.rows.Select(x => x.value).Min() * .99;

                    foreach (var item in IssData.data.rows)
                    {
                        SeriesCollection[0].Values.Add(item.value);
                        LabelsX.Add(item.period.ToString());
                    }
                }
            }, TaskScheduler.Current);
        }
    }
}
