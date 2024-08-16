using System;
using System.Windows;
using CurveAnalyzer.ViewModel;

namespace CurveAnalyzer
{
    public partial class MainWindow : Window
    {
        private MainViewModel _mainViewModel;
        public MainWindow()
        {
            InitializeComponent();

            DataContextChanged += MainWindow_DataContextChanged;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _mainViewModel?.Initialize();
        }

        private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MainViewModel newModel)
            {
                _mainViewModel = newModel;
            }
        }
    }
}
