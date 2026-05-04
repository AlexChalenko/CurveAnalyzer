using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurveAnalyzer.Infrastructure.Migrations;

[DbContextAttribute(typeof(MoexContext))]
[Migration("20260504150000_AddOfzLiquidityStorage")]
public partial class AddOfzLiquidityStorage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "Bid",
            table: "OfzDailyTrades",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "Offer",
            table: "OfzDailyTrades",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "Spread",
            table: "OfzDailyTrades",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "HighBid",
            table: "OfzDailyTrades",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "LowOffer",
            table: "OfzDailyTrades",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "ImpliedFloatingRate",
            table: "OfzDailyTrades",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "ImpliedInflation",
            table: "OfzDailyTrades",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "ImpliedCbrRate",
            table: "OfzDailyTrades",
            type: "REAL",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "OfzLiquiditySnapshots",
            columns: table => new
            {
                BoardId = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                SecId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                TradeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                ObservedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Bid = table.Column<double>(type: "REAL", nullable: true),
                Offer = table.Column<double>(type: "REAL", nullable: true),
                Spread = table.Column<double>(type: "REAL", nullable: true),
                BidDepth = table.Column<double>(type: "REAL", nullable: true),
                OfferDepth = table.Column<double>(type: "REAL", nullable: true),
                BidDepthTotal = table.Column<double>(type: "REAL", nullable: true),
                OfferDepthTotal = table.Column<double>(type: "REAL", nullable: true),
                NumBids = table.Column<int>(type: "INTEGER", nullable: true),
                NumOffers = table.Column<int>(type: "INTEGER", nullable: true),
                ValueToday = table.Column<double>(type: "REAL", nullable: true),
                VolumeToday = table.Column<double>(type: "REAL", nullable: true),
                NumTrades = table.Column<int>(type: "INTEGER", nullable: true),
                EffectiveYield = table.Column<double>(type: "REAL", nullable: true),
                EffectiveYieldAtWeightedAveragePrice = table.Column<double>(type: "REAL", nullable: true),
                Duration = table.Column<double>(type: "REAL", nullable: true),
                DurationAtWeightedAveragePrice = table.Column<double>(type: "REAL", nullable: true),
                ZSpread = table.Column<double>(type: "REAL", nullable: true),
                ZSpreadAtWeightedAveragePrice = table.Column<double>(type: "REAL", nullable: true),
                ZSpreadBp = table.Column<double>(type: "REAL", nullable: true),
                GSpreadBp = table.Column<double>(type: "REAL", nullable: true),
                ImpliedFloatingRate = table.Column<double>(type: "REAL", nullable: true),
                ImpliedInflation = table.Column<double>(type: "REAL", nullable: true),
                ImpliedCbrRate = table.Column<double>(type: "REAL", nullable: true),
                IsProvisional = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OfzLiquiditySnapshots", x => new { x.BoardId, x.SecId, x.TradeDate });
            });

        migrationBuilder.CreateIndex(
            name: "IX_OfzLiquiditySnapshots_SecId_TradeDate",
            table: "OfzLiquiditySnapshots",
            columns: ["SecId", "TradeDate"]);

        migrationBuilder.CreateIndex(
            name: "IX_OfzLiquiditySnapshots_TradeDate",
            table: "OfzLiquiditySnapshots",
            column: "TradeDate");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OfzLiquiditySnapshots");

        migrationBuilder.DropColumn(
            name: "Bid",
            table: "OfzDailyTrades");

        migrationBuilder.DropColumn(
            name: "Offer",
            table: "OfzDailyTrades");

        migrationBuilder.DropColumn(
            name: "Spread",
            table: "OfzDailyTrades");

        migrationBuilder.DropColumn(
            name: "HighBid",
            table: "OfzDailyTrades");

        migrationBuilder.DropColumn(
            name: "LowOffer",
            table: "OfzDailyTrades");

        migrationBuilder.DropColumn(
            name: "ImpliedFloatingRate",
            table: "OfzDailyTrades");

        migrationBuilder.DropColumn(
            name: "ImpliedInflation",
            table: "OfzDailyTrades");

        migrationBuilder.DropColumn(
            name: "ImpliedCbrRate",
            table: "OfzDailyTrades");
    }
}
