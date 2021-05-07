using System;
using System.Windows;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;
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
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDataManager, DataManager>();
            return services.BuildServiceProvider();
        }
    }
}
