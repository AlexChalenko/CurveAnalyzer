using System;
using System.Windows;
using CurveAnalyzer.Charts;
using CurveAnalyzer.Data;
using CurveAnalyzer.DataProviders;
using CurveAnalyzer.Interfaces;
using CurveAnalyzer.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace CurveAnalyzer
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }

        public App()
        {
            Services = ConfigureServices();
            InitializeComponent();

            DispatcherUnhandledException += (sender, e) =>
            {
                MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDataProvider, OnlineDataProvider>();
            services.AddSingleton<IHistoryDataProvider, HistoryDataProvider>();
            services.AddSingleton<IDataManager, DataManager>();


            services.AddSingleton<MainViewModel>();

            services.AddSingleton<DailyCurveChart>();
            services.AddSingleton<DailyChartViewModel>();

            services.AddSingleton<WeeklyRateDynamicChart>();
            services.AddSingleton<WeeklyChartViewModel>();

            services.AddSingleton<DailySpreadChart>();
            services.AddSingleton<SpreadChartViewModel>();
            return services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow()
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
            mainWindow.Show();
        }
    }
}
