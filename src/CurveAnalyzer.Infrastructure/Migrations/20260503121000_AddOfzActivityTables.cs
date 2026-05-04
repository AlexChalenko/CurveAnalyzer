using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurveAnalyzer.Infrastructure.Migrations;

[DbContextAttribute(typeof(MoexContext))]
[Migration("20260503121000_AddOfzActivityTables")]
public partial class AddOfzActivityTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OfzActivityLoadStates",
            columns: table => new
            {
                BoardId = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                TradeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                RowsLoaded = table.Column<int>(type: "INTEGER", nullable: false),
                LoadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OfzActivityLoadStates", x => new { x.BoardId, x.TradeDate });
            });

        migrationBuilder.CreateTable(
            name: "OfzIssues",
            columns: table => new
            {
                SecId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                ShortName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                SecName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                Isin = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                MatDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                FaceValue = table.Column<double>(type: "REAL", nullable: true),
                FaceUnit = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                CurrencyId = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                CouponPercent = table.Column<double>(type: "REAL", nullable: true),
                BondType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                BondSubType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OfzIssues", x => x.SecId);
            });

        migrationBuilder.CreateTable(
            name: "OfzDailyTrades",
            columns: table => new
            {
                BoardId = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                SecId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                TradeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                NumTrades = table.Column<int>(type: "INTEGER", nullable: true),
                Value = table.Column<double>(type: "REAL", nullable: true),
                Volume = table.Column<double>(type: "REAL", nullable: true),
                OpenPrice = table.Column<double>(type: "REAL", nullable: true),
                LowPrice = table.Column<double>(type: "REAL", nullable: true),
                HighPrice = table.Column<double>(type: "REAL", nullable: true),
                ClosePrice = table.Column<double>(type: "REAL", nullable: true),
                WeightedAveragePrice = table.Column<double>(type: "REAL", nullable: true),
                YieldClose = table.Column<double>(type: "REAL", nullable: true),
                YieldAtWeightedAveragePrice = table.Column<double>(type: "REAL", nullable: true),
                Duration = table.Column<double>(type: "REAL", nullable: true),
                ZSpread = table.Column<double>(type: "REAL", nullable: true),
                ZSpreadAtWeightedAveragePrice = table.Column<double>(type: "REAL", nullable: true),
                LoadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OfzDailyTrades", x => new { x.BoardId, x.SecId, x.TradeDate });
                table.ForeignKey(
                    name: "FK_OfzDailyTrades_OfzIssues_SecId",
                    column: x => x.SecId,
                    principalTable: "OfzIssues",
                    principalColumn: "SecId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OfzActivityLoadStates_TradeDate",
            table: "OfzActivityLoadStates",
            column: "TradeDate");

        migrationBuilder.CreateIndex(
            name: "IX_OfzDailyTrades_SecId_TradeDate",
            table: "OfzDailyTrades",
            columns: ["SecId", "TradeDate"]);

        migrationBuilder.CreateIndex(
            name: "IX_OfzDailyTrades_TradeDate",
            table: "OfzDailyTrades",
            column: "TradeDate");

        migrationBuilder.CreateIndex(
            name: "IX_OfzDailyTrades_TradeDate_Duration",
            table: "OfzDailyTrades",
            columns: ["TradeDate", "Duration"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OfzActivityLoadStates");

        migrationBuilder.DropTable(
            name: "OfzDailyTrades");

        migrationBuilder.DropTable(
            name: "OfzIssues");
    }
}
