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
            services.AddSingleton<FolderState>();
            services.AddScoped<CalculationService>();
            services.AddScoped<ResourceService>();
            services.AddScoped<TaskService>();
            services.AddScoped<MhdServices>();
            services.AddScoped<IContextMenuBuilderService, ContextMenuBuilderService>();
            services.AddScoped<IUnitOfWorkService, UnitOfWorkService>();

            return services;
        }
    }
}
