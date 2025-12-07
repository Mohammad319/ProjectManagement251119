using TaskResourceBlueprints.Services;
using Microsoft.Extensions.DependencyInjection;
using TaskResourceBlueprints.Dto.ProjectTask;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Services;
using TaskResourceBlueprints.Services.ProjectTask;
using TaskResourceBlueprints.Services.QuestionConditions;
using TaskResourceBlueprints.Services.Resource;
using TaskResourceBlueprints.Services.ResourceProperties;
using TaskResourceBlueprints.Services.TaskGroups;
using TaskResourceBlueprints.Services.UnitGroups;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
using TaskResourceBlueprints.Services.Common;

namespace TaskResourceBlueprints
{
    public static class ServiceRegistration
    {
        public static void AddTaskResourceBlueprints(this IServiceCollection services)
        {
            //services.AddDbContext<TaskResourceBlueprintsContext>(options => options.UseSqlServer(TaskResourceBlueprintsDb));

            services.AddScoped(typeof(IEntityService<>), typeof(EntityService<>));

            services.AddScoped<ITaskDefinitionQueryService, ProjectTaskQueryService>();
            services.AddScoped<ITaskDefinitionService, ProjectTaskService>();
            services.AddScoped<ITaskResourceService, TaskResourceService>();

            services.AddScoped<ITasksUserComputationServiceWasm, TasksUserComputationServiceWasm>();

            services.AddScoped<ITaskGroupsQueryService,TaskGroupsQueryService>();
            services.AddScoped<ITaskConditionsService,TaskConditionsService>();
            services.AddScoped<ITaskUnitGroupService, TaskUnitGroupService>();
            services.AddScoped<IResourceService, ResourceService>();
            services.AddScoped<IResourceAttributeService, ResourceAttributeService>();
            services.AddScoped<IResourceAttributeSetService, ResourceAttributeSetService>();
            services.AddScoped<IResourceCategoryService, ResourceCategoryService>();
        }
    }
}
