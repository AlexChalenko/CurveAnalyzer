using CurveAnalyzer.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CurveAnalyzer.ApiServices;

public static class DependencyInjection
{
    public static IServiceCollection AddMoexApiServices(this IServiceCollection services)
    {
        services.AddSingleton<HttpClient>();
        services.AddScoped<IDataService, OnlineDataService>();

        return services;
    }
}
