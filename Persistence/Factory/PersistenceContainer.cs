using Application.Feature.Calculation.Resource;
using Application.Feature.General;
using Application.Interfaces.Email;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Service;
using Persistence.Service.CalculationItems.Project;

namespace Persistence.Factory
{
    public static class PersistenceContainer
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
        {
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IResourceService, ResourceService>();
            services.AddScoped(typeof(ILookupStatusQueryService<>), typeof(LookupStatusQueryService<>));

            // وبما أنك تستخدمه مع:
            services.AddScoped<ILookupStatusQueryService<TaskStatusEntity>, LookupStatusQueryService<TaskStatusEntity>>();
            services.AddScoped<ILookupStatusQueryService<ContractEntity>, LookupStatusQueryService<ContractEntity>>();
            services.AddScoped<ILookupStatusQueryService<TypeEntity>, LookupStatusQueryService<TypeEntity>>();
            services.AddScoped<ILookupStatusQueryService<StatusEntity>, LookupStatusQueryService<StatusEntity>>();
            services.AddScoped<ILookupStatusQueryService<CompensationEntity>,
                              LookupStatusQueryService<CompensationEntity>>();
            services.AddScoped<ILookupStatusQueryService<ProcurementMethodEntity>, LookupStatusQueryService<ProcurementMethodEntity>>();

            services.AddScoped(provider =>
            {
                var factory = provider.GetRequiredService<IDbContextFactory>();
                return factory.CreateDbContext();
            });

            return services;
        }
    }

}