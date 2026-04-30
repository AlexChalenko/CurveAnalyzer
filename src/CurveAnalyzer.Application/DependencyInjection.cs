using CurveAnalyzer.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CurveAnalyzer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHistoryDataService, HistoryDataService>();
        services.AddScoped<DataSyncService>();

        return services;
    }
}
