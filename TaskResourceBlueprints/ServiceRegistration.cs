using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
using TaskResourceBlueprints.Services.Common;
using TaskResourceBlueprints.Services.Demo;
using TaskResourceBlueprints.Services.Import;
using TaskResourceBlueprints.Services.ProjectTask;
using TaskResourceBlueprints.Services.QuestionConditions;
using TaskResourceBlueprints.Services.Resource;
using TaskResourceBlueprints.Services.ResourceProperties;
using TaskResourceBlueprints.Services.TaskGroups;
using TaskResourceBlueprints.Services.UnitGroups;

namespace TaskResourceBlueprints
{
    public static class ServiceRegistration
    {
        public static void AddTaskResourceBlueprints(this IServiceCollection services)
        {
            services.AddScoped(typeof(IEntityService<>), typeof(EntityService<>));

            services.AddScoped<IConstructionDemoDataService, ConstructionDemoDataService>();
            services.AddScoped<ITaskResourceCsvImportService, TaskResourceCsvImportService>();
            services.AddScoped<ITaskDefinitionQueryService, ProjectTaskQueryService>();
            services.AddScoped<ITaskDefinitionService, ProjectTaskService>();
            services.AddScoped<ITaskResourceService, TaskResourceService>();

            services.AddScoped<ITasksUserComputationServiceWasm, TasksUserComputationServiceWasm>();

            services.AddScoped<ITaskGroupsQueryService, TaskGroupsQueryService>();
            services.AddScoped<ITaskConditionsService, TaskConditionsService>();
            services.AddScoped<ITaskUnitGroupService, TaskUnitGroupService>();
            services.AddScoped<IResourceBlueprintsService, ResourceBlueprintsService>();
            services.AddScoped<IResourceAttributeService, ResourceAttributeService>();
            services.AddScoped<IResourceAttributeSetService, ResourceAttributeSetService>();
            services.AddScoped<IResourceCategoryService, ResourceCategoryService>();
        }
    }
}
