using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculationRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalculationRole",
                table: "Calculations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CustomCalculationRoleName",
                table: "Calculations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE [Calculations]
                SET [CalculationRole] =
                    CASE [BidRole]
                        WHEN 1 THEN 2
                        WHEN 2 THEN 3
                        WHEN 3 THEN 4
                        ELSE 0
                    END
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Calculations_CalculationRole",
                table: "Calculations",
                sql: "[CalculationRole] IN (0, 1, 2, 3, 4, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Calculations_CalculationRole",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "CalculationRole",
                table: "Calculations");

            migrationBuilder.DropColumn(
                name: "CustomCalculationRoleName",
                table: "Calculations");
        }
    }
}
