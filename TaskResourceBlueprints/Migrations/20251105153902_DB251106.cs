using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class DB251106 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SortId",
                table: "ResourceTenant",
                newName: "ResourceTypeId");

            migrationBuilder.RenameColumn(
                name: "ResId",
                table: "ResourceTenant",
                newName: "ResourceSortId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResourceTypeId",
                table: "ResourceTenant",
                newName: "SortId");

            migrationBuilder.RenameColumn(
                name: "ResourceSortId",
                table: "ResourceTenant",
                newName: "ResId");
        }
    }
}
