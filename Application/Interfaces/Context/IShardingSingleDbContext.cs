using Domain.Entities.Application;
using Domain.Entities.Calculation;
using Domain.Entities.Organisation;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using Domain.Entities.ResourceType;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.Interfaces.Context
{
    public interface IShardingSingleDbContext
    {
        public  DatabaseFacade Database { get; }
        public ChangeTracker ChangeTracker { get; }
        //void SetTenant(int tenantId);
       // DbSet<OfferResourceEntity> OfferResource { get; set; }
        DbSet<ProjectEntity> Project { get; set; }
        DbSet<OpportunityEntity> Opportunity { get; set; }
        DbSet<ShareCalcEntity> ShareCalc { get; set; }
        DbSet<FolderEntity> Folder { get; set; }
        DbSet<StatusEntity> CalculationStatus { get; set; }
        DbSet<TypeEntity> CalcProjectType { get; set; }
        DbSet<TemplateEntity> Template { get; set; }
        DbSet<CalculationEntity> Calculation { get; set; }
        DbSet<TaskEntity> Tasks { get; set; }
        DbSet<ResourceEntity> Resource { get; set; }
        DbSet<ResourceTypeEntity> ResourceType { get; set; }
        DbSet<ResourceSortEntity> ResourceSort { get; set; }
        DbSet<DepartmentEntity> Department { get; set; }
        DbSet<OrganisationTypeEntity> OrganisationType { get; set; }
        DbSet<StorageEntity> Storage { get; set; }
        DbSet<TaskStatusEntity> TaskStatus { get; set; }
        DbSet<StatusResourcesEntity> ResourceStatus { get; set; }
        DbSet<ProcurementMethodsEntity> ProcurementMethod { get; set; }
        DbSet<CompensationEntity> Compensation { get; set; }
        DbSet<ContractEntity> Contract { get; set; }
        DbSet<TenderEntity> Tender { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
        DbSet<ApplicationEntity> Application { get; set; }
        DbSet<ApplicationValuesEntity> ApplicationValues { get; set; }
        DbSet<OfferEntity> Offer { get; set; }
        DbSet<OrganisationCategoryEntity> OrganisationCategory { get; set; }

        DbSet<OrganisationEntity> Organisation { get; set; }
        DbSet<AccountGroupEntity> AccountGroup { get; set; }
        DbSet<AccountEntity> Account { get; set; }
        public DbSet<UserEntity> User { get; set; }
        DbSet<AttributeNameTenderEntity> AttributeNameTender { get; set; }
        public DbSet<TenderAttributeBindEntity> TenderAttributeBind { get; set; }
    }
}
