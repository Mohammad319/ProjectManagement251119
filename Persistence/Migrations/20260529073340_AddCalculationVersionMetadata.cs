using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculationVersionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedFromCalculationId",
                table: "Calculations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentVersion",
                table: "Calculations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VersionGroupId",
                table: "Calculations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Calculations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_Project_VersionGroup_Current",
                table: "Calculations",
                columns: new[] { "TenantId", "ProjectId", "VersionGroupId", "IsCurrentVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_Project_VersionGroup_Number",
                table: "Calculations",
                columns: new[] { "TenantId", "ProjectId", "VersionGroupId", "VersionNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Calculations_Tenant_Project_VersionGroup_Current",
                table: "Calculations");

            migrationBuilder.DropIndex(
                name: "IX_Calculations_Tenant_Project_VersionGroup_Number",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "CreatedFromCalculationId",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "IsCurrentVersion",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "VersionGroupId",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Calculations");
        }
    }
}
