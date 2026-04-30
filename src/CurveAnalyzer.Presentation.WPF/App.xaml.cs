using System.Windows;
using CurveAnalyzer.ApiServices;
using CurveAnalyzer.Application;
using CurveAnalyzer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WpfApplication = System.Windows.Application;

namespace CurveAnalyzer.Presentation.WPF;

public partial class App : WpfApplication
{
    private readonly IHost _host;

    public new static App Current => (App)WpfApplication.Current;

    public App()
    {
        _host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services
                    .AddApplication()
                    .AddMoexApiServices()
                    .AddInfrastructure(context.Configuration)
                    .AddPresentation();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        await _host.StartAsync();
        await _host.Services
            .GetRequiredService<IDatabaseInitializer>()
            .InitializeAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
