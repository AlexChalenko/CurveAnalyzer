using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CurveAnalyzer.Infrastructure;

public sealed class MoexContextDesignTimeFactory : IDesignTimeDbContextFactory<MoexContext>
{
    private const string ApplicationDataFolderName = "CurveAnalyzer";
    private const string DatabaseFileName = "zcyc.db";

    public MoexContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MoexContext>()
            .UseSqlite(CreateDefaultConnectionString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new MoexContext(options);
    }

    private static string CreateDefaultConnectionString()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(localApplicationData, ApplicationDataFolderName);
        Directory.CreateDirectory(directory);

        return new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(directory, DatabaseFileName)
        }.ToString();
    }
}
