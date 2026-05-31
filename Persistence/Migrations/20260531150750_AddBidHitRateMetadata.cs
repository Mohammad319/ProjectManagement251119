using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBidHitRateMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Calculations_CalculationType",
                table: "Calculations");

            migrationBuilder.AddColumn<bool>(
                name: "CountsAsLostBid",
                table: "Statuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CountsAsSubmittedBid",
                table: "Statuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CountsAsWonBid",
                table: "Statuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BidRole",
                table: "Calculations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE [Statuses]
                SET [CountsAsSubmittedBid] = 1
                WHERE [Name] IN (N'Skickad / inlämnad', N'Tilldelad / vunnen', N'Förlorad');

                UPDATE [Statuses]
                SET [CountsAsWonBid] = 1
                WHERE [Name] = N'Tilldelad / vunnen';

                UPDATE [Statuses]
                SET [CountsAsLostBid] = 1
                WHERE [Name] = N'Förlorad';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Calculations_BidRole",
                table: "Calculations",
                sql: "[BidRole] IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Calculations_CalculationType",
                table: "Calculations",
                sql: "[CalculationType] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Statuses_BidResult_NotWonAndLost",
                table: "Statuses",
                sql: "[CountsAsWonBid] = 0 OR [CountsAsLostBid] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Statuses_BidResult_RequiresSubmitted",
                table: "Statuses",
                sql: "[CountsAsSubmittedBid] = 1 OR ([CountsAsWonBid] = 0 AND [CountsAsLostBid] = 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Calculations_BidRole",
                table: "Calculations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Calculations_CalculationType",
                table: "Calculations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Statuses_BidResult_NotWonAndLost",
                table: "Statuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Statuses_BidResult_RequiresSubmitted",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "CountsAsLostBid",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "CountsAsSubmittedBid",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "CountsAsWonBid",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "BidRole",
                table: "Calculations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Calculations_CalculationType",
                table: "Calculations",
                sql: "[CalculationType] IN (0, 1)");
        }
    }
}
