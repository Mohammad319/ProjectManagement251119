using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories.Calculation
{
    public interface IStorageRepository
    {
        Task<bool> UpdateResourceAppStorageTenantAsync(int ResourceId, ResourceTenantLinkBase taskResourceDto);

        Task<List<ProjectTaskDto>> GetTasksForUserDtoAsync(ProjectTaskFilterDto f);
        Task<ProjectTaskDto> GetTaskForUserDtoAsync(int id);
        Task IncrementTaskUsageAsync(int id);
        Task<List<ConditionDto>> GetConditionsAsync();
        Task<List<TaskWithResourcesMDto>> GetTasksAppAsync();
        Task<List<ResourceEXDto>> GetResourcesAppAsync();

        // Task<List<StorageModel>> GetAsync(CalculationItemType type, List<AuthorityStorage> level, List<StorageSort> sort);
        Task<List<StorageDTO<T>>> GetStorageAsync<T>(GetFilterDTO filter);
        Task<List<T>> GetAsync<T>(GetFilterDTO filter);
        Task<bool> CreateItem(PostStorygeDTO post);
        Task<bool> SaveAsync(int id, CalculationItemType type, AuthorityStorage level, StorageSort sort);
        Task<bool> RemoveAsync(int id);
        Task<Dictionary<string, double>> GetTfIdfAsync();
    }
}
