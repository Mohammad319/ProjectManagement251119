using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DB260201_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Tasks",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true,
                oldComputedColumnSql: "CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Unit'))), '') AS nvarchar(30))");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Tasks",
                type: "decimal(18,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldNullable: true,
                oldComputedColumnSql: "TRY_CONVERT(decimal(18,3), JSON_VALUE([Metadata], '$.Quantity'))");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Resources",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true,
                oldComputedColumnSql: "CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Unit'))), '') AS nvarchar(30))");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Resources",
                type: "decimal(18,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldNullable: true,
                oldComputedColumnSql: "TRY_CONVERT(decimal(18,3), JSON_VALUE([Metadata], '$.Quantity'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Tasks",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                computedColumnSql: "CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Unit'))), '') AS nvarchar(30))",
                stored: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Tasks",
                type: "decimal(18,3)",
                nullable: true,
                computedColumnSql: "TRY_CONVERT(decimal(18,3), JSON_VALUE([Metadata], '$.Quantity'))",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Resources",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                computedColumnSql: "CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Unit'))), '') AS nvarchar(30))",
                stored: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Resources",
                type: "decimal(18,3)",
                nullable: true,
                computedColumnSql: "TRY_CONVERT(decimal(18,3), JSON_VALUE([Metadata], '$.Quantity'))",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldNullable: true);
        }
    }
}
