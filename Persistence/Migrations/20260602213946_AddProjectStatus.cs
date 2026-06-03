using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectStatusId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountsAsSubmittedBid = table.Column<bool>(type: "bit", nullable: false),
                    CountsAsWonBid = table.Column<bool>(type: "bit", nullable: false),
                    CountsAsLostBid = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStatuses", x => x.Id);
                    table.CheckConstraint("CK_ProjectStatuses_BidResult_NotWonAndLost", "[CountsAsWonBid] = 0 OR [CountsAsLostBid] = 0");
                    table.CheckConstraint("CK_ProjectStatuses_BidResult_RequiresSubmitted", "[CountsAsSubmittedBid] = 1 OR ([CountsAsWonBid] = 0 AND [CountsAsLostBid] = 0)");
                    table.CheckConstraint("CK_ProjectStatuses_Color_Hex", "[Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'");
                    table.CheckConstraint("CK_ProjectStatuses_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_ProjectStatuses_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ProjectStatuses_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectStatuses_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [ProjectStatuses] ([TenantId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Name], [Color], [SortOrder], [IsVisible], [CountsAsSubmittedBid], [CountsAsWonBid], [CountsAsLostBid])
                SELECT tenant.[TenantId], GETUTCDATE(), NULL, NULL, NULL, seed.[Name], seed.[Color], seed.[SortOrder], CAST(1 AS bit), seed.[CountsAsSubmittedBid], seed.[CountsAsWonBid], seed.[CountsAsLostBid]
                FROM (SELECT DISTINCT [TenantId] FROM [Projects]) AS tenant
                CROSS APPLY (VALUES
                    (N'Förfrågan',                N'#6B7280', 100, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
                    (N'Pågående',                 N'#1D4ED8', 200, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
                    (N'Inlämnat / väntar beslut', N'#EAB308', 300, CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
                    (N'Vunnet',                   N'#166534', 400, CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit)),
                    (N'Förlorat',                 N'#DC2626', 500, CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit)),
                    (N'Ej inlämnat',              N'#F97316', 600, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
                    (N'Ej intressant',            N'#FEF08A', 700, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit)),
                    (N'Avbrutet',                 N'#7C2D12', 800, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit))
                ) AS seed([Name], [Color], [SortOrder], [CountsAsSubmittedBid], [CountsAsWonBid], [CountsAsLostBid])
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [ProjectStatuses] existing
                    WHERE existing.[TenantId] = tenant.[TenantId]
                      AND existing.[Name] = seed.[Name]
                );

                UPDATE p
                SET [ProjectStatusId] = ps.[Id]
                FROM [Projects] p
                INNER JOIN [Statuses] s
                    ON s.[TenantId] = p.[TenantId]
                   AND s.[Id] = TRY_CONVERT(int, JSON_VALUE(p.[Metadata], '$.StatusId'))
                INNER JOIN [ProjectStatuses] ps
                    ON ps.[TenantId] = p.[TenantId]
                   AND ps.[Name] = CASE
                        WHEN s.[Name] IN (N'Tilldelad / vunnen', N'Vunnen') THEN N'Vunnet'
                        WHEN s.[Name] IN (N'Förlorad') THEN N'Förlorat'
                        WHEN s.[Name] IN (N'Skickad / inlämnad') THEN N'Inlämnat / väntar beslut'
                        WHEN s.[Name] IN (N'Ej intressant / ej lämnat') THEN N'Ej intressant'
                        ELSE s.[Name]
                   END
                WHERE p.[ProjectStatusId] IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectStatusId",
                table: "Projects",
                column: "ProjectStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatuses_CreatedBy",
                table: "ProjectStatuses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatuses_Tenant_Name",
                table: "ProjectStatuses",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatuses_Tenant_Visible_Order",
                table: "ProjectStatuses",
                columns: new[] { "TenantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatuses_UpdatedBy",
                table: "ProjectStatuses",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectStatuses_ProjectStatusId",
                table: "Projects",
                column: "ProjectStatusId",
                principalTable: "ProjectStatuses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectStatuses_ProjectStatusId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "ProjectStatuses");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectStatusId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectStatusId",
                table: "Projects");
        }
    }
}
