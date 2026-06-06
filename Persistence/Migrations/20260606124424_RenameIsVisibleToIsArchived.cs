using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameIsVisibleToIsArchived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsVisible",
                table: "Projects",
                newName: "IsArchived");

            // Invert: old IsVisible=1 (active) → new IsArchived=0 (not archived)
            migrationBuilder.Sql("UPDATE [Projects] SET [IsArchived] = CASE WHEN [IsArchived] = 1 THEN 0 ELSE 1 END");

            migrationBuilder.RenameColumn(
                name: "IsVisible",
                table: "Calculations",
                newName: "IsArchived");

            // Invert: old IsVisible=1 (active) → new IsArchived=0 (not archived)
            migrationBuilder.Sql("UPDATE [Calculations] SET [IsArchived] = CASE WHEN [IsArchived] = 1 THEN 0 ELSE 1 END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsArchived",
                table: "Projects",
                newName: "IsVisible");

            // Invert back: IsArchived=0 (not archived) → IsVisible=1 (active)
            migrationBuilder.Sql("UPDATE [Projects] SET [IsVisible] = CASE WHEN [IsVisible] = 1 THEN 0 ELSE 1 END");

            migrationBuilder.RenameColumn(
                name: "IsArchived",
                table: "Calculations",
                newName: "IsVisible");

            // Invert back: IsArchived=0 (not archived) → IsVisible=1 (active)
            migrationBuilder.Sql("UPDATE [Calculations] SET [IsVisible] = CASE WHEN [IsVisible] = 1 THEN 0 ELSE 1 END");
        }
    }
}
