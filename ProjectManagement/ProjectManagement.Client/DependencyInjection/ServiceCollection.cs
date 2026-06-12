using ProjectManagement.Client.Services;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Calculation.CalculationItems;
using ProjectManagement.Client.Services.Folder;

namespace ProjectManagement.Client.DependencyInjection
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<FolderService>();
            services.AddScoped<FolderState>();
            services.AddScoped<CalculationInteractionState>();
            services.AddScoped<CalculationService>();
            services.AddScoped<CalculationFilterPresetStorage>();
            services.AddScoped<CalculationListViewPreference>();
            services.AddScoped<ProjectListViewPreference>();
            services.AddScoped<ListSavedFilterStorage>();
            services.AddScoped<ICalculationTableCoordinator, CalculationTableCoordinator>();
            services.AddScoped<ResourceService>();
            services.AddScoped<TaskService>();
            services.AddScoped<MhdServices>();
            services.AddScoped<IContextMenuBuilderService, ContextMenuBuilderService>();
            services.AddScoped<IUnitOfWorkService, UnitOfWorkService>();

            return services;
        }
    }
}
