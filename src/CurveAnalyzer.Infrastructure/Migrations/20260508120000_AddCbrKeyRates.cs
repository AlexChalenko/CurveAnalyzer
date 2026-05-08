using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurveAnalyzer.Infrastructure.Migrations;

[DbContextAttribute(typeof(MoexContext))]
[Migration("20260508120000_AddCbrKeyRates")]
public partial class AddCbrKeyRates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CbrKeyRates",
            columns: table => new
            {
                Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                Rate = table.Column<double>(type: "REAL", nullable: false),
                LoadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CbrKeyRates", x => x.Date);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CbrKeyRates_Date",
            table: "CbrKeyRates",
            column: "Date");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CbrKeyRates");
    }
}
