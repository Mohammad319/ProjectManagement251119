using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation.Implement
{
    public class StorageRepository(HTTPRepository _httpRepository) : IStorageRepository
    {
        static string StorageURLBase => PMAPIConst.Storage;
        public async Task<bool> SaveAsync(int id, CalculationItemType type, AuthorityStorage level, StorageSort sort)
        {
            return await _httpRepository.GetAsync<bool>(StorageURLBase + URLConst.Storages.Save + $"/{id}/{type}/{level}/{sort}");
        }
        public async Task<bool> RemoveAsync(int id)
        {
            return await _httpRepository.GetAsync<bool>(StorageURLBase + URLConst.Storages.Remove + $"/{id}");
        }

        public async Task<List<T>> GetAsync<T>(GetFilterDTO filter)
        {
            return await _httpRepository.PostAsync<List<T>, GetFilterDTO>(filter, StorageURLBase);
        }

        public async Task<bool> CreateItem(PostStorygeDTO post)
        {
            return await _httpRepository.PostAsync<bool, PostStorygeDTO>(post, StorageURLBase + "cre");
        }

        public async Task<List<StorageDTO<T>>> GetStorageAsync<T>(GetFilterDTO filter)
        {
            return await _httpRepository.PostAsync<List<StorageDTO<T>>, GetFilterDTO>(filter, StorageURLBase);
        }

        public async Task<List<ConditionDto>> GetConditionsAsync()
        {
            return await _httpRepository.GetAsync<List<ConditionDto>>(StorageURLBase + $"condition");
        }

        public async Task<List<TaskWithResourcesMDto>> GetTasksAppAsync()
        {
            return await _httpRepository.GetAsync<List<TaskWithResourcesMDto>>(StorageURLBase + $"tasksapp");
        }

        public async Task<List<ResourceEXDto>> GetResourcesAppAsync()
        {
            return await _httpRepository.GetAsync<List<ResourceEXDto>>(StorageURLBase + $"resapp");
        }

        public async Task<List<ProjectTaskDto>> GetTasksForUserDtoAsync(ProjectTaskFilterDto f)
        {
            return await _httpRepository.PostAsync<List<ProjectTaskDto>, ProjectTaskFilterDto>(f, StorageURLBase + "tasksapp2");
        }

        public async Task<ProjectTaskDto> GetTaskForUserDtoAsync(int id)
        {
            return await _httpRepository.GetAsync<ProjectTaskDto>(StorageURLBase + "tasksapp2/" + id);
        }

        public async Task<bool> UpdateResourceAppStorageTenantAsync(int ResourceId, ResourceTenantLinkBase taskResourceDto)
        {
            return await _httpRepository.PostAsync<bool, ResourceTenantLinkBase>(taskResourceDto, StorageURLBase + "updaterestenant/" + ResourceId);
        }
    }
}
