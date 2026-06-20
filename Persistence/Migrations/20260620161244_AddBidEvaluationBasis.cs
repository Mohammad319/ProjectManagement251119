using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBidEvaluationBasis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BidEvaluationBasis",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backward compatibility: derive the new evaluation basis from the existing method.
            // HighestPoints (1) -> PriceQuality (2); LowestTotalCost (2) -> Cost (1);
            // LowestComparison (0) -> Price (0) by default.
            migrationBuilder.Sql(@"
UPDATE [Projects] SET [BidEvaluationBasis] = CASE [BidEvaluationModel]
    WHEN 1 THEN 2
    WHEN 2 THEN 1
    ELSE 0
END;");

            // Old 'Lägst jämförelsesumma' projects whose bids actually use a mervärdeavdrag
            // are really Pris och kvalitet, per the agreed backward-compatibility rule.
            migrationBuilder.Sql(@"
UPDATE [Projects] SET [BidEvaluationBasis] = 2
WHERE [BidEvaluationModel] = 0
  AND [Id] IN (SELECT [ProjectId] FROM [ProjectBids] WHERE [DeductionPercent] > 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BidEvaluationBasis",
                table: "Projects");
        }
    }
}
