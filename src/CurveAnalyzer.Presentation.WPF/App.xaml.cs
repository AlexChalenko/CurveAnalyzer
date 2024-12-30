using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using CurveAnalyzer.ApiServices;
using CurveAnalyzer.Application;
using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Infrastructure;
using CurveAnalyzer.Infrastructure.Repositories;
using CurveAnalyzer.Interfaces;
using CurveAnalyzer.Presentation.WPF.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CurveAnalyzer.Presentation.WPF;

public partial class App : System.Windows.Application
{
    private IHost _host;

    private IServiceProvider _serviceProvider => _host.Services;
    public new static App Current => (App)System.Windows.Application.Current;

    public App()
    {
        _host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();
                services.AddScoped<IZcycRepository, ZcycRepository>();
                services.AddDbContext<MoexContext>(options =>
                {
                    options.UseSqlite("Data Source = zcyc.db");
                    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                });
                services.AddScoped<IDataService, OnlineDataService>();
                services.AddScoped<IHistoryDataService, HistoryDataService>();
                services.AddScoped<DataSyncService>();
                //services.AddSingleton<YieldCurveControl>();
                services.AddSingleton<YieldCurveViewModel>();
                services.AddSingleton<RateChartViewModel>();
                services.AddSingleton<SpreadChartViewModel>();
                services.AddSingleton<IMessenger, WeakReferenceMessenger>();
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _host.Start();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);

    }
}
