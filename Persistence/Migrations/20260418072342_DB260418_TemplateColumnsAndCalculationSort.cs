using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Context;

#nullable disable

namespace Persistence.Migrations
{
    [DbContext(typeof(ShardingSingleDbContext))]
    [Migration("20260418072342_DB260418_TemplateColumnsAndCalculationSort")]
    public partial class DB260418_TemplateColumnsAndCalculationSort : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sort",
                table: "Calculations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateTable(
                name: "TemplateColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Columns = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateColumns_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateColumns_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateColumns_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_CreatedBy",
                table: "TemplateColumns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_Tenant_Template",
                table: "TemplateColumns",
                columns: new[] { "TenantId", "TemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_UpdatedBy",
                table: "TemplateColumns",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_TemplateColumns_TemplateId",
                table: "TemplateColumns",
                column: "TemplateId",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [TemplateColumns] ([TemplateId], [Columns], [TenantId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy])
                SELECT
                    [Id],
                    COALESCE(JSON_QUERY([Metadata], '$.NetCalc.Columns'), N'[]'),
                    [TenantId],
                    [CreatedAt],
                    [CreatedBy],
                    [UpdatedAt],
                    [UpdatedBy]
                FROM [Templates]
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [TemplateColumns]
                    WHERE [TemplateColumns].[TemplateId] = [Templates].[Id]);
                """);

            migrationBuilder.Sql(
                """
                UPDATE c
                SET [Sort] = COALESCE(JSON_QUERY(t.[Metadata], '$.NetCalc.Sort'), N'{}')
                FROM [Calculations] c
                LEFT JOIN [Templates] t ON t.[Id] = c.[TemplateId];
                """);

            migrationBuilder.Sql(
                """
                UPDATE [Templates]
                SET [Metadata] = JSON_MODIFY(
                    JSON_MODIFY([Metadata], '$.NetCalc.Columns', NULL),
                    '$.NetCalc.Sort',
                    NULL)
                WHERE ISJSON([Metadata]) = 1;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE t
                SET [Metadata] = JSON_MODIFY(
                    JSON_MODIFY(t.[Metadata], '$.NetCalc.Columns', JSON_QUERY(tc.[Columns])),
                    '$.NetCalc.Sort',
                    JSON_QUERY(COALESCE(c.[Sort], N'{}')))
                FROM [Templates] t
                LEFT JOIN [TemplateColumns] tc ON tc.[TemplateId] = t.[Id]
                OUTER APPLY (
                    SELECT TOP (1) [Sort]
                    FROM [Calculations]
                    WHERE [TemplateId] = t.[Id]
                    ORDER BY [Id]
                ) c
                WHERE ISJSON(t.[Metadata]) = 1;
                """);

            migrationBuilder.DropTable(
                name: "TemplateColumns");

            migrationBuilder.DropColumn(
                name: "Sort",
                table: "Calculations");
        }
    }
}
