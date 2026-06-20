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

        /// <summary>Dry-run of a project import: runs name-matching against the receiving tenant without writing anything,
        /// so the UI can show what matched, what imports with deviations, and the target before the user confirms.</summary>
        Task<AtacostImportPreviewDTO> PreviewProjectPackageAsync(byte[] fileBytes, Guid targetFolderId, int userId, CancellationToken ct = default);

        /// <summary>Dry-run of a calculation import into a target project. Same contract as the project preview.</summary>
        Task<AtacostImportPreviewDTO> PreviewCalculationPackageAsync(byte[] fileBytes, Guid targetProjectId, int userId, CancellationToken ct = default);

        /// <summary>Imports a project package (.atacost byte[]) into the target folder and creates a new project. Returns the new project id or Guid.Empty.</summary>
        Task<Guid> ImportProjectPackageAsync(
            byte[] fileBytes,
            Guid targetFolderId,
            int userId,
            int? departmentId,
            bool allowCrossDepartment,
            CancellationToken ct = default);

        /// <summary>Imports a calculation package (.atacost byte[]) into the target project and creates a new calculation. Returns the new calculation id or 0.</summary>
        Task<int> ImportCalculationPackageAsync(
            byte[] fileBytes,
            Guid targetProjectId,
            int userId,
            int? departmentId,
            bool allowCrossDepartment,
            CancellationToken ct = default);
    }
}
