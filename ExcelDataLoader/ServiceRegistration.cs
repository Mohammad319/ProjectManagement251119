using ExcelDataLoader.Services;
using Microsoft.Extensions.DependencyInjection;
using ProjectImportHub.Dto.ProjectTask;
using ProjectImportHub.Infrastructure;
using ProjectImportHub.Services;
using ProjectImportHub.Services.ProjectTask;
using ProjectImportHub.Services.QuestionConditions;
using ProjectImportHub.Services.Resource;
using ProjectImportHub.Services.ResourceProperties;
using ProjectImportHub.Services.TaskGroups;
using ProjectImportHub.Services.UnitGroups;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;

namespace ExcelDataLoader
{
    public static class ServiceRegistration
    {
        public static void AddExcelDataLoader(this IServiceCollection services)
        {
            //services.AddDbContext<ExcelDataLoaderContext>(options => options.UseSqlServer(ExcelDataLoaderDb));

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
