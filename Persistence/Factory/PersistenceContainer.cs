using Application.Feature.Account;
using Application.Feature.Application;
using Application.Feature.Calculation.CalcShare;
using Application.Feature.Calculation.Calculation;
using Application.Feature.Calculation.Resource;
using Application.Feature.Calculation.Task;
using Application.Feature.General;
using Application.Feature.Identity.Department;
using Application.Feature.Offer;
using Application.Feature.Organisation.Organisation;
using Application.Feature.Organisation.OrganisationCategory;
using Application.Feature.Organisation.OrganisationType;
using Application.Feature.PriceImport;
using Application.Feature.TfIdf;
using Application.Feature.Project.Folder;
using Application.Feature.Project.Project;
using Application.Feature.ResourceType;
using Application.Interfaces.Email;
using Application.Services.CalculationItems.Opportunity;
using Application.Services.CalculationItems.Storage;
using Application.Services.CalculationItems.TemplateTable;
using Application.Services.CalculationItems.Tender;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Service;
using Persistence.Service.Application;
using Persistence.Service.CalculationItems.Calculation;
using Persistence.Service.CalculationItems.Opportunity;
using Persistence.Service.CalculationItems.Project;
using Persistence.Service.CalculationItems.Resource;
using Persistence.Service.CalculationItems.ShareCalc;
using Persistence.Service.CalculationItems.Storage;
using Persistence.Service.CalculationItems.Task;
using Persistence.Service.CalculationItems.Template;
using Persistence.Service.CalculationItems.Tender;
using Persistence.Service.Department;
using Persistence.Service.Folder;
using Persistence.Service.Offer;
using Persistence.Service.Organisation;
using Persistence.Service.PriceImport;
using Persistence.Service.TfIdf;
using Persistence.Service.Lookup;
using Persistence.Service.Project;
using Persistence.Service.ResourceAccount;
using Persistence.Service.ResourceType;

namespace Persistence.Factory
{
    public static class PersistenceContainer
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
        {
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IResourceService, ResourceService>();
            services.AddScoped(typeof(ILookupStatusCommandService<>), typeof(LookupStatusCommandService<>));
            services.AddScoped<ICalculationQueryService, CalculationQueryService>();
            services.AddScoped<ICalculationService, CalculationService>();
            services.AddScoped<IOpportunityService, OpportunityService>();
            services.AddScoped<IResourceQueryService, ResourceQueryService>();
            services.AddScoped<IShareCalcService, ShareCalcCommandService>();
            services.AddScoped<IStorageCommandService, StorageCommandService>();
            services.AddScoped<IStorageQueryService, StorageQueryService>();
            services.AddScoped<ITaskQueryService, TaskQueryService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddSingleton<ITaskResourceSuggestionMlRanker, TaskResourceSuggestionMlRanker>();
            services.AddScoped<ITaskResourceSuggestionService, TaskResourceSuggestionService>();
            services.AddScoped<ITemplateCommandService, TemplateCommandService>();
            services.AddScoped<ITemplateQueryService, TemplateQueryService>();
            services.AddScoped<ITemplateColumnCommandService, TemplateColumnCommandService>();
            services.AddScoped<ITemplateColumnQueryService, TemplateColumnQueryService>();
            services.AddScoped<ITenderAttributeCommandService, TenderAttributeCommandService>();
            services.AddScoped<ITenderAttributeQueryService, TenderAttributeQueryService>();
            services.AddScoped<ITenderCommandService, TenderCommandService>();
            services.AddScoped<ITenderQueryService, TenderQueryService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IFolderService, FolderService>();
            services.AddScoped<IOfferService, OfferService>();
            services.AddScoped<IOrganisationCategoryService, OrganisationCategoryService>();
            services.AddScoped<IOrganisationService, OrganisationService>();
            services.AddScoped<IOrganisationTypeService, OrganisationTypeService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IAccountGroupService, AccountGroupService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IResourceTypeService, ResourceTypeService>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IPriceImportService, PriceImportService>();
            services.AddScoped<ITfIdfIndexProvider, TfIdfIndexService>();
            services.AddSingleton<LookupCacheService>();

            // وبما أنك تستخدمه مع:
            services.AddScoped<ILookupStatusCommandService<TaskStatusEntity>, LookupStatusCommandService<TaskStatusEntity>>();
            services.AddScoped<ILookupStatusCommandService<ContractEntity>, LookupStatusCommandService<ContractEntity>>();
            services.AddScoped<ILookupStatusCommandService<TypeEntity>, LookupStatusCommandService<TypeEntity>>();
            services.AddScoped<ILookupStatusCommandService<StatusEntity>, LookupStatusCommandService<StatusEntity>>();
            services.AddScoped<ILookupStatusCommandService<CompensationEntity>, LookupStatusCommandService<CompensationEntity>>();
            services.AddScoped<ILookupStatusCommandService<ProcurementMethodEntity>, LookupStatusCommandService<ProcurementMethodEntity>>();
            services.AddScoped<ILookupStatusCommandService<StatusResourcesEntity>, LookupStatusCommandService<StatusResourcesEntity>>();

            return services;
        }
    }

}
