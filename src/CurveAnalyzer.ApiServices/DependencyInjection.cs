using CurveAnalyzer.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CurveAnalyzer.ApiServices;

public static class DependencyInjection
{
    public static IServiceCollection AddMoexApiServices(this IServiceCollection services)
    {
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IDataService, OnlineDataService>();
        services.AddSingleton<IOfzActivityDataService, OfzActivityOnlineDataService>();
        services.AddSingleton<IOfzIndexDataService, OfzIndexOnlineDataService>();
        services.AddSingleton<IOfzCashflowDataService, OfzCashflowOnlineDataService>();
        services.AddSingleton<ICbrKeyRateDataService, CbrKeyRateDataService>();

        return services;
    }
}
