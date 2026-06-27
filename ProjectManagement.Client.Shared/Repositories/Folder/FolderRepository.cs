using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.Repositories.Folder;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Folder;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Folder
{
    public class FolderRepository(HTTPRepository _httpRepository) : IFolderRepository
    {
        static string FolderURLBase => PMAPIConst.Folders;
        public async Task<List<FolderMVVM>> GetByDepartmentAsync(int DepartmentId, bool includeArchived = false)
            => (await _httpRepository.GetAsync<List<ListFolderDTO>>(FolderURLBase + URLConst.Folder.GetFoldersByDepartmentId + $"/{DepartmentId}?includeArchived={includeArchived}"))
                .Select(x => x.ToFolderMVVM())
                .OrderByDescending(x => x.Order)
                .ToList();
        public async Task<List<FolderMVVM>> GetByVisible(bool includeArchived)
            => (await _httpRepository.GetAsync<List<ListFolderDTO>>(FolderURLBase + URLConst.GetList + $"?includeArchived={includeArchived}"))
                .Select(x => x.ToFolderMVVM())
                .OrderByDescending(x => x.Order)
                .ToList();
        public async Task<List<DepartmentAccessDTO>> GetAccessibleDepartmentsAsync()
            => await _httpRepository.GetAsync<List<DepartmentAccessDTO>>(FolderURLBase + URLConst.Folder.AccessibleDepartments)
               ?? [];
        public async Task<List<FolderMVVM>> GetAccessibleFoldersAsync(bool includeArchived = false)
            => (await _httpRepository.GetAsync<List<ListFolderDTO>>(FolderURLBase + URLConst.Folder.AccessibleFolders + $"?includeArchived={includeArchived}"))
                .Select(x => x.ToFolderMVVM())
                .ToList();
        public async Task<DetailsFolderDTO> DetailsAsync(Guid id)
        {
            return await _httpRepository.GetAsync<DetailsFolderDTO>(FolderURLBase + "details/" + id);
        }
        public async Task<bool> ReOrderAsync(Guid Id, int newOrder)
        {
            return await _httpRepository.GetAsync<bool>(FolderURLBase + URLConst.ReOrder + $"/{Id}/{newOrder}");
        }
        public async Task<Guid> CreateAsync(PostFolderDTO model)
        {
            return await _httpRepository.PostAsync<Guid, PostFolderDTO>(model, FolderURLBase);
        }
        public async Task<Guid> CreateAsync(PostFolderDTO model, int departmentId)
        {
            return await _httpRepository.PostAsync<Guid, PostFolderDTO>(model, FolderURLBase + URLConst.Folder.CreateForDepartment + "/" + departmentId);
        }
        public async Task<bool> UpdateAsync(Guid Id, PostFolderDTO model)
        {
            return await _httpRepository.PutAsync(model, FolderURLBase + Id);
        }
        public async Task<bool> MoveAsync(Guid id, int departmentId)
        {
            return await _httpRepository.PutAsync(new { }, FolderURLBase + URLConst.Folder.Move + $"/{id}/{departmentId}");
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _httpRepository.DeleteAsync(FolderURLBase + id);
        }

        public async Task<List<FolderMVVM>> GetAllVisibleAsync()
        {
            return (await _httpRepository.GetAsync<List<ListFolderDTO>>(FolderURLBase + "getall"))
                .Select(x => x.ToFolderMVVM())
                .ToList();
        }
    }
}
