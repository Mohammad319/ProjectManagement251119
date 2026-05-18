using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
using TaskResourceBlueprints.Services.Common;
using TaskResourceBlueprints.Services.Demo;
using TaskResourceBlueprints.Services.Evaluation;
using TaskResourceBlueprints.Services.Feedback;
using TaskResourceBlueprints.Services.Import;
using TaskResourceBlueprints.Services.ProjectTask;
using TaskResourceBlueprints.Services.Resource;
using TaskResourceBlueprints.Services.StateGroups;
using TaskResourceBlueprints.Services.Training;
namespace TaskResourceBlueprints
{
    public static class ServiceRegistration
    {
        public static void AddTaskResourceBlueprints(this IServiceCollection services)
        {
            services.AddScoped(typeof(IEntityService<>), typeof(EntityService<>));

            services.AddScoped<ITaskResourceCsvImportService, TaskResourceCsvImportService>();
            services.AddScoped<ITaskNameCorpusImportService, TaskNameCorpusImportService>();
            services.AddScoped<ICleanTaskResourceDatasetImportService, CleanTaskResourceDatasetImportService>();
            services.AddScoped<ITaskResourceTrainingDatasetService, TaskResourceTrainingDatasetService>();
            services.AddScoped<ITaskResourceMlTrainingService, TaskResourceMlTrainingService>();
            services.AddScoped<ITaskResourceSuggestionEvaluationService, TaskResourceSuggestionEvaluationService>();
            services.AddScoped<ITaskResourceSuggestionFeedbackReviewService, TaskResourceSuggestionFeedbackReviewService>();
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
