using Domain.Entities.Application;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Context;

public partial class ShardingSingleDbContext
{
    #region DbSets

    public DbSet<TenderEntity> Tenders { get; set; } = default!;
    public DbSet<ProjectEntity> Projects { get; set; } = default!;
    public DbSet<FolderEntity> Folders { get; set; } = default!;
    public DbSet<StatusEntity> CalculationStatus { get; set; } = default!;
    public DbSet<CalculationEntity> Calculations { get; set; } = default!;
    public DbSet<TaskEntity> Tasks { get; set; } = default!;
    public DbSet<ResourceEntity> Resources { get; set; } = default!;
    public DbSet<ResourceTypeEntity> ResourceTypes { get; set; } = default!;
    public DbSet<OrganisationTypeEntity> OrganisationType { get; set; } = default!;
    public DbSet<StorageEntity> Storages { get; set; } = default!;
    public DbSet<TemplateEntity> Templates { get; set; } = default!;
    public DbSet<ResourceSortEntity> ResourceSorts { get; set; } = default!;
    public DbSet<ApplicationEntity> Applications { get; set; } = default!;
    public DbSet<ApplicationValuesEntity> ApplicationValues { get; set; } = default!;
    public DbSet<TypeEntity> CalcProjectType { get; set; } = default!;
    public DbSet<TaskStatusEntity> TaskStatus { get; set; } = default!;
    public DbSet<ProcurementMethodEntity> ProcurementMethod { get; set; } = default!;
    public DbSet<CompensationEntity> Compensations { get; set; } = default!;
    public DbSet<ContractEntity> Contracts { get; set; } = default!;
    public DbSet<OfferEntity> Offers { get; set; } = default!;
    public DbSet<OrganisationCategoryEntity> OrganisationCategory { get; set; } = default!;
    public DbSet<OrganisationEntity> Organisation { get; set; } = default!;
    public DbSet<AccountGroupEntity> AccountGroup { get; set; } = default!;
    public DbSet<AccountEntity> Accounts { get; set; } = default!;
    public DbSet<ShareCalcEntity> ShareCalc { get; set; } = default!;
    public DbSet<OpportunityEntity> Opportunity { get; set; } = default!;
    public DbSet<DepartmentEntity> Department { get; set; } = default!;
    public DbSet<StatusResourcesEntity> ResourceStatus { get; set; } = default!;
    public DbSet<UserEntity> User { get; set; } = default!;
    public DbSet<TenderAttributeDefinitionEntity> AttributeNameTender { get; set; } = default!;
    public DbSet<TenderAttributeBindEntity> TenderAttributeBind { get; set; } = default!;

    #endregion
}
