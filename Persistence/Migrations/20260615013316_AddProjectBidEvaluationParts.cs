using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectBidEvaluationParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BidEvaluationModel",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAwarded",
                table: "ProjectBids",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Placement",
                table: "ProjectBids",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ProjectBids",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ProjectBids",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PartType",
                table: "ProjectBidPriceColumns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Bakåtkompatibilitet: befintliga "Vinnare" mappas till Tilldelad med placering 1.
            migrationBuilder.Sql(
                "UPDATE [ProjectBids] SET [IsAwarded] = 1, [Placement] = 1 WHERE [IsWinner] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BidEvaluationModel",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsAwarded",
                table: "ProjectBids");

            migrationBuilder.DropColumn(
                name: "Placement",
                table: "ProjectBids");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ProjectBids");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProjectBids");

            migrationBuilder.DropColumn(
                name: "PartType",
                table: "ProjectBidPriceColumns");
        }
    }
}
