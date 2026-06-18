using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPermissions.Migrations
{
    /// <inheritdoc />
    public partial class DB260617_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tenants_MaxCalculations_Positive",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MaxCalculations",
                table: "Tenants");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "MaxCalculations",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tenants_MaxCalculations_Positive",
                table: "Tenants",
                sql: "[MaxCalculations] >= 1");
        }
    }
}
