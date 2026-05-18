using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskResourceSuggestionFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskResourceSuggestionFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TargetTaskId = table.Column<int>(type: "int", nullable: false),
                    TargetTaskName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetTaskCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TargetTaskUnit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TargetTaskQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    SourceTaskId = table.Column<int>(type: "int", nullable: false),
                    SourceTaskName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SourceTaskUnit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SourceTaskQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    Feedback = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskResourceSuggestionFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskResourceSuggestionFeedbacks_Source_SourceTaskId_Feedback",
                table: "TaskResourceSuggestionFeedbacks",
                columns: new[] { "Source", "SourceTaskId", "Feedback" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskResourceSuggestionFeedbacks_TenantId_TargetTaskId_Source_SourceTaskId",
                table: "TaskResourceSuggestionFeedbacks",
                columns: new[] { "TenantId", "TargetTaskId", "Source", "SourceTaskId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskResourceSuggestionFeedbacks");
        }
    }
}
