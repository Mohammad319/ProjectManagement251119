using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Seeding;
using System.Text.Json;

#nullable disable

namespace Persistence.Migrations
{
    /// <summary>
    /// Completes EnsureStandardColumnTemplates/FixStandardAppearanceTemplatePalettes for tenants
    /// that have NO template rows at all (e.g. demo tenants that never ran the admin company-seed):
    /// those earlier fixes derived the tenant list from the template tables themselves and therefore
    /// skipped such tenants entirely. This migration derives tenants from core tables that every
    /// active tenant has rows in (Departments/Users, plus the template tables), then inserts any
    /// missing system standard templates:
    ///  - appearance: "Standard ljus" (light palette) and "Standard mörk" (dark palette),
    ///  - columns: the 8 standard column templates from TenantSeedCatalog.StandardColumnTemplates.
    /// Idempotent by name incl. legacy equivalents ("Mall01"/"Mall02", "Mall C0x"), so re-running or
    /// running after the admin seed never duplicates anything; user copies and department templates
    /// are never touched.
    /// </summary>
    public partial class EnsureStandardTemplatesForExistingTenants : Migration
    {
        private const string TenantSource = @"(SELECT DISTINCT TenantId FROM Departments
      UNION SELECT DISTINCT TenantId FROM Users
      UNION SELECT DISTINCT TenantId FROM Templates
      UNION SELECT DISTINCT TenantId FROM TemplateColumns)";

        private static readonly Dictionary<string, string[]> LegacyColumnEquivalents = new()
        {
            ["Kompakt nettokalkyl"] = ["Mall C01"],
            ["Ekonomi"] = ["Mall C02", "Kalkyl ekonomi"],
            ["Anbud"] = ["Mall C03"],
            ["Produktion"] = ["Mall C04"],
            ["CO2 / miljö"] = ["Mall C05", "Resurs & CO2"],
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Appearance templates (Standard ljus/mörk) ─────────────────────────────
            foreach (var (name, legacyName, dark) in new[]
                     {
                         ("Standard ljus", "Mall01", false),
                         ("Standard mörk", "Mall02", true),
                     })
            {
                // Same stored shape as TemplateEntity.Metadata (TemplateMetadataData with defaults
                // for MathRound/Currency/DateFormat); only the palettes differ between ljus/mörk.
                var appearance = TenantSeedCatalog.BuildAppearance(dark);
                var metadataJson = JsonSerializer.Serialize(new TemplateMetadataData
                {
                    NetCalc = new NetCalcStyleData { Color = appearance.NetCalc.Color.Clone() },
                    SummarySheet = appearance.SummarySheet.Clone(),
                });

                migrationBuilder.Sql($@"
INSERT INTO Templates (Name, IsVisible, DepartmentId, IsDefault, Metadata, TenantId, CreatedAt)
SELECT N'{Sql(name)}', 1, NULL, 0, N'{Sql(metadataJson)}', t.TenantId, SYSUTCDATETIME()
FROM {TenantSource} t
WHERE NOT EXISTS (
    SELECT 1 FROM Templates e
    WHERE e.TenantId = t.TenantId
      AND e.DepartmentId IS NULL
      AND e.Name IN (N'{Sql(name)}', N'{Sql(legacyName)}'));
");
            }

            // ── Column templates (the 8 standard ones) ────────────────────────────────
            foreach (var seed in TenantSeedCatalog.StandardColumnTemplates)
            {
                var columnsJson = JsonSerializer.Serialize(TenantSeedCatalog.BuildColumns(seed.Columns));
                var names = LegacyColumnEquivalents.TryGetValue(seed.Name, out var legacy)
                    ? new[] { seed.Name }.Concat(legacy)
                    : [seed.Name];
                var nameList = string.Join(", ", names.Select(n => $"N'{Sql(n)}'"));

                migrationBuilder.Sql($@"
INSERT INTO TemplateColumns (Name, IsVisible, DepartmentId, IsDefault, Columns, TenantId, CreatedAt)
SELECT N'{Sql(seed.Name)}', 1, NULL, 0, N'{Sql(columnsJson)}', t.TenantId, SYSUTCDATETIME()
FROM {TenantSource} t
WHERE NOT EXISTS (
    SELECT 1 FROM TemplateColumns e
    WHERE e.TenantId = t.TenantId
      AND e.DepartmentId IS NULL
      AND e.Name IN ({nameList}));
");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data fix only — the previous state (missing standard templates) was the bug.
        }

        private static string Sql(string value) => value.Replace("'", "''");
    }
}
