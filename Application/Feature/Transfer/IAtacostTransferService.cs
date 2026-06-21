using ProjectManagement.Shared.DTO.Transfer;

namespace Application.Feature.Transfer
{
    /// <summary>
    /// External export/import of project/calculation copies (ATACOST packages).
    /// These are standalone copies, not live sharing between companies.
    /// The backend enforces access and excludes private calculations from the export.
    /// Import always creates a new project/calculation with fresh internal IDs.
    /// </summary>
    public interface IAtacostTransferService
    {
        /// <summary>Builds a project package (.atacost content as byte[]). Returns null when access is denied.</summary>
        Task<byte[]?> BuildProjectPackageAsync(
            Guid projectId,
            AtacostProjectExportRequest request,
            int userId,
            int? departmentId,
            bool isViewer,
            CancellationToken ct = default);

        /// <summary>Builds a calculation package (.atacost content as byte[]). Returns null when access is denied or the calculation is private.</summary>
        Task<byte[]?> BuildCalculationPackageAsync(
            int calculationId,
            AtacostCalculationExportRequest request,
            int userId,
            int? departmentId,
            bool isViewer,
            CancellationToken ct = default);

        /// <summary>Reads metadata from an uploaded .atacost package without importing. Returns IsValid=false when the file is invalid.</summary>
        Task<AtacostPackageInfoDTO> InspectPackageAsync(byte[] fileBytes, CancellationToken ct = default);

        /// <summary>Dry-run of a project import: runs name-matching (plus any manual overrides) against the receiving
        /// tenant without writing anything, so the UI can show what matched, what still needs action, what imports
        /// with deviations, and the target before the user confirms.</summary>
        Task<AtacostImportPreviewDTO> PreviewProjectPackageAsync(byte[] fileBytes, Guid targetFolderId, int userId, IReadOnlyList<AtacostManualMappingDTO>? overrides = null, CancellationToken ct = default);

        /// <summary>Dry-run of a calculation import into a target project. Same contract as the project preview.</summary>
        Task<AtacostImportPreviewDTO> PreviewCalculationPackageAsync(byte[] fileBytes, Guid targetProjectId, int userId, IReadOnlyList<AtacostManualMappingDTO>? overrides = null, CancellationToken ct = default);

        /// <summary>Imports a project package into the target folder and creates a new project. Returns a
        /// structured result with the new project id on success or a precise reason/message on failure.</summary>
        Task<AtacostImportResultDTO> ImportProjectPackageAsync(
            byte[] fileBytes,
            Guid targetFolderId,
            int userId,
            int? departmentId,
            bool allowCrossDepartment,
            IReadOnlyList<AtacostManualMappingDTO>? overrides = null,
            CancellationToken ct = default);

        /// <summary>Imports a calculation package into the target project and creates a new calculation. Returns a
        /// structured result with the new calculation id on success or a precise reason/message on failure.</summary>
        Task<AtacostImportResultDTO> ImportCalculationPackageAsync(
            byte[] fileBytes,
            Guid targetProjectId,
            int userId,
            int? departmentId,
            bool allowCrossDepartment,
            IReadOnlyList<AtacostManualMappingDTO>? overrides = null,
            CancellationToken ct = default);
    }
}
