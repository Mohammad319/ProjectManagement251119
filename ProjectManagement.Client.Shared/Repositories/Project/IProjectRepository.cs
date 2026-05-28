using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Project;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Project
{
    public interface IProjectRepository
    {
        Task<List<ListProjectMVVM>> GetByFolderIdAsync(Guid folderId, bool includeArchived = false);
        Task<List<ListProjectMVVM>> GetOtherDepartmentAsync(Guid folderId, bool includeArchived = false);
        Task<List<SearchProjectsMVVM>> FilterAsync(ProjectFilter filter);
        Task<GetProjectCalcConfigDTO> GetConfig(int? m, int? con, int? com, int? t);
        Task<ProjectDetailsDTO> DetailsAsync(Guid projectId);
        Task<PostProjectDTO> GetToPostAsync(Guid projectId);
        Task<bool> ReOrderAsync(Guid Id, int newOrder);
        Task<Guid> CreateAsync(PostProjectDTO create);
        Task<bool> UpdateAsync(Guid id, PostProjectDTO project);
        Task<bool> MoveAsync(Guid id, Guid targetFolderId);
        Task<Guid> CopyAsync(Guid targetFolderId, Guid projectId, bool includeCalculations);
        Task<bool> DeleteAsync(Guid id);
    }
}
