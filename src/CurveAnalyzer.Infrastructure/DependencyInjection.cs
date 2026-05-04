using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurveAnalyzer.Infrastructure;

public static class DependencyInjection
{
    private const string ApplicationDataFolderName = "CurveAnalyzer";
    private const string DatabaseFileName = "zcyc.db";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(
            configuration.GetConnectionString("MoexHistory") ?? CreateDefaultConnectionString());

        services.AddDbContextFactory<MoexContext>(options =>
            options
                .UseSqlite(connectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddSingleton<IZcycRepository, ZcycRepository>();
        services.AddSingleton<IOfzActivityRepository, OfzActivityRepository>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }

    private static string CreateDefaultConnectionString()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var databasePath = Path.Combine(localApplicationData, ApplicationDataFolderName, DatabaseFileName);

        return new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();
    }

    private static string ResolveConnectionString(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return builder.ToString();
        }

        dataSource = Environment.ExpandEnvironmentVariables(dataSource);
        if (!Path.IsPathRooted(dataSource))
        {
            var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            dataSource = Path.Combine(localApplicationData, ApplicationDataFolderName, dataSource);
        }

        var directory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        builder.DataSource = dataSource;
        return builder.ToString();
    }
}
