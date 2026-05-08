using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurveAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfzMarketIndexPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfzMarketIndexPoints",
                columns: table => new
                {
                    SecId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TradeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceKind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Close = table.Column<double>(type: "REAL", nullable: true),
                    Open = table.Column<double>(type: "REAL", nullable: true),
                    High = table.Column<double>(type: "REAL", nullable: true),
                    Low = table.Column<double>(type: "REAL", nullable: true),
                    Value = table.Column<double>(type: "REAL", nullable: true),
                    Yield = table.Column<double>(type: "REAL", nullable: true),
                    Duration = table.Column<double>(type: "REAL", nullable: true),
                    CurrencyId = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    ObservedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TradeSessionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecalcDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LoadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsProvisional = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfzMarketIndexPoints", x => new { x.SecId, x.TradeDate, x.SourceKind });
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfzMarketIndexPoints_SecId_TradeDate",
                table: "OfzMarketIndexPoints",
                columns: new[] { "SecId", "TradeDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OfzMarketIndexPoints_TradeDate",
                table: "OfzMarketIndexPoints",
                column: "TradeDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfzMarketIndexPoints");
        }
    }
}
