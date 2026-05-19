using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPermissions.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantMlSettingsAndTrainingRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantMlSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UsageMode = table.Column<int>(type: "int", nullable: false),
                    IncludeFeedbackInTraining = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMlSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantMlSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantMlTrainingRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ModelPath = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PositiveExamples = table.Column<int>(type: "int", nullable: false),
                    NegativeExamples = table.Column<int>(type: "int", nullable: false),
                    TotalTasks = table.Column<int>(type: "int", nullable: false),
                    TasksWithResources = table.Column<int>(type: "int", nullable: false),
                    TasksWithoutResources = table.Column<int>(type: "int", nullable: false),
                    TotalResources = table.Column<int>(type: "int", nullable: false),
                    MissingUnitResources = table.Column<int>(type: "int", nullable: false),
                    UnknownTypeResources = table.Column<int>(type: "int", nullable: false),
                    FeedbackExamples = table.Column<int>(type: "int", nullable: false),
                    GlobalPositiveAverage = table.Column<double>(type: "float", nullable: true),
                    GlobalNegativeAverage = table.Column<double>(type: "float", nullable: true),
                    GlobalMargin = table.Column<double>(type: "float", nullable: true),
                    TenantPositiveAverage = table.Column<double>(type: "float", nullable: true),
                    TenantNegativeAverage = table.Column<double>(type: "float", nullable: true),
                    TenantMargin = table.Column<double>(type: "float", nullable: true),
                    BetterModel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResourceTypeBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMlTrainingRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantMlTrainingRuns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_TenantMlSettings_TenantId",
                table: "TenantMlSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMlTrainingRuns_Tenant_Started",
                table: "TenantMlTrainingRuns",
                columns: new[] { "TenantId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantMlSettings");

            migrationBuilder.DropTable(
                name: "TenantMlTrainingRuns");
        }
    }
}
