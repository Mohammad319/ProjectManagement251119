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

namespace TaskResourceBlueprints
{
    public static class ServiceRegistration
    {
        public static void AddTaskResourceBlueprints(this IServiceCollection services)
        {
            //services.AddDbContext<TaskResourceBlueprintsContext>(options => options.UseSqlServer(TaskResourceBlueprintsDb));

            services.AddScoped(typeof(IEntityService<>), typeof(EntityService<>));
            services.AddScoped<ITaskResourceService, TaskResourceService>();

            services.AddScoped<IProjectTaskQueryService, ProjectTaskQueryService>();
            services.AddScoped<IProjectTaskService, ProjectTaskService>();
            services.AddScoped<ITaskFoldersService, TaskFoldersService>();

            services.AddScoped<IProjectTaskService,ProjectTaskService>();
            services.AddScoped<ITasksUserComputationServiceWasm, TasksUserComputationServiceWasm>();

            services.AddScoped<ITaskGroupsQueryService,TaskGroupsQueryService>();
            services.AddScoped<ITaskGroupsCommandService, TaskGroupsCommandService>();
            services.AddScoped<ITaskConditionsService,TaskConditionsService>();
            services.AddScoped<IUnitGroupsService, UnitGroupsService>();
            services.AddScoped<IResourceService, ResourceService>();
            services.AddScoped<IResourcePropertiesService, ResourcePropertiesService>();
            services.AddScoped<IGroupPropertiesService, GroupPropertiesService>();
            services.AddScoped<IResourceFolderService, ResourceFolderService>();
        }
    }
}
