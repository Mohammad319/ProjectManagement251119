using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculationApprovalStatusSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowsProductionCalculation",
                table: "Statuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovalStatus",
                table: "Statuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LocksCalculation",
                table: "Statuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                table: "Calculations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByName",
                table: "Calculations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "Calculations",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowsProductionCalculation",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "IsApprovalStatus",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "LocksCalculation",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "ApprovedByName",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "Calculations");
        }
    }
}
