using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurveAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfzCashflowEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfzCashflowEvents",
                columns: table => new
                {
                    SecId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SourceKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    EventDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecordDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Value = table.Column<double>(type: "REAL", nullable: true),
                    ValueRub = table.Column<double>(type: "REAL", nullable: true),
                    ValuePercent = table.Column<double>(type: "REAL", nullable: true),
                    FaceValue = table.Column<double>(type: "REAL", nullable: true),
                    InitialFaceValue = table.Column<double>(type: "REAL", nullable: true),
                    FaceUnit = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Price = table.Column<double>(type: "REAL", nullable: true),
                    Agent = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    OfferType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SourceLabel = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LoadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsProvisional = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfzCashflowEvents", x => new { x.SecId, x.EventType, x.EventDate, x.SourceKind, x.SourceKey });
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfzCashflowEvents_EventDate",
                table: "OfzCashflowEvents",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_OfzCashflowEvents_SecId_EventDate",
                table: "OfzCashflowEvents",
                columns: new[] { "SecId", "EventDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfzCashflowEvents");
        }
    }
}
