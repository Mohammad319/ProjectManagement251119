using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Model.Tenant;
using ProjectManagement.Client.Shared.Repositories;
using ProjectManagement.Client.Shared.Repositories.App;
using ProjectManagement.Client.Shared.Repositories.Application;
using ProjectManagement.Client.Shared.Repositories.Application.Implement;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation.Implement;
using ProjectManagement.Client.Shared.Repositories.Folder;
using ProjectManagement.Client.Shared.Repositories.Identity;
using ProjectManagement.Client.Shared.Repositories.Offer;
using ProjectManagement.Client.Shared.Repositories.Organisation;
using ProjectManagement.Client.Shared.Repositories.Organisation.Implement;
using ProjectManagement.Client.Shared.Repositories.Project;
using ProjectManagement.Client.Shared.Repositories.Project.Implement;
using ProjectManagement.Client.Shared.Repositories.ResourceType;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
namespace ProjectManagement.Client.DependencyInjection
{
    public static class RepositoriesCollection
    {
        public static IServiceCollection AddProjectRepositories(this IServiceCollection services)
        {
            services.AddScoped<HTTPRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IFolderRepository, FolderRepository>();
            services.AddScoped<ICalculationRepository, CalculationRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IResourceRepository, ResourceRepository>();
            services.AddScoped<IResourceTypeRepository, ResourceTypeRepository>();
            services.AddScoped<IDepartmentsRepository, DepartmentsRepository>();
            services.AddScoped<IStorageRepository, StorageRepository>();
            services.AddScoped<ITemplateRepository, TemplateRepository>();
            services.AddScoped<ITemplateColumnRepository, TemplateColumnRepository>();
            services.AddScoped<IApplicationRepo, ApplicationRepo>();
            services.AddScoped<IOfferRepository, OfferRepository>();
            services.AddScoped<IOrganisationRepository, OrganisationRepository>();
            services.AddScoped<IOpportunityRepository, OpportunityRepository>();
            services.AddScoped<IShareCalcRepository, ShareCalcRepository>();
            services.AddScoped<ITenderRepository, TenderRepository>();
            services.AddScoped<IItemCalcCategoryRepository, ItemCalcCategoryRepository>();
            services.AddScoped<IUnitOfWorkRepository, UnitOfWorkRepository>();
            services.AddScoped<ITasksUserComputationServiceWasm, TasksUserComputationServiceWasm>();

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));


            services.AddScoped<AppState>();
            return services;
        }
    }
}
