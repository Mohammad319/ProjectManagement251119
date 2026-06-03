using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddControlStatusMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "TaskStatuses",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "TaskStatuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemDefault",
                table: "TaskStatuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "StatusResources",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "StatusResources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemDefault",
                table: "StatusResources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            SeedControlStatuses(migrationBuilder, "TaskStatuses");
            SeedControlStatuses(migrationBuilder, "StatusResources");

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatuses_Tenant_Code",
                table: "TaskStatuses",
                columns: new[] { "TenantId", "Code" },
                filter: "[Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_StatusResources_Tenant_Code",
                table: "StatusResources",
                columns: new[] { "TenantId", "Code" },
                filter: "[Code] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskStatuses_Tenant_Code",
                table: "TaskStatuses");

            migrationBuilder.DropIndex(
                name: "IX_StatusResources_Tenant_Code",
                table: "StatusResources");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "TaskStatuses");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "TaskStatuses");

            migrationBuilder.DropColumn(
                name: "IsSystemDefault",
                table: "TaskStatuses");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "StatusResources");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "StatusResources");

            migrationBuilder.DropColumn(
                name: "IsSystemDefault",
                table: "StatusResources");
        }

        private static void SeedControlStatuses(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.Sql($"""
                DECLARE @Seed TABLE (
                    [Code] nvarchar(64) NOT NULL,
                    [Name] nvarchar(80) NOT NULL,
                    [Color] nvarchar(7) NOT NULL,
                    [SortOrder] int NOT NULL,
                    [IsDefault] bit NOT NULL
                );

                INSERT INTO @Seed ([Code], [Name], [Color], [SortOrder], [IsDefault])
                VALUES
                    (N'DRAFT', N'Utkast', N'#64748b', 100, 1),
                    (N'REVIEW_REQUIRED', N'Kontroll krävs', N'#f97316', 200, 0),
                    (N'QUANTITY_REVIEW', N'Mängdkontroll', N'#f97316', 300, 0),
                    (N'COST_REVIEW', N'Kostnadskontroll', N'#f97316', 400, 0),
                    (N'REVIEWED', N'Kontrollerad', N'#16a34a', 500, 0),
                    (N'DEVIATING', N'Avvikande', N'#dc2626', 600, 0);

                UPDATE target
                SET
                    target.[Name] = seed.[Name],
                    target.[Color] = seed.[Color],
                    target.[SortOrder] = seed.[SortOrder],
                    target.[IsVisible] = 1,
                    target.[Code] = seed.[Code],
                    target.[IsDefault] = seed.[IsDefault],
                    target.[IsSystemDefault] = 1
                FROM [{tableName}] target
                INNER JOIN @Seed seed
                    ON target.[Code] = seed.[Code]
                    OR (target.[Code] = N'' AND target.[Name] = seed.[Name]);

                INSERT INTO [{tableName}]
                    ([TenantId], [CreatedAt], [Name], [Color], [SortOrder], [IsVisible], [Code], [IsDefault], [IsSystemDefault])
                SELECT tenants.[TenantId], SYSUTCDATETIME(), seed.[Name], seed.[Color], seed.[SortOrder], 1, seed.[Code], seed.[IsDefault], 1
                FROM (SELECT DISTINCT [TenantId] FROM [{tableName}]) tenants
                CROSS JOIN @Seed seed
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [{tableName}] existing
                    WHERE existing.[TenantId] = tenants.[TenantId]
                      AND existing.[Code] = seed.[Code]
                );
                """);
        }
    }
}
