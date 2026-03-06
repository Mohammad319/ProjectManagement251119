using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Shared.DTO.Folder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Folder
{
    public interface IFolderRepository
    {
        Task<List<FolderMVVM>> GetByDepartmentAsync(int DepartmentId);
        Task<List<FolderMVVM>> GetAllVisibleAsync();
        Task<List<FolderMVVM>> GetByVisible(bool IsVisible);
        Task<DetailsFolderDTO> DetailsAsync(Guid id);
        Task<bool> ReOrderAsync(Guid Id, int newOrder);
        Task<Guid> CreateAsync(PostFolderDTO model);
        Task<bool> UpdateAsync(Guid Id, PostFolderDTO model);
        Task<bool> DeleteAsync(Guid id);
    }
}
