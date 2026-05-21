using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class AddBlueprintSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status_UsageCount_Code",
                table: "Tasks",
                columns: new[] { "Status", "UsageCount", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_IsActive_IsVisible_Unit",
                table: "Resources",
                columns: new[] { "IsActive", "IsVisible", "Unit" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_Status_UsageCount_Code",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Resources_IsActive_IsVisible_Unit",
                table: "Resources");
        }
    }
}
