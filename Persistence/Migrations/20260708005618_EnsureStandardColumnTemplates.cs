using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Seeding;
using System.Text.Json;

#nullable disable

namespace Persistence.Migrations
{
    /// <summary>
    /// Data fix: existing tenant databases only have the legacy column template "Mall C01"
    /// (displayed as "Kompakt nettokalkyl") because the standard-column seed only runs from the
    /// admin portal's company-seed action, which older tenants never re-ran. This migration inserts
    /// the missing standard column templates (company-level, visible, not default) for every tenant
    /// already present in the database. Idempotent: a tenant that already has a template with the
    /// standard name — or its legacy "Mall C0x" equivalent — is skipped, so nothing is duplicated
    /// and existing company templates/copies are never touched. Column sets come from
    /// TenantSeedCatalog.StandardColumnTemplates (single source of truth, same as the seeder);
    /// the operation stays idempotent even if that catalog evolves.
    /// </summary>
    public partial class EnsureStandardColumnTemplates : Migration
    {
        // Legacy technical names and their old display names, mapped to the standard template that
        // replaces them (mirrors LegacyColumnNames/LegacyColumnDisplayNames in TemplateIndex.razor).
        private static readonly Dictionary<string, string[]> LegacyEquivalents = new()
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
            foreach (var seed in TenantSeedCatalog.StandardColumnTemplates)
            {
                var columnsJson = JsonSerializer.Serialize(TenantSeedCatalog.BuildColumns(seed.Columns));
                var names = LegacyEquivalents.TryGetValue(seed.Name, out var legacy)
                    ? new[] { seed.Name }.Concat(legacy)
                    : [seed.Name];
                var nameList = string.Join(", ", names.Select(n => $"N'{n.Replace("'", "''")}'"));

                migrationBuilder.Sql($@"
INSERT INTO TemplateColumns (Name, IsVisible, DepartmentId, IsDefault, Columns, TenantId, CreatedAt)
SELECT N'{seed.Name.Replace("'", "''")}', 1, NULL, 0, N'{columnsJson.Replace("'", "''")}', t.TenantId, SYSUTCDATETIME()
FROM (SELECT DISTINCT TenantId FROM Templates
      UNION SELECT DISTINCT TenantId FROM TemplateColumns) t
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
    }
}
