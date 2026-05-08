using CurveAnalyzer.Application.Interfaces;
using CurveAnalyzer.Core;
using CurveAnalyzer.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CurveAnalyzer.Infrastructure.Tests.Repositories;

public sealed class OfzActivityRepositoryTests
{
    [Fact]
    public async Task SaveDailyDataAsync_UpsertsIssueMetadataWithoutDowngradingReliableClassification()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new TestMoexContextFactory();
        var repository = new OfzActivityRepository(factory);
        var firstDate = new DateTime(2026, 05, 04);
        var secondDate = firstDate.AddDays(1);

        var reliableIssue = new OfzIssue
        {
            SecId = "SU29019RMFS5",
            ShortName = "ОФЗ 29019",
            SecName = "ОФЗ 29019",
            FaceUnit = "SUR",
            CurrencyId = "SUR",
            BondType = "Флоатер",
            ClassificationSource = OfzIssueClassificationSources.History,
            MetadataLoadedAt = firstDate
        };

        var weakSnapshotIssue = new OfzIssue
        {
            SecId = "SU29019RMFS5",
            ShortName = "SU29019RMFS5",
            FaceUnit = "SUR",
            CurrencyId = "SUR",
            ClassificationSource = OfzIssueClassificationSources.Snapshot,
            MetadataLoadedAt = secondDate
        };

        await repository.SaveDailyDataAsync(DailyData(firstDate, reliableIssue), cancellationToken);
        await repository.SaveDailyDataAsync(DailyData(secondDate, weakSnapshotIssue), cancellationToken);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var issue = Assert.Single(await context.OfzIssues.AsNoTracking().ToListAsync(cancellationToken));

        Assert.Equal("ОФЗ 29019", issue.ShortName);
        Assert.Equal("Флоатер", issue.BondType);
        Assert.Equal(OfzCouponType.Floating, issue.NormalizedCouponType);
        Assert.Equal("ОФЗ-ПК", issue.NormalizedTypeMarker);
        Assert.Equal(OfzClassificationReliability.Reliable, issue.ClassificationReliability);
        Assert.Equal(OfzIssueClassificationSources.History, issue.ClassificationSource);
        Assert.Equal("RUB", issue.NominalCurrency);
        Assert.Equal(2, await context.OfzDailyTrades.CountAsync(cancellationToken));
        Assert.Equal(2, await context.OfzActivityLoadStates.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task SaveDailyDataAsync_ReplacesDailyTradesWithoutDuplicatingIssues()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new TestMoexContextFactory();
        var repository = new OfzActivityRepository(factory);
        var date = new DateTime(2026, 05, 04);
        var issue = new OfzIssue
        {
            SecId = "SU26238RMFS4",
            ShortName = "ОФЗ 26238",
            FaceUnit = "SUR",
            CurrencyId = "SUR",
            BondType = "Постоянный купон",
            ClassificationSource = OfzIssueClassificationSources.History
        };

        await repository.SaveDailyDataAsync(DailyData(date, issue, value: 100_000_000), cancellationToken);
        await repository.SaveDailyDataAsync(DailyData(date, issue, value: 250_000_000), cancellationToken);

        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var trade = Assert.Single(await context.OfzDailyTrades.AsNoTracking().ToListAsync(cancellationToken));

        Assert.Equal(250_000_000, trade.Value);
        Assert.Single(await context.OfzIssues.AsNoTracking().ToListAsync(cancellationToken));
        Assert.Single(await context.OfzActivityLoadStates.AsNoTracking().ToListAsync(cancellationToken));
    }

    [Fact]
    public async Task SaveCbrKeyRatesAsync_UpsertsAndReturnsDateRange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new TestMoexContextFactory();
        var repository = new OfzActivityRepository(factory);
        var firstDate = new DateTime(2026, 05, 07);
        var secondDate = firstDate.AddDays(1);

        await repository.SaveCbrKeyRatesAsync(
            [
                new CbrKeyRate { Date = firstDate, Rate = 14.25, LoadedAt = firstDate },
                new CbrKeyRate { Date = secondDate, Rate = 14.50, LoadedAt = secondDate }
            ],
            cancellationToken);
        await repository.SaveCbrKeyRatesAsync(
            [new CbrKeyRate { Date = firstDate, Rate = 14.00, LoadedAt = secondDate }],
            cancellationToken);

        var rates = await repository.GetCbrKeyRatesAsync(firstDate, secondDate, cancellationToken);

        Assert.Collection(
            rates,
            first =>
            {
                Assert.Equal(firstDate, first.Date);
                Assert.Equal(14.00, first.Rate);
            },
            second =>
            {
                Assert.Equal(secondDate, second.Date);
                Assert.Equal(14.50, second.Rate);
            });
    }

    private static OfzActivityDailyData DailyData(
        DateTime date,
        OfzIssue issue,
        double value = 100_000_000)
    {
        const string boardId = "TQOB";

        return new OfzActivityDailyData(
            boardId,
            date.Date,
            [issue],
            [
                new OfzDailyTrade
                {
                    BoardId = boardId,
                    SecId = issue.SecId,
                    TradeDate = date.Date,
                    Value = value,
                    NumTrades = 10,
                    WeightedAveragePrice = 100,
                    YieldAtWeightedAveragePrice = 13
                }
            ],
            new OfzActivityLoadState
            {
                BoardId = boardId,
                TradeDate = date.Date,
                Status = OfzActivityLoadStatus.Loaded,
                RowsLoaded = 1,
                LoadedAt = DateTime.UtcNow
            });
    }

    private sealed class TestMoexContextFactory : IDbContextFactory<MoexContext>, IDisposable
    {
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<MoexContext> options;

        public TestMoexContextFactory()
        {
            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            options = new DbContextOptionsBuilder<MoexContext>()
                .UseSqlite(connection)
                .Options;

            using var context = new MoexContext(options);
            context.Database.EnsureCreated();
        }

        public MoexContext CreateDbContext() => new(options);

        public Task<MoexContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        public void Dispose() => connection.Dispose();
    }
}
