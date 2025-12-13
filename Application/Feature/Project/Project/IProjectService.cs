using ProjectManagement.Shared.DTO.Project;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Feature.Project.Project
{
    public interface IProjectService
    {
        // Commands
        Task<Guid> CreateAsync(PostProjectDTO dto, int userId, int? departmentId, CancellationToken ct);
        Task<bool> UpdateAsync(Guid id, PostProjectDTO dto, int userId, int? departmentId, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, int userId, int? departmentId, CancellationToken ct);
        Task<bool> UpdateOrderAsync(Guid id, double newOrder, CancellationToken ct);

        // Queries
        Task<ProjectDetailsDTO?> GetDetailsAsync(Guid id, CancellationToken ct);
        Task<PostProjectDTO?> GetPostAsync(Guid id, CancellationToken ct);
        Task<IEnumerable<ListProjectDTO>> GetByFolderAsync(Guid folderId, bool isVisible, int userId, int? departmentId, CancellationToken ct);
        Task<IEnumerable<ListProjectDTO>> GetOtherGroupByFolderAsync(Guid folderId, int userId, int? departmentId, CancellationToken ct);
        Task<IEnumerable<SearchProjectDTO>> SearchAsync(ProjectFilter filter, int userId, int? departmentId, CancellationToken ct);
    }

}
