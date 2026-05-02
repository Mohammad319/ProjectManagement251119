using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
using TaskResourceBlueprints.Services.Common;
using TaskResourceBlueprints.Services.Demo;
using TaskResourceBlueprints.Services.Import;
using TaskResourceBlueprints.Services.ProjectTask;
using TaskResourceBlueprints.Services.Resource;
using TaskResourceBlueprints.Services.StateGroups;
namespace TaskResourceBlueprints
{
    public static class ServiceRegistration
    {
        public static void AddTaskResourceBlueprints(this IServiceCollection services)
        {
            services.AddScoped(typeof(IEntityService<>), typeof(EntityService<>));

            services.AddScoped<ITaskResourceCsvImportService, TaskResourceCsvImportService>();
            services.AddScoped<ITaskDefinitionQueryService, ProjectTaskQueryService>();
            services.AddScoped<ITaskDefinitionService, ProjectTaskService>();
            services.AddScoped<ITaskResourceService, TaskResourceService>();

            services.AddScoped<ITasksUserComputationServiceWasm, TasksUserComputationServiceWasm>();

            services.AddScoped<IResourceBlueprintsService, ResourceBlueprintsService>();
            services.AddScoped<IResourceCategoryService, ResourceCategoryService>();
            services.AddScoped<ITaskStateGroupService, TaskStateGroupService>();
        }
    }
}
