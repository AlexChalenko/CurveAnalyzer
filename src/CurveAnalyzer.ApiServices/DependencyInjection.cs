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

        return services;
    }
}
