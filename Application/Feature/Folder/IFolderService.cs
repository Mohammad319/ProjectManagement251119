using ProjectManagement.Shared.DTO.Folder;

namespace Application.Feature.Project.Folder
{
    public interface IFolderService
    {
        // Commands
        Task<Guid> CreateAsync(PostFolderDTO dto, int userId, int departmentId, CancellationToken ct = default);
        Task<bool> UpdateAsync(Guid id, PostFolderDTO dto, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, int userId, int? departmentId, CancellationToken ct = default);
        Task<bool> UpdateOrderAsync(Guid id, double newOrder, CancellationToken ct = default);

        // Queries
        Task<List<ListFolderDTO>> GetAllVisibleAsync(CancellationToken ct = default);
        Task<List<ListFolderDTO>> GetByDepartmentAsync(bool isVisible, int? departmentId, CancellationToken ct = default);
        Task<List<ListFolderDTO>> GetFromOtherDepartmentAsync(int departmentId, CancellationToken ct = default);
        Task<DetailsFolderDTO?> GetDetailsAsync(Guid id, CancellationToken ct = default);
    }
}
