using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPermissions.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantMlAutoTrainingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoTrainingEnabled",
                table: "TenantMlSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AutoTrainingIntervalDays",
                table: "TenantMlSettings",
                type: "int",
                nullable: false,
                defaultValue: 14);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastScheduledTrainingAtUtc",
                table: "TenantMlSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextTrainingAtUtc",
                table: "TenantMlSettings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoTrainingEnabled",
                table: "TenantMlSettings");

            migrationBuilder.DropColumn(
                name: "AutoTrainingIntervalDays",
                table: "TenantMlSettings");

            migrationBuilder.DropColumn(
                name: "LastScheduledTrainingAtUtc",
                table: "TenantMlSettings");

            migrationBuilder.DropColumn(
                name: "NextTrainingAtUtc",
                table: "TenantMlSettings");
        }
    }
}
