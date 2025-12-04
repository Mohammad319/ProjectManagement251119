using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Project.Implement
{
    public class ProjectRepository(HTTPRepository _httpRepository) : IProjectRepository
    {
        static string ProjectsURLBase => "api/v1/projects/";

        public async Task<List<ListProjectMVVM>> GetByFolderIdAsync(Guid folderId, bool IsVisible = true) =>
            (await _httpRepository.GetAsync<List<ListProjectMVVM>>(ProjectsURLBase + URLConst.Project.GetByFolderDepartmentId + $"/{folderId}?isVisible={IsVisible}")).OrderByDescending(x => x.Order).ToList();
        public async Task<List<ListProjectMVVM>> GetOtherDepartmentAsync(Guid folderId) =>
            (await _httpRepository.GetAsync<List<ListProjectMVVM>>(ProjectsURLBase + URLConst.Project.GetProjectsOtherDepartment + $"/{folderId}")).OrderByDescending(x => x.Order).ToList();
        public async Task<GetProjectCalcConfigDTO> GetConfig(int? m, int? con, int? com, int? t)
        {
            if (!m.HasValue) m = 0;
            if (!con.HasValue) con = 0;
            if (!com.HasValue) com = 0;
            if (!t.HasValue) t = 0;
            return await _httpRepository.GetAsync<GetProjectCalcConfigDTO>(ProjectsURLBase + $"config/{m}/{con}/{com}/{t}");
        }
        public async Task<List<SearchProjectsMVVM>> FilterAsync(ProjectFilter model)
        {
            return await _httpRepository.PostAsync<List<SearchProjectsMVVM>, ProjectFilter>(model, ProjectsURLBase + URLConst.Project.Search);
        }
        public async Task<ProjectDetailsDTO> DetailsAsync(Guid id) =>
            await _httpRepository.GetAsync<ProjectDetailsDTO>(ProjectsURLBase + URLConst.Details + "/" + id);

        public async Task<PostProjectDTO> GetToPostAsync(Guid projectId)
        {
            return await _httpRepository.GetAsync<PostProjectDTO>(ProjectsURLBase + URLConst.Project.GetProjectPost + "/" + projectId);
        }
        public async Task<bool> ReOrderAsync(Guid Id, double newOrder)
        {
            return await _httpRepository.GetAsync<bool>(ProjectsURLBase + URLConst.ReOrder + $"/{Id}/{newOrder}");
        }
       public async Task<Guid> CreateAsync(PostProjectDTO model)
        {
            return await _httpRepository.PostAsync<Guid, PostProjectDTO>(model, ProjectsURLBase);
        }
        public async Task<bool> UpdateAsync(Guid id, PostProjectDTO model)
        {
            return await _httpRepository.PutAsync(model, ProjectsURLBase + id);
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _httpRepository.DeleteAsync(ProjectsURLBase + id.ToString());
        }
    }
}
