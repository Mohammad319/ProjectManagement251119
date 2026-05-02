using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class DB_RemoveTaskUnitGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_TaskUnitGroups_TaskUnitGroupId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "TaskUnitGroups");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_TaskUnitGroupId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TaskUnitGroupId",
                table: "Tasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskUnitGroupId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskUnitGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Keys = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskUnitGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TaskUnitGroupId",
                table: "Tasks",
                column: "TaskUnitGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_TaskUnitGroups_TaskUnitGroupId",
                table: "Tasks",
                column: "TaskUnitGroupId",
                principalTable: "TaskUnitGroups",
                principalColumn: "Id");
        }
    }
}
