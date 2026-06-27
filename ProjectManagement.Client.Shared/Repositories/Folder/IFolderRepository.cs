using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Shared.DTO.Folder;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Folder
{
    public interface IFolderRepository
    {
        Task<List<FolderMVVM>> GetByDepartmentAsync(int DepartmentId, bool includeArchived = false);
        Task<List<FolderMVVM>> GetAllVisibleAsync();
        Task<List<FolderMVVM>> GetByVisible(bool includeArchived);

        /// <summary>Departments the user may pick (normal + shared-only with 👥).</summary>
        Task<List<DepartmentAccessDTO>> GetAccessibleDepartmentsAsync();

        /// <summary>"Alla tillgängliga": all folders the user can reach across departments.</summary>
        Task<List<FolderMVVM>> GetAccessibleFoldersAsync(bool includeArchived = false);
        Task<DetailsFolderDTO> DetailsAsync(Guid id);
        Task<bool> ReOrderAsync(Guid Id, int newOrder);
        Task<Guid> CreateAsync(PostFolderDTO model);
        Task<Guid> CreateAsync(PostFolderDTO model, int departmentId);
        Task<bool> UpdateAsync(Guid Id, PostFolderDTO model);
        Task<bool> MoveAsync(Guid id, int departmentId);
        Task<bool> DeleteAsync(Guid id);
    }
}
