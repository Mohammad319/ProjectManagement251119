using ProjectManagement.Client.Shared.Repositories.Account;
using ProjectManagement.Client.Shared.Repositories.App;
using ProjectManagement.Client.Shared.Repositories.Application;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.Repositories.Folder;
using ProjectManagement.Client.Shared.Repositories.Identity;
using ProjectManagement.Client.Shared.Repositories.Offer;
using ProjectManagement.Client.Shared.Repositories.Organisation;
using ProjectManagement.Client.Shared.Repositories.Project;
using ProjectManagement.Client.Shared.Repositories.ResourceType;

namespace ProjectManagement.Client.Shared.Repositories
{
    public interface IUnitOfWorkRepository
    {
        IAccountGroupsRepository AccountGroup { get; }
        IItemCalcCategoryRepository ItemCalcCategory { get; }
        IApplicationRepo Application { get; }
        ICalculationRepository Calculation { get; }
        IOpportunityRepository Opportunity { get; }
        IResourceRepository Resource { get; }
        IResourceTypeRepository ResType { get; }
        IShareCalcRepository ShareCalc { get; }
        IStorageRepository Storage { get; }
        ITaskRepository Task { get; }
        ITemplateRepository Template { get; }
        ITenderRepository Tender { get; }
        IFolderRepository Folder { get; }
        IDepartmentsRepository Departments { get; }
        IOfferRepository Offer { get; }
        IOrganisationRepository Org { get; }
        IProjectRepository Project { get; }
    }

    public class UnitOfWorkRepository(
        IAccountGroupsRepository accountgroupsrepository,
        IItemCalcCategoryRepository itemcalccategoryrepository,
        IApplicationRepo applicationrepo,
        ICalculationRepository calculationrepository,
        IOpportunityRepository opportunityrepository,
        IResourceRepository resourcerepository,
        IResourceTypeRepository resourcetyperepository,
        IShareCalcRepository sharecalcrepository,
        IStorageRepository storagerepository,
        ITaskRepository taskrepository,
        ITemplateRepository templaterepository,
        ITenderRepository tenderrepository,
        IFolderRepository folderrepository,
        IDepartmentsRepository departmentsrepository,
        IOfferRepository offerrepository,
        IOrganisationRepository organisationrepository,
        IProjectRepository projectrepository
        ) : IUnitOfWorkRepository
    {
        public IAccountGroupsRepository AccountGroup { get; } = accountgroupsrepository;
        public IItemCalcCategoryRepository ItemCalcCategory { get; } = itemcalccategoryrepository;
        public IApplicationRepo Application { get; } = applicationrepo;
        public ICalculationRepository Calculation { get; } = calculationrepository;
        public IOpportunityRepository Opportunity { get; } = opportunityrepository;
        public IResourceRepository Resource { get; } = resourcerepository;
        public IResourceTypeRepository ResType { get; } = resourcetyperepository;
        public IShareCalcRepository ShareCalc { get; } = sharecalcrepository;
        public IStorageRepository Storage { get; } = storagerepository;
        public ITaskRepository Task { get; } = taskrepository;
        public ITemplateRepository Template { get; } = templaterepository;
        public ITenderRepository Tender { get; } = tenderrepository;
        public IFolderRepository Folder { get; } = folderrepository;
        public IDepartmentsRepository Departments { get; } = departmentsrepository;
        public IOfferRepository Offer { get; } = offerrepository;
        public IOrganisationRepository Org { get; } = organisationrepository;
        public IProjectRepository Project { get; } = projectrepository;
    }

}
