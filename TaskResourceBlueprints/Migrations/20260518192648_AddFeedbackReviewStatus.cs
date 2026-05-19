using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackReviewStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "TaskResourceSuggestionFeedbacks",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "TaskResourceSuggestionFeedbacks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskResourceSuggestionFeedbacks_TenantId_ReviewStatus_UpdatedAtUtc",
                table: "TaskResourceSuggestionFeedbacks",
                columns: new[] { "TenantId", "ReviewStatus", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskResourceSuggestionFeedbacks_TenantId_ReviewStatus_UpdatedAtUtc",
                table: "TaskResourceSuggestionFeedbacks");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "TaskResourceSuggestionFeedbacks");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "TaskResourceSuggestionFeedbacks");
        }
    }
}
