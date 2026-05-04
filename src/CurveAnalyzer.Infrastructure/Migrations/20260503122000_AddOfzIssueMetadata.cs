using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CurveAnalyzer.Infrastructure.Migrations;

[DbContextAttribute(typeof(MoexContext))]
[Migration("20260503122000_AddOfzIssueMetadata")]
public partial class AddOfzIssueMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "CouponValue",
            table: "OfzIssues",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CouponPeriod",
            table: "OfzIssues",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "InitialFaceValue",
            table: "OfzIssues",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IssueName",
            table: "OfzIssues",
            type: "TEXT",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "IssueSize",
            table: "OfzIssues",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "IssueSizePlaced",
            table: "OfzIssues",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ListLevel",
            table: "OfzIssues",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "MetadataLoadedAt",
            table: "OfzIssues",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "NextCouponDate",
            table: "OfzIssues",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CouponValue",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "CouponPeriod",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "InitialFaceValue",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "IssueName",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "IssueSize",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "IssueSizePlaced",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "ListLevel",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "MetadataLoadedAt",
            table: "OfzIssues");

        migrationBuilder.DropColumn(
            name: "NextCouponDate",
            table: "OfzIssues");
    }
}
