using ProjectManagement.Shared.DTO.Folder;

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
        Task<List<ListFolderDTO>> GetByDepartmentAsync(bool includeArchived, int? departmentId, CancellationToken ct = default);
        Task<List<ListFolderDTO>> GetFromOtherDepartmentAsync(int departmentId, bool includeArchived, CancellationToken ct = default);
        Task<DetailsFolderDTO?> GetDetailsAsync(Guid id, int? departmentId, CancellationToken ct = default);
    }
}
