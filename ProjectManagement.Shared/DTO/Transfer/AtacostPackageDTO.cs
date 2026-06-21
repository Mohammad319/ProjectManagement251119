using System;
using System.Collections.Generic;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.DTO.Transfer
{
    /// <summary>Distinct outcomes of an import attempt, so the UI can show a precise message.</summary>
    public enum AtacostImportStatus
    {
        Success = 0,
        TargetNotFound = 1,       // target project/folder missing or not in the current tenant
        NotAuthorized = 2,        // cross-department without permission, or target locked/archived
        RequiredMappingMissing = 3, // a mandatory value has no local mapping
        SaveFailed = 4,           // unexpected DB error (details are logged, not exposed)
        InvalidFile = 5,          // package could not be read / wrong kind
    }

    /// <summary>
    /// Structured result of an import. Returned with HTTP 200 so the dialog can render a precise
    /// message instead of a generic failure. Technical exception details are logged server-side only.
    /// </summary>
    public sealed class AtacostImportResultDTO
    {
        public AtacostImportStatus Status { get; set; } = AtacostImportStatus.Success;
        public bool Success => Status == AtacostImportStatus.Success;

        /// <summary>New calculation id on a successful calculation import (0 otherwise).</summary>
        public int NewCalculationId { get; set; }
        /// <summary>New project id on a successful project import (null otherwise).</summary>
        public Guid? NewProjectId { get; set; }

        /// <summary>Safe, user-facing Swedish message (no technical details).</summary>
        public string Message { get; set; } = string.Empty;

        public static AtacostImportResultDTO Ok(int calculationId) =>
            new() { Status = AtacostImportStatus.Success, NewCalculationId = calculationId };
        public static AtacostImportResultDTO Ok(Guid projectId) =>
            new() { Status = AtacostImportStatus.Success, NewProjectId = projectId };
        public static AtacostImportResultDTO Fail(AtacostImportStatus status, string message) =>
            new() { Status = status, Message = message };
    }

    /// <summary>A local value the user can pick to resolve an unmatched import slot.</summary>
    public sealed class AtacostLocalOptionDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// One unmatched value the user may resolve in the preview. Slots are keyed by a semantic
    /// <see cref="Type"/> (account, type, calcstatus, …) plus the normalized source value, so the
    /// same source value is mapped consistently across the project and all its calculations.
    /// </summary>
    public sealed class AtacostMappableSlotDTO
    {
        /// <summary>Semantic type key (matches <see cref="AtacostManualMappingDTO.Type"/>).</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>Swedish field label for the UI.</summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>Source value as shown to the user (e.g. "4010 Material").</summary>
        public string OriginalValue { get; set; } = string.Empty;
        /// <summary>Normalized source value used as the matching key.</summary>
        public string OriginalKey { get; set; } = string.Empty;
        /// <summary>Calculation rows affected (0 for header/main fields).</summary>
        public int AffectedRows { get; set; }
        /// <summary>Row-level slots (account/resource refs) also allow "exclude rows".</summary>
        public bool IsRowLevel { get; set; }
        public List<AtacostLocalOptionDTO> Options { get; set; } = [];
        /// <summary>Echo of the user's current decision so it survives a preview refresh.</summary>
        public int? SelectedLocalId { get; set; }
        public string? SelectedAction { get; set; }
    }

    /// <summary>A user's manual decision for one unmatched value during import.</summary>
    public sealed class AtacostManualMappingDTO
    {
        public string Type { get; set; } = string.Empty;
        public string OriginalValue { get; set; } = string.Empty;
        /// <summary>Chosen local id when <see cref="Action"/> = "map".</summary>
        public int? LocalId { get; set; }
        /// <summary>"map" (use LocalId), "exclude" (skip affected rows). Null/empty = import with deviation.</summary>
        public string? Action { get; set; }
    }

    /// <summary>Body for preview/import: the uploaded package plus any manual mapping decisions.</summary>
    public sealed class AtacostImportRequest
    {
        public byte[] FileBytes { get; set; } = [];
        public List<AtacostManualMappingDTO> Overrides { get; set; } = [];
    }

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

        // Calc-only header values read straight from the file (project import leaves these null).
        public string? CalcTypeName { get; set; }
        public string? CalcStatusName { get; set; }
        public CalculationRole? CalcRole { get; set; }
        public string? CalcCustomRoleName { get; set; }

        /// <summary>Unmatched values the user can resolve in the preview (dropdowns + actions).</summary>
        public List<AtacostMappableSlotDTO> MappableSlots { get; set; } = [];

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
