using System;
using System.Collections.Generic;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Shared.DTO.Transfer
{
    /// <summary>
    /// Result of a dry-run import (no data written). Shows the user, before committing, what will
    /// be matched automatically, what imports with deviations, and whether the import may proceed.
    /// The deviation/mapping detail reuses the calculation import-info shape.
    /// </summary>
    public sealed class AtacostImportPreviewDTO
    {
        public bool IsValid { get; set; }

        /// <summary>True when nothing blocks the import. Always true in the current model
        /// (all dropdowns optional); reserved for a future required-field/manual-mapping phase.</summary>
        public bool CanImport { get; set; } = true;

        public string Kind { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Read-only "Department / Folder" (or "… / Project") target summary.</summary>
        public string TargetSummary { get; set; } = string.Empty;

        public string? Message { get; set; }
        public string? SenderCompany { get; set; }
        public int CalculationCount { get; set; }

        /// <summary>Import-info that would be stored on the project (summary, mapped main values,
        /// grouped deviations, not-imported groups).</summary>
        public CalculationImportInfoDTO Info { get; set; } = new();
    }

    /// <summary>
    /// External project/calculation copy (ATACOST package). This is a standalone copy, not live sharing.
    /// The package is serialized to JSON and zipped as .atacost. Import always creates a new
    /// project/calculation with fresh internal IDs and no link back to the original.
    /// </summary>
    public sealed class AtacostPackageDTO
    {
        /// <summary>Package format version (for future backward compatibility).</summary>
        public int FormatVersion { get; set; } = 1;

        /// <summary>"project" or "calculation".</summary>
        public string Kind { get; set; } = string.Empty;

        public DateTime ExportedAtUtc { get; set; }

        /// <summary>Sender company (tenant name) when available - informational only.</summary>
        public string? SenderCompany { get; set; }

        /// <summary>Message from the sender that travels with the copy (metadata).</summary>
        public string? Message { get; set; }

        /// <summary>Display name (project or calculation name) so the import dialog can show it without parsing the whole package.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Set when Kind = "project".</summary>
        public AtacostProjectPayload? Project { get; set; }

        /// <summary>Set when Kind = "calculation".</summary>
        public AtacostCalculationPayload? Calculation { get; set; }

        public const string KindProject = "project";
        public const string KindCalculation = "calculation";
    }

    public sealed class AtacostProjectPayload
    {
        public PostProjectDTO Project { get; set; } = new();
        public List<AtacostCalculationPayload> Calculations { get; set; } = [];
    }

    public sealed class AtacostCalculationPayload
    {
        public CalculationPostDTO Calculation { get; set; } = new();

        /// <summary>Top-level rows; child rows and resources are nested inside each TaskPostDTO.</summary>
        public List<TaskPostDTO> Tasks { get; set; } = [];
    }

    /// <summary>Request to export a project copy.</summary>
    public sealed class AtacostProjectExportRequest
    {
        /// <summary>Calculation ids to include in the copy (private calculations are ignored in the backend).</summary>
        public List<int> CalculationIds { get; set; } = [];
        public string? Message { get; set; }
    }

    /// <summary>Request to export a calculation copy.</summary>
    public sealed class AtacostCalculationExportRequest
    {
        public string? Message { get; set; }
    }

    /// <summary>
    /// Lightweight metadata about a package - used by the import dialog to show the contents
    /// (name, calculation count, message) before running the import. Fetched via "inspect".
    /// </summary>
    public sealed class AtacostPackageInfoDTO
    {
        public bool IsValid { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int CalculationCount { get; set; }
        public string? Message { get; set; }
        public string? SenderCompany { get; set; }
        public DateTime ExportedAtUtc { get; set; }
    }
}
