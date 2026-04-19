using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DB260326_5_TemplateColumnTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateColumns_Templates_TemplateId",
                table: "TemplateColumns");

            migrationBuilder.DropIndex(
                name: "IX_TemplateColumns_Tenant_Template",
                table: "TemplateColumns");

            migrationBuilder.DropIndex(
                name: "UX_TemplateColumns_TemplateId",
                table: "TemplateColumns");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "TemplateColumns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "TemplateColumns",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "TemplateColumns",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TemplateColumnId",
                table: "Calculations",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE tc
                SET
                    [Name] = LEFT(CONCAT(COALESCE(NULLIF(LTRIM(RTRIM(t.[Name])), N''), CONCAT(N'Columns ', tc.[Id])), N' columns'), 80),
                    [DepartmentId] = t.[DepartmentId],
                    [IsVisible] = CAST(1 AS bit)
                FROM [TemplateColumns] tc
                LEFT JOIN [Templates] t ON t.[Id] = tc.[TemplateId];
                """);

            migrationBuilder.Sql(
                """
                UPDATE c
                SET [TemplateColumnId] = tc.[Id]
                FROM [Calculations] c
                INNER JOIN [TemplateColumns] tc ON tc.[TemplateId] = c.[TemplateId];
                """);

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "TemplateColumns");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_DepartmentId",
                table: "TemplateColumns",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_Tenant_Department_Id",
                table: "TemplateColumns",
                columns: new[] { "TenantId", "DepartmentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_Tenant_Department_Name",
                table: "TemplateColumns",
                columns: new[] { "TenantId", "DepartmentId", "Name" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TemplateColumns_Name_NotEmpty",
                table: "TemplateColumns",
                sql: "LEN(LTRIM(RTRIM([Name]))) > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_TemplateColumnId",
                table: "Calculations",
                column: "TemplateColumnId");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_TemplateColumns_TemplateColumnId",
                table: "Calculations",
                column: "TemplateColumnId",
                principalTable: "TemplateColumns",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateColumns_Departments_DepartmentId",
                table: "TemplateColumns",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Calculations_TemplateColumns_TemplateColumnId",
                table: "Calculations");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateColumns_Departments_DepartmentId",
                table: "TemplateColumns");

            migrationBuilder.DropIndex(
                name: "IX_TemplateColumns_DepartmentId",
                table: "TemplateColumns");

            migrationBuilder.DropIndex(
                name: "IX_TemplateColumns_Tenant_Department_Id",
                table: "TemplateColumns");

            migrationBuilder.DropIndex(
                name: "IX_TemplateColumns_Tenant_Department_Name",
                table: "TemplateColumns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TemplateColumns_Name_NotEmpty",
                table: "TemplateColumns");

            migrationBuilder.DropIndex(
                name: "IX_Calculations_TemplateColumnId",
                table: "Calculations");

            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                table: "TemplateColumns",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE tc
                SET [TemplateId] = c.[TemplateId]
                FROM [TemplateColumns] tc
                OUTER APPLY (
                    SELECT TOP (1) [TemplateId]
                    FROM [Calculations]
                    WHERE [TemplateColumnId] = tc.[Id]
                      AND [TemplateId] IS NOT NULL
                    ORDER BY [Id]
                ) c
                WHERE c.[TemplateId] IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE tc
                SET [TemplateId] = t.[Id]
                FROM [TemplateColumns] tc
                OUTER APPLY (
                    SELECT TOP (1) [Id]
                    FROM [Templates]
                    WHERE [DepartmentId] = tc.[DepartmentId] OR [DepartmentId] IS NULL
                    ORDER BY CASE WHEN [DepartmentId] = tc.[DepartmentId] THEN 0 ELSE 1 END, [Id]
                ) t
                WHERE tc.[TemplateId] IS NULL
                  AND t.[Id] IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM [TemplateColumns]
                WHERE [TemplateId] IS NULL;

                WITH ranked AS (
                    SELECT [Id],
                           ROW_NUMBER() OVER (PARTITION BY [TemplateId] ORDER BY [Id]) AS [RowNumber]
                    FROM [TemplateColumns]
                )
                DELETE FROM ranked
                WHERE [RowNumber] > 1;
                """);

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "TemplateColumns");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "TemplateColumns");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "TemplateColumns");

            migrationBuilder.DropColumn(
                name: "TemplateColumnId",
                table: "Calculations");

            migrationBuilder.AlterColumn<int>(
                name: "TemplateId",
                table: "TemplateColumns",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_Tenant_Template",
                table: "TemplateColumns",
                columns: new[] { "TenantId", "TemplateId" });

            migrationBuilder.CreateIndex(
                name: "UX_TemplateColumns_TemplateId",
                table: "TemplateColumns",
                column: "TemplateId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateColumns_Templates_TemplateId",
                table: "TemplateColumns",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
