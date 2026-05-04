using CurveAnalyzer.Core;
using Microsoft.EntityFrameworkCore;

namespace CurveAnalyzer.Infrastructure;

public class MoexContext : DbContext
{
    public DbSet<Zcyc> Zcycs { get; set; }
    public DbSet<OfzIssue> OfzIssues { get; set; }
    public DbSet<OfzDailyTrade> OfzDailyTrades { get; set; }
    public DbSet<OfzActivityLoadState> OfzActivityLoadStates { get; set; }

    public MoexContext(DbContextOptions<MoexContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var zcyc = modelBuilder.Entity<Zcyc>();

        zcyc.HasKey(z => z.Num);

        zcyc.HasIndex(z => new { z.Tradedate, z.Period })
            .HasDatabaseName("IX_Zcycs_Tradedate_Period");

        zcyc.HasIndex(z => new { z.Period, z.Tradedate })
            .HasDatabaseName("IX_Zcycs_Period_Tradedate");

        var issue = modelBuilder.Entity<OfzIssue>();
        issue.HasKey(i => i.SecId);
        issue.Property(i => i.SecId).HasMaxLength(32);
        issue.Property(i => i.ShortName).HasMaxLength(128);
        issue.Property(i => i.SecName).HasMaxLength(256);
        issue.Property(i => i.IssueName).HasMaxLength(512);
        issue.Property(i => i.Isin).HasMaxLength(32);
        issue.Property(i => i.FaceUnit).HasMaxLength(16);
        issue.Property(i => i.CurrencyId).HasMaxLength(16);
        issue.Property(i => i.BondType).HasMaxLength(64);
        issue.Property(i => i.BondSubType).HasMaxLength(64);
        issue.Ignore(i => i.IsRub);
        issue.Ignore(i => i.IsStandardOfz);
        issue.Ignore(i => i.CouponType);
        issue.Ignore(i => i.CouponTypeMarker);
        issue.Ignore(i => i.CurrencyMarker);
        issue.Ignore(i => i.TypeMarker);
        issue.Ignore(i => i.DisplayMarker);

        var trade = modelBuilder.Entity<OfzDailyTrade>();
        trade.HasKey(t => new { t.BoardId, t.SecId, t.TradeDate });
        trade.Property(t => t.BoardId).HasMaxLength(16);
        trade.Property(t => t.SecId).HasMaxLength(32);
        trade.Ignore(t => t.PreferredYield);
        trade.Ignore(t => t.PreferredPrice);
        trade.HasOne(t => t.Issue)
            .WithMany()
            .HasForeignKey(t => t.SecId)
            .HasPrincipalKey(i => i.SecId);
        trade.HasIndex(t => t.TradeDate)
            .HasDatabaseName("IX_OfzDailyTrades_TradeDate");
        trade.HasIndex(t => new { t.SecId, t.TradeDate })
            .HasDatabaseName("IX_OfzDailyTrades_SecId_TradeDate");
        trade.HasIndex(t => new { t.TradeDate, t.Duration })
            .HasDatabaseName("IX_OfzDailyTrades_TradeDate_Duration");

        var loadState = modelBuilder.Entity<OfzActivityLoadState>();
        loadState.HasKey(s => new { s.BoardId, s.TradeDate });
        loadState.Property(s => s.BoardId).HasMaxLength(16);
        loadState.Property(s => s.ErrorMessage).HasMaxLength(512);
        loadState.Property(s => s.IsProvisional).HasDefaultValue(false);
        loadState.HasIndex(s => s.TradeDate)
            .HasDatabaseName("IX_OfzActivityLoadStates_TradeDate");
    }
}
