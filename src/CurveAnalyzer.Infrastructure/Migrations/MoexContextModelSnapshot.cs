using CurveAnalyzer.Core;
using CurveAnalyzer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace CurveAnalyzer.Infrastructure.Migrations;

[DbContextAttribute(typeof(MoexContext))]
partial class MoexContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.7");

        modelBuilder.Entity<OfzActivityLoadState>(b =>
        {
            b.Property(e => e.BoardId)
                .HasMaxLength(16)
                .HasColumnType("TEXT");

            b.Property(e => e.TradeDate)
                .HasColumnType("TEXT");

            b.Property(e => e.ErrorMessage)
                .HasMaxLength(512)
                .HasColumnType("TEXT");

            b.Property(e => e.IsProvisional)
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasDefaultValue(false);

            b.Property(e => e.LoadedAt)
                .HasColumnType("TEXT");

            b.Property(e => e.RowsLoaded)
                .HasColumnType("INTEGER");

            b.Property(e => e.Status)
                .HasColumnType("INTEGER");

            b.HasKey(e => new { e.BoardId, e.TradeDate });

            b.HasIndex(e => e.TradeDate)
                .HasDatabaseName("IX_OfzActivityLoadStates_TradeDate");

            b.ToTable("OfzActivityLoadStates");
        });

        modelBuilder.Entity<OfzDailyTrade>(b =>
        {
            b.Property(e => e.BoardId)
                .HasMaxLength(16)
                .HasColumnType("TEXT");

            b.Property(e => e.SecId)
                .HasMaxLength(32)
                .HasColumnType("TEXT");

            b.Property(e => e.TradeDate)
                .HasColumnType("TEXT");

            b.Property(e => e.ClosePrice)
                .HasColumnType("REAL");

            b.Property(e => e.Duration)
                .HasColumnType("REAL");

            b.Property(e => e.HighPrice)
                .HasColumnType("REAL");

            b.Property(e => e.LoadedAt)
                .HasColumnType("TEXT");

            b.Property(e => e.LowPrice)
                .HasColumnType("REAL");

            b.Property(e => e.NumTrades)
                .HasColumnType("INTEGER");

            b.Property(e => e.OpenPrice)
                .HasColumnType("REAL");

            b.Property(e => e.Value)
                .HasColumnType("REAL");

            b.Property(e => e.Volume)
                .HasColumnType("REAL");

            b.Property(e => e.WeightedAveragePrice)
                .HasColumnType("REAL");

            b.Property(e => e.YieldAtWeightedAveragePrice)
                .HasColumnType("REAL");

            b.Property(e => e.YieldClose)
                .HasColumnType("REAL");

            b.Property(e => e.ZSpread)
                .HasColumnType("REAL");

            b.Property(e => e.ZSpreadAtWeightedAveragePrice)
                .HasColumnType("REAL");

            b.HasKey(e => new { e.BoardId, e.SecId, e.TradeDate });

            b.HasIndex(e => new { e.SecId, e.TradeDate })
                .HasDatabaseName("IX_OfzDailyTrades_SecId_TradeDate");

            b.HasIndex(e => e.TradeDate)
                .HasDatabaseName("IX_OfzDailyTrades_TradeDate");

            b.HasIndex(e => new { e.TradeDate, e.Duration })
                .HasDatabaseName("IX_OfzDailyTrades_TradeDate_Duration");

            b.HasOne(e => e.Issue)
                .WithMany()
                .HasForeignKey(e => e.SecId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.ToTable("OfzDailyTrades");
        });

        modelBuilder.Entity<OfzIssue>(b =>
        {
            b.Property(e => e.SecId)
                .HasMaxLength(32)
                .HasColumnType("TEXT");

            b.Property(e => e.BondSubType)
                .HasMaxLength(64)
                .HasColumnType("TEXT");

            b.Property(e => e.BondType)
                .HasMaxLength(64)
                .HasColumnType("TEXT");

            b.Property(e => e.CouponPercent)
                .HasColumnType("REAL");

            b.Property(e => e.CouponPeriod)
                .HasColumnType("INTEGER");

            b.Property(e => e.CouponValue)
                .HasColumnType("REAL");

            b.Property(e => e.CurrencyId)
                .HasMaxLength(16)
                .HasColumnType("TEXT");

            b.Property(e => e.FaceUnit)
                .HasMaxLength(16)
                .HasColumnType("TEXT");

            b.Property(e => e.FaceValue)
                .HasColumnType("REAL");

            b.Property(e => e.InitialFaceValue)
                .HasColumnType("REAL");

            b.Property(e => e.IssueName)
                .HasMaxLength(512)
                .HasColumnType("TEXT");

            b.Property(e => e.IssueSize)
                .HasColumnType("REAL");

            b.Property(e => e.IssueSizePlaced)
                .HasColumnType("REAL");

            b.Property(e => e.Isin)
                .HasMaxLength(32)
                .HasColumnType("TEXT");

            b.Property(e => e.ListLevel)
                .HasColumnType("INTEGER");

            b.Property(e => e.MatDate)
                .HasColumnType("TEXT");

            b.Property(e => e.MetadataLoadedAt)
                .HasColumnType("TEXT");

            b.Property(e => e.NextCouponDate)
                .HasColumnType("TEXT");

            b.Property(e => e.SecName)
                .HasMaxLength(256)
                .HasColumnType("TEXT");

            b.Property(e => e.ShortName)
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("TEXT");

            b.HasKey(e => e.SecId);

            b.Ignore(e => e.CouponType);
            b.Ignore(e => e.CouponTypeMarker);
            b.Ignore(e => e.CurrencyMarker);
            b.Ignore(e => e.DisplayMarker);
            b.Ignore(e => e.IsRub);
            b.Ignore(e => e.IsStandardOfz);
            b.Ignore(e => e.TypeMarker);

            b.ToTable("OfzIssues");
        });

        modelBuilder.Entity<Zcyc>(b =>
        {
            b.Property(e => e.Num)
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasAnnotation("Sqlite:Autoincrement", true);

            b.Property(e => e.Period)
                .HasColumnType("REAL");

            b.Property(e => e.Tradedate)
                .HasColumnType("TEXT");

            b.Property(e => e.Value)
                .HasColumnType("REAL");

            b.HasKey(e => e.Num);

            b.HasIndex(e => new { e.Period, e.Tradedate })
                .HasDatabaseName("IX_Zcycs_Period_Tradedate");

            b.HasIndex(e => new { e.Tradedate, e.Period })
                .HasDatabaseName("IX_Zcycs_Tradedate_Period");

            b.ToTable("Zcycs");
        });
#pragma warning restore 612, 618
    }
}
