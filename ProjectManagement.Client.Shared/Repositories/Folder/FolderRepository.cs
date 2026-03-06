using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.Repositories.Folder;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Folder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Project.Implement
{
    public class FolderRepository(HTTPRepository _httpRepository) : IFolderRepository
    {
        static string FolderURLBase => "api/v1/folders/";
        public async Task<List<FolderMVVM>> GetByDepartmentAsync(int DepartmentId)
            => (await _httpRepository.GetAsync<List<FolderMVVM>>(FolderURLBase + URLConst.Folder.GetFoldersByDepartmentId + $"/{DepartmentId}")).OrderByDescending(x => x.Order).ToList();
        public async Task<List<FolderMVVM>> GetByVisible(bool IsVisible)
            => (await _httpRepository.GetAsync<List<FolderMVVM>>(FolderURLBase + URLConst.GetList + $"?isVisible={IsVisible}")).OrderByDescending(x => x.Order).ToList();
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
        public async Task<bool> UpdateAsync(Guid Id, PostFolderDTO model)
        {
            return await _httpRepository.PutAsync(model, FolderURLBase + Id);
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _httpRepository.DeleteAsync(FolderURLBase + id);
        }

        public async Task<List<FolderMVVM>> GetAllVisibleAsync()
        {
            return await _httpRepository.GetAsync<List<FolderMVVM>>(FolderURLBase + "getall");
        }
    }
}
