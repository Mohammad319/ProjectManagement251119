using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectImportHub.Migrations
{
    /// <inheritdoc />
    public partial class DB251030 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Resources");

            migrationBuilder.AddColumn<string>(
                name: "UpperNote",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpperNote",
                table: "Tasks");

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Resources",
                type: "int",
                nullable: true);
        }
    }
}
