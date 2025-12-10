using Domain.Entities.Application;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Interfaces.Context
{
    public interface IShardingSingleDbContext
    {
        //void SetTenant(int tenantId);
        // DbSet<OfferResourceEntity> OfferResource { get; set; }
        DbSet<ProjectEntity> Projects { get; set; }
        DbSet<OpportunityEntity> Opportunity { get; set; }
        DbSet<ShareCalcEntity> ShareCalc { get; set; }
        DbSet<FolderEntity> Folders { get; set; }
        DbSet<StatusEntity> CalculationStatus { get; set; }
        DbSet<TypeEntity> CalcProjectType { get; set; }
        DbSet<TemplateEntity> Templates { get; set; }
        DbSet<CalculationEntity> Calculations { get; set; }
        DbSet<TaskEntity> Tasks { get; set; }
        DbSet<ResourceEntity> Resources { get; set; }
        DbSet<ResourceTypeEntity> ResourceTypes { get; set; }
        DbSet<ResourceSortEntity> ResourceSorts { get; set; }
        DbSet<DepartmentEntity> Department { get; set; }
        DbSet<OrganisationTypeEntity> OrganisationType { get; set; }
        DbSet<StorageEntity> Storages { get; set; }
        DbSet<TaskStatusEntity> TaskStatus { get; set; }
        DbSet<StatusResourcesEntity> ResourceStatus { get; set; }
        DbSet<ProcurementMethodEntity> ProcurementMethod { get; set; }
        DbSet<CompensationEntity> Compensations { get; set; }
        DbSet<ContractEntity> Contracts { get; set; }
        DbSet<TenderEntity> Tenders { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
        DbSet<ApplicationEntity> Applications { get; set; }
        DbSet<ApplicationValuesEntity> ApplicationValues { get; set; }
        DbSet<OfferEntity> Offers { get; set; }
        DbSet<OrganisationCategoryEntity> OrganisationCategory { get; set; }

        DbSet<OrganisationEntity> Organisation { get; set; }
        DbSet<AccountGroupEntity> AccountGroup { get; set; }
        DbSet<AccountEntity> Accounts { get; set; }
        public DbSet<UserEntity> User { get; set; }
        DbSet<TenderAttributeDefinitionEntity> AttributeNameTender { get; set; }
        public DbSet<TenderAttributeBindEntity> TenderAttributeBind { get; set; }
    }
}
