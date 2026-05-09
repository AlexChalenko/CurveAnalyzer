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

    [Fact]
    public async Task SaveMarketIndexPointsAsync_UpsertsAndReturnsDateRange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new TestMoexContextFactory();
        var repository = new OfzActivityRepository(factory);
        var firstDate = new DateTime(2026, 05, 07);
        var secondDate = firstDate.AddDays(1);

        await repository.SaveMarketIndexPointsAsync(
            [
                IndexPoint("RGBI", firstDate, close: 100, loadedAt: firstDate),
                IndexPoint("RGBI", secondDate, close: 101, loadedAt: secondDate)
            ],
            cancellationToken);
        await repository.SaveMarketIndexPointsAsync(
            [IndexPoint("RGBI", firstDate, close: 100.5, loadedAt: secondDate)],
            cancellationToken);

        var points = await repository.GetMarketIndexPointsAsync(firstDate, secondDate, cancellationToken);

        Assert.Collection(
            points,
            first =>
            {
                Assert.Equal(firstDate, first.TradeDate);
                Assert.Equal(100.5, first.Close);
            },
            second =>
            {
                Assert.Equal(secondDate, second.TradeDate);
                Assert.Equal(101, second.Close);
            });
    }

    [Fact]
    public async Task SaveMarketIndexPointsAsync_KeepsHistoryAndSnapshotCacheEntries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new TestMoexContextFactory();
        var repository = new OfzActivityRepository(factory);
        var date = new DateTime(2026, 05, 08);

        await repository.SaveMarketIndexPointsAsync(
            [
                IndexPoint("RGBI", date, close: 100, sourceKind: OfzMarketIndexSourceKind.History),
                IndexPoint("RGBI", date, close: 100.2, sourceKind: OfzMarketIndexSourceKind.Snapshot, isProvisional: true)
            ],
            cancellationToken);

        var restartedRepository = new OfzActivityRepository(factory);
        var points = await restartedRepository.GetMarketIndexPointsAsync(date, date, cancellationToken);

        Assert.Equal(2, points.Count);
        Assert.Contains(points, point => point.SourceKind == OfzMarketIndexSourceKind.History && point.Close == 100);
        Assert.Contains(points, point => point.SourceKind == OfzMarketIndexSourceKind.Snapshot && point.IsProvisional);
    }

    [Fact]
    public async Task SaveMarketIndexPointsAsync_DeduplicatesAfterSecIdNormalization()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new TestMoexContextFactory();
        var repository = new OfzActivityRepository(factory);
        var date = new DateTime(2026, 05, 08);

        await repository.SaveMarketIndexPointsAsync(
            [
                IndexPoint("RGBI", date, close: 100, loadedAt: date),
                IndexPoint(" RGBI ", date, close: 100.4, loadedAt: date.AddMinutes(1))
            ],
            cancellationToken);

        var point = Assert.Single(await repository.GetMarketIndexPointsAsync(date, date, cancellationToken));

        Assert.Equal("RGBI", point.SecId);
        Assert.Equal(100.4, point.Close);
    }

    [Fact]
    public async Task SaveCashflowEventsAsync_UpsertsAndReturnsDateRange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new TestMoexContextFactory();
        var repository = new OfzActivityRepository(factory);
        var firstDate = new DateTime(2026, 05, 20);
        var secondDate = firstDate.AddDays(10);

        await repository.SaveCashflowEventsAsync(
            [
                CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Coupon, firstDate, value: 12.3, loadedAt: firstDate),
                CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Maturity, secondDate, value: 1_000, loadedAt: secondDate)
            ],
            cancellationToken);
        await repository.SaveCashflowEventsAsync(
            [CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Coupon, firstDate, value: 12.7, loadedAt: secondDate)],
            cancellationToken);

        var events = await repository.GetCashflowEventsAsync(
            ["SU26238RMFS4"],
            firstDate,
            secondDate,
            cancellationToken);

        Assert.Collection(
            events,
            first =>
            {
                Assert.Equal(firstDate, first.EventDate);
                Assert.Equal(OfzCashflowEventType.Coupon, first.EventType);
                Assert.Equal(12.7, first.Value);
                Assert.Equal(12.7, first.ValueRub);
            },
            second =>
            {
                Assert.Equal(secondDate, second.EventDate);
                Assert.Equal(OfzCashflowEventType.Maturity, second.EventType);
                Assert.Equal(1_000, second.Value);
            });
    }

    [Fact]
    public async Task SaveCashflowEventsAsync_NormalizesZeroCouponValuesAndKeepsDistinctSourceKinds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new TestMoexContextFactory();
        var repository = new OfzActivityRepository(factory);
        var date = new DateTime(2026, 05, 20);

        await repository.SaveCashflowEventsAsync(
            [
                CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Coupon, date, value: 0, sourceKind: OfzCashflowSourceKind.Schedule),
                CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Coupon, date, value: 12, sourceKind: OfzCashflowSourceKind.Snapshot, isProvisional: true)
            ],
            cancellationToken);

        var events = await repository.GetCashflowEventsAsync(["SU26238RMFS4"], date, date, cancellationToken);

        Assert.Equal(2, events.Count);
        Assert.Contains(events, item => item.SourceKind == OfzCashflowSourceKind.Schedule && item.Value is null);
        Assert.Contains(events, item => item.SourceKind == OfzCashflowSourceKind.Snapshot && item.IsProvisional);
    }

    [Fact]
    public async Task SaveCashflowEventsAsync_KeepsZeroValuesForNonCouponEvents()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new TestMoexContextFactory();
        var repository = new OfzActivityRepository(factory);
        var date = new DateTime(2026, 05, 20);

        await repository.SaveCashflowEventsAsync(
            [CashflowEvent("SU26238RMFS4", OfzCashflowEventType.Maturity, date, value: 0)],
            cancellationToken);

        var cashflowEvent = Assert.Single(await repository.GetCashflowEventsAsync(
            ["SU26238RMFS4"],
            date,
            date,
            cancellationToken));

        Assert.Equal(0, cashflowEvent.Value);
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

    private static OfzMarketIndexPoint IndexPoint(
        string secId,
        DateTime tradeDate,
        double close,
        DateTime? loadedAt = null,
        OfzMarketIndexSourceKind sourceKind = OfzMarketIndexSourceKind.History,
        bool isProvisional = false)
    {
        return new OfzMarketIndexPoint
        {
            SecId = secId,
            ShortName = secId,
            TradeDate = tradeDate.Date,
            Close = close,
            SourceKind = sourceKind,
            LoadedAt = loadedAt ?? DateTime.UtcNow,
            IsProvisional = isProvisional
        };
    }

    private static OfzCashflowEvent CashflowEvent(
        string secId,
        OfzCashflowEventType eventType,
        DateTime eventDate,
        double? value,
        DateTime? loadedAt = null,
        OfzCashflowSourceKind sourceKind = OfzCashflowSourceKind.Schedule,
        bool isProvisional = false)
    {
        return new OfzCashflowEvent
        {
            SecId = secId,
            SourceKey = $"{eventType}:{eventDate:yyyy-MM-dd}:{sourceKind}",
            ShortName = "ОФЗ 26238",
            EventType = eventType,
            EventDate = eventDate.Date,
            Value = value,
            ValueRub = null,
            ValuePercent = value,
            FaceUnit = "SUR",
            SourceKind = sourceKind,
            SourceLabel = sourceKind.ToString(),
            LoadedAt = loadedAt ?? DateTime.UtcNow,
            IsProvisional = isProvisional
        };
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
