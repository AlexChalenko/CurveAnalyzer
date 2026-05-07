using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurveAnalyzer.Infrastructure.Migrations;

[DbContextAttribute(typeof(MoexContext))]
[Migration("20260506120000_AddOfzIssueClassificationMetadata")]
public partial class AddOfzIssueClassificationMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NormalizedCouponType",
            table: "OfzIssues",
            type: "TEXT",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NormalizedTypeMarker",
            table: "OfzIssues",
            type: "TEXT",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ClassificationReliability",
            table: "OfzIssues",
            type: "TEXT",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ClassificationSource",
            table: "OfzIssues",
            type: "TEXT",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ClassificationEvidence",
            table: "OfzIssues",
            type: "TEXT",
            maxLength: 1024,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ClassificationLoadedAt",
            table: "OfzIssues",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsIndexedNominal",
            table: "OfzIssues",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsAmortizing",
            table: "OfzIssues",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NominalCurrency",
            table: "OfzIssues",
            type: "TEXT",
            maxLength: 16,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NormalizedCouponType",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "NormalizedTypeMarker",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "ClassificationReliability",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "ClassificationSource",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "ClassificationEvidence",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "ClassificationLoadedAt",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "IsIndexedNominal",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "IsAmortizing",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "NominalCurrency",
            table: "OfzIssues");
    }
}
