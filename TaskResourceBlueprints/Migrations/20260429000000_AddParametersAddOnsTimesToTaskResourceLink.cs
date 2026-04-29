using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class AddParametersAddOnsTimesToTaskResourceLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Parameters",
                table: "TaskDefinitionResourceLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "AddOns",
                table: "TaskDefinitionResourceLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Times",
                table: "TaskDefinitionResourceLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Parameters",
                table: "TaskDefinitionResourceLinks");

            migrationBuilder.DropColumn(
                name: "AddOns",
                table: "TaskDefinitionResourceLinks");

            migrationBuilder.DropColumn(
                name: "Times",
                table: "TaskDefinitionResourceLinks");
        }
    }
}
