using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class DB260520_3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Name",
                table: "Tasks",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_Name",
                table: "Tasks");
        }
    }
}
