using CurveAnalyzer.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CurveAnalyzer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IHistoryDataService, HistoryDataService>();
        services.AddSingleton<DataSyncService>();
        services.AddSingleton<OfzActivityService>();

        return services;
    }
}
