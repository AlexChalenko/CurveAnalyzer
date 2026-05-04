using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurveAnalyzer.Infrastructure.Migrations;

[DbContextAttribute(typeof(MoexContext))]
[Migration("20260503120000_InitialZcycBaseline")]
public partial class InitialZcycBaseline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Zcycs",
            columns: table => new
            {
                Num = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Tradedate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Period = table.Column<double>(type: "REAL", nullable: false),
                Value = table.Column<double>(type: "REAL", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Zcycs", x => x.Num);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Zcycs_Period_Tradedate",
            table: "Zcycs",
            columns: ["Period", "Tradedate"]);

        migrationBuilder.CreateIndex(
            name: "IX_Zcycs_Tradedate_Period",
            table: "Zcycs",
            columns: ["Tradedate", "Period"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Zcycs");
    }
}
