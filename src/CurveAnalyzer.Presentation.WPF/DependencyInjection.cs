using CurveAnalyzer.Presentation.WPF.ViewModels;
using CurveAnalyzer.Presentation.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CurveAnalyzer.Presentation.WPF;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<YieldCurveViewModel>();
        services.AddSingleton<RateChartViewModel>();
        services.AddSingleton<SpreadChartViewModel>();
        services.AddSingleton<OfzActivityViewModel>();
        services.AddSingleton<YieldCurveControl>();
        services.AddSingleton<RateChartControl>();
        services.AddSingleton<SpreadChartControl>();
        services.AddSingleton<OfzActivityControl>();

        return services;
    }
}
