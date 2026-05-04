using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace CurveAnalyzer.Infrastructure;

public sealed class DatabaseInitializer(IDbContextFactory<MoexContext> contextFactory) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await DeleteLegacyEnsureCreatedCacheIfNeededAsync(context, cancellationToken).ConfigureAwait(false);
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task DeleteLegacyEnsureCreatedCacheIfNeededAsync(
        MoexContext context,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var databasePath = connection.DataSource;
        if (string.IsNullOrWhiteSpace(databasePath) ||
            databasePath.Equals(":memory:", StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(databasePath))
        {
            return;
        }

        var shouldClose = connection.State == System.Data.ConnectionState.Closed;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        bool shouldDelete;
        try
        {
            var hasZcycs = await HasTableAsync(connection, "Zcycs", cancellationToken).ConfigureAwait(false);
            var hasMigrationHistory = await HasTableAsync(connection, "__EFMigrationsHistory", cancellationToken).ConfigureAwait(false);
            shouldDelete = hasZcycs && !hasMigrationHistory;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        if (shouldDelete)
        {
            DeleteSqliteCacheFiles(databasePath);
        }
    }

    private static async Task<bool> HasTableAsync(
        DbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(result) > 0;
    }

    private static void DeleteSqliteCacheFiles(string databasePath)
    {
        SqliteConnection.ClearAllPools();

        foreach (var path in new[] { databasePath, $"{databasePath}-wal", $"{databasePath}-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
