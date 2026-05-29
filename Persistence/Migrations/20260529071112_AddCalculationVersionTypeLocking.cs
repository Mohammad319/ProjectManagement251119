using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculationVersionTypeLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalculationType",
                table: "Calculations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Calculations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAtUtc",
                table: "Calculations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockedByUserId",
                table: "Calculations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceCalculationId",
                table: "Calculations",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Calculations_CalculationType",
                table: "Calculations",
                sql: "[CalculationType] IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Calculations_CalculationType",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "CalculationType",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "LockedAtUtc",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "LockedByUserId",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "SourceCalculationId",
                table: "Calculations");
        }
    }
}
