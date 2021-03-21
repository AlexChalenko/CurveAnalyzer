using System.Windows;

namespace CurveAnalyzer
{
    public partial class MainWindow : Window
    {
        private MainViewModel mainViewModel;

        public MainWindow()
        {
            DataContext = mainViewModel = new MainViewModel();
            InitializeComponent();
        }
    }
}
