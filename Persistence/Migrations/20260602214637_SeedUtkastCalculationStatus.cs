using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedUtkastCalculationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO [Statuses] ([TenantId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Name], [Color], [SortOrder], [IsVisible], [AllowsProductionCalculation], [CountsAsLostBid], [CountsAsSubmittedBid], [CountsAsWonBid], [IsApprovalStatus], [LocksCalculation])
                SELECT tenant.[TenantId], GETUTCDATE(), NULL, NULL, NULL, N'Utkast', N'#64748B', 50, CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)
                FROM (
                    SELECT DISTINCT [TenantId] FROM [Statuses]
                    UNION
                    SELECT DISTINCT [TenantId] FROM [Calculations]
                ) AS tenant
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Statuses] existing
                    WHERE existing.[TenantId] = tenant.[TenantId]
                      AND existing.[Name] = N'Utkast'
                );

                UPDATE oldStatus
                SET [IsVisible] = 0
                FROM [Statuses] oldStatus
                WHERE oldStatus.[Name] = N'Förfrågan'
                  AND EXISTS (
                      SELECT 1
                      FROM [Statuses] draftStatus
                      WHERE draftStatus.[TenantId] = oldStatus.[TenantId]
                        AND draftStatus.[Name] = N'Utkast'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Statuses]
                SET [IsVisible] = 1
                WHERE [Name] = N'Förfrågan';

                DELETE draftStatus
                FROM [Statuses] draftStatus
                WHERE draftStatus.[Name] = N'Utkast'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [Calculations] calculation
                      WHERE calculation.[StatusId] = draftStatus.[Id]
                  );
                """);
        }
    }
}
