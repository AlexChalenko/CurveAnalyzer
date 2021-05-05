using Microsoft.EntityFrameworkCore;
using MoexData;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using CurveAnalyzer.Data;
using CurveAnalyzer.Interfaces;

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
