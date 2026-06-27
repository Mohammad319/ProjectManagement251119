using ProjectManagement.Shared.DTO.Folder;
using ProjectManagement.Shared.DTO.General;

namespace Application.Feature.Project.Folder
{
    public interface IFolderService
    {
        // Commands
        Task<Guid> CreateAsync(PostFolderDTO dto, int userId, int departmentId, CancellationToken ct = default);
        Task<bool> UpdateAsync(Guid id, PostFolderDTO dto, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> MoveAsync(Guid id, int targetDepartmentId, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> UpdateOrderAsync(Guid id, int newOrder, int? departmentId, CancellationToken ct = default);

        // Queries
        Task<List<ListFolderDTO>> GetAllVisibleAsync(CancellationToken ct = default);
        Task<List<ListFolderDTO>> GetByDepartmentAsync(bool includeArchived, int? departmentId, CancellationToken ct = default, int userId = 0, bool isViewer = false);
        Task<List<ListFolderDTO>> GetFromOtherDepartmentAsync(int departmentId, bool includeArchived, CancellationToken ct = default);
        Task<DetailsFolderDTO?> GetDetailsAsync(Guid id, int? departmentId, CancellationToken ct = default);

        /// <summary>
        /// Departments the user may pick in the folder workspace dropdown: their normal-access
        /// departments (own department, or all departments for Admin) plus any department where one
        /// or more projects are shared/assigned to the user (tagged <see cref="DepartmentAccessDTO.SharedOnly"/>).
        /// </summary>
        Task<List<DepartmentAccessDTO>> GetAccessibleDepartmentsAsync(int userId, int? departmentId, CancellationToken ct = default);

        /// <summary>True when the user has at least one shared/assigned project in <paramref name="targetDepartmentId"/>.</summary>
        Task<bool> HasSharedProjectsInDepartmentAsync(int targetDepartmentId, int userId, int? callerDepartmentId, CancellationToken ct = default);

        /// <summary>
        /// Folders in a department the user has NO normal access to, returned as read-only visual groups:
        /// only folders that contain at least one project the user may see (own-created or shared), with
        /// the shared-project count. <c>IsSharedGroup = true</c>.
        /// </summary>
        Task<List<ListFolderDTO>> GetSharedDepartmentFoldersAsync(int targetDepartmentId, int userId, int? callerDepartmentId, bool includeArchived, CancellationToken ct = default);

        /// <summary>
        /// "Alla tillgängliga": every folder the user can reach across departments — normal-access folders
        /// (own department, or all for Admin) plus shared-group folders from other departments — each tagged
        /// with its department and <c>IsSharedGroup</c>.
        /// </summary>
        Task<List<ListFolderDTO>> GetAccessibleFoldersAsync(int userId, int? callerDepartmentId, bool isAdmin, bool includeArchived, CancellationToken ct = default);
    }
}
