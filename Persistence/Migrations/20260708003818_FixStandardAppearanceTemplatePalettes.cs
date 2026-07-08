using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <summary>
    /// Data fix: the seeded, system-owned appearance templates "Standard mörk" (legacy name "Mall02")
    /// and "Standard ljus" (legacy "Mall01") ended up with the SAME light palette in existing tenant
    /// databases, so the two standards looked identical when copied/edited. This migration rewrites
    /// only the color keys inside the Metadata JSON (JSON_MODIFY keeps columns, currency, sorting,
    /// etc. intact) for company-level rows (DepartmentId IS NULL) with those exact names. User copies
    /// ("Kopia av ..."), renamed templates and department templates are never touched. The palettes
    /// mirror TenantSeedCatalog.BuildAppearance(dark) — keep them in sync.
    /// </summary>
    public partial class FixStandardAppearanceTemplatePalettes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Standard mörk / Mall02 → genuinely dark, hue-differentiated palette.
            migrationBuilder.Sql(@"
UPDATE Templates SET Metadata =
    JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(
    JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(
    JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(
        CAST(Metadata AS nvarchar(max)),
        '$.NetCalc.Color.Header', '#134e4a'),
        '$.NetCalc.Color.Note', '#1f2937'),
        '$.NetCalc.Color.Border', '#4b5563'),
        '$.NetCalc.Color.BorderStyle', 'solid'),
        '$.NetCalc.Color.Text', '#f3f4f6'),
        '$.NetCalc.Color.Task', '#312e81'),
        '$.NetCalc.Color.SubTask', '#1e3a8a'),
        '$.NetCalc.Color.Resource', '#1e293b'),
        '$.NetCalc.Color.TaskCodeName', '#713f12'),
        '$.NetCalc.Color.TaskDetailBaseQuantity', '#164e63'),
        '$.NetCalc.Color.ResourceParameter', '#14532d'),
        '$.NetCalc.Color.ResourceAttachment', '#831843'),
        '$.NetCalc.Color.ResourceTime', '#374151'),
        '$.NetCalc.Color.InactiveText', '#9ca3af'),
        '$.SummarySheet.Color.Header', '#134e4a'),
        '$.SummarySheet.Color.Note', '#1f2937'),
        '$.SummarySheet.Color.Border', '#4b5563'),
        '$.SummarySheet.Color.BorderStyle', 'solid'),
        '$.SummarySheet.Color.Text', '#f3f4f6'),
        '$.SummarySheet.Color.Sum', '#312e81'),
        '$.SummarySheet.Color.Factor', '#312e81')
WHERE DepartmentId IS NULL
  AND Name IN (N'Mall02', N'Standard mörk')
  AND Metadata IS NOT NULL
  AND ISJSON(CAST(Metadata AS nvarchar(max))) = 1;
");

            // Standard ljus / Mall01 → normalize to the seeded light palette in case it drifted.
            migrationBuilder.Sql(@"
UPDATE Templates SET Metadata =
    JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(
    JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(
    JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(
        CAST(Metadata AS nvarchar(max)),
        '$.NetCalc.Color.Header', '#e1f0ad'),
        '$.NetCalc.Color.Note', '#f0ebeb'),
        '$.NetCalc.Color.Border', '#000'),
        '$.NetCalc.Color.BorderStyle', 'dotted'),
        '$.NetCalc.Color.Text', '#000'),
        '$.NetCalc.Color.Task', '#bec1f9'),
        '$.NetCalc.Color.SubTask', '#b1d2f7'),
        '$.NetCalc.Color.Resource', '#e2f1fd'),
        '$.NetCalc.Color.TaskCodeName', '#fde68a'),
        '$.NetCalc.Color.TaskDetailBaseQuantity', '#bae6fd'),
        '$.NetCalc.Color.ResourceParameter', '#d1fae5'),
        '$.NetCalc.Color.ResourceAttachment', '#fce7f3'),
        '$.NetCalc.Color.ResourceTime', '#e2e8f0'),
        '$.NetCalc.Color.InactiveText', '#8a8a8a'),
        '$.SummarySheet.Color.Header', '#e1f0ad'),
        '$.SummarySheet.Color.Note', '#f0ebeb'),
        '$.SummarySheet.Color.Border', '#000'),
        '$.SummarySheet.Color.BorderStyle', 'dotted'),
        '$.SummarySheet.Color.Text', '#000'),
        '$.SummarySheet.Color.Sum', '#c4c7fe'),
        '$.SummarySheet.Color.Factor', '#c4c7fe')
WHERE DepartmentId IS NULL
  AND Name IN (N'Mall01', N'Standard ljus')
  AND Metadata IS NOT NULL
  AND ISJSON(CAST(Metadata AS nvarchar(max))) = 1;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data fix only — nothing to revert (the old state was the bug).
        }
    }
}
