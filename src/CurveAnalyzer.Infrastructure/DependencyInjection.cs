using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurveAnalyzer.Infrastructure;

public static class DependencyInjection
{
    private const string DefaultConnectionString = "Data Source=zcyc.db";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MoexHistory") ?? DefaultConnectionString;

        services.AddDbContext<MoexContext>(options =>
            options
                .UseSqlite(connectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IZcycRepository, ZcycRepository>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }
}
