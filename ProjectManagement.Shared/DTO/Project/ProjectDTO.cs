using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Project
{
    public class ProjectData
    {
        public List<AddressDTO> Address { get; set; } = [];
        public List<string> Notes { get; set; } = [];
        public List<string> Responsibles { get; set; } = [];
        public List<UnderContactOrganisationBase> Contacts { get; set; } = [];

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Developer { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsManager { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ProjectManager { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Designer { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Supervisor { get; set; } = string.Empty;

        public string OverviewInfoProject { get; set; } = string.Empty;

        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsContactPersonTender { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Inspector { get; set; } = string.Empty;

        public int? StatusId { get; set; }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string StatusName { get; set; } = string.Empty;

        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ProcurementName { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ProcurementNumber { get; set; } = string.Empty;

        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string CustomerReference { get; set; } = string.Empty;

        [MaxLength(300, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ProcurementLink { get; set; } = string.Empty;

        public DateTime? PublicationDate { get; set; }
        public DateTime? DecisionDate { get; set; }

        public ProjectData Clone()
        {
            return new ProjectData
            {
                Address = MetadataCloneHelper.CloneAddresses(Address),
                Notes = MetadataCloneHelper.CloneStrings(Notes),
                Responsibles = MetadataCloneHelper.CloneStrings(Responsibles),
                Contacts = MetadataCloneHelper.CloneContacts(Contacts),
                Developer = MetadataCloneHelper.CopyText(Developer),
                ClientsManager = MetadataCloneHelper.CopyText(ClientsManager),
                ProjectManager = MetadataCloneHelper.CopyText(ProjectManager),
                Designer = MetadataCloneHelper.CopyText(Designer),

                Supervisor = MetadataCloneHelper.CopyText(Supervisor),
                OverviewInfoProject = MetadataCloneHelper.CopyText(OverviewInfoProject),
                ClientsContactPersonTender = MetadataCloneHelper.CopyText(ClientsContactPersonTender),
                Inspector = MetadataCloneHelper.CopyText(Inspector),
                StatusId = StatusId,
                StatusName = MetadataCloneHelper.CopyText(StatusName),
                ProcurementName = MetadataCloneHelper.CopyText(ProcurementName),
                ProcurementNumber = MetadataCloneHelper.CopyText(ProcurementNumber),
                CustomerReference = MetadataCloneHelper.CopyText(CustomerReference),
                ProcurementLink = MetadataCloneHelper.CopyText(ProcurementLink),
                PublicationDate = PublicationDate,
                DecisionDate = DecisionDate
            };
        }
    }

    public class ProjectBaseData : ProjectBase
    {
        private ProjectData? data = new();

        [JsonIgnore]
        public ProjectData Data
        {
            get
            {
                data ??= new ProjectData();
                return data;
            }
            set => data = value?.Clone() ?? new ProjectData();
        }

        public List<AddressDTO> Address
        {
            get => Data.Address;
            set => Data.Address = MetadataCloneHelper.CloneAddresses(value);
        }

        public List<string> Notes
        {
            get => Data.Notes;
            set => Data.Notes = MetadataCloneHelper.CloneStrings(value);
        }

        public List<string> Responsibles
        {
            get => Data.Responsibles;
            set => Data.Responsibles = MetadataCloneHelper.CloneStrings(value);
        }

        public List<UnderContactOrganisationBase> Contacts
        {
            get => Data.Contacts;
            set => Data.Contacts = MetadataCloneHelper.CloneContacts(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Developer
        {
            get => Data.Developer;
            set => Data.Developer = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsManager
        {
            get => Data.ClientsManager;
            set => Data.ClientsManager = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ProjectManager
        {
            get => Data.ProjectManager;
            set => Data.ProjectManager = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Designer
        {
            get => Data.Designer;
            set => Data.Designer = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Supervisor
        {
            get => Data.Supervisor;
            set => Data.Supervisor = MetadataCloneHelper.CopyText(value);
        }

        public string OverviewInfoProject
        {
            get => Data.OverviewInfoProject;
            set => Data.OverviewInfoProject = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsContactPersonTender
        {
            get => Data.ClientsContactPersonTender;
            set => Data.ClientsContactPersonTender = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Inspector
        {
            get => Data.Inspector;
            set => Data.Inspector = MetadataCloneHelper.CopyText(value);
        }

        public int? StatusId
        {
            get => Data.StatusId;
            set => Data.StatusId = value;
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string StatusName
        {
            get => Data.StatusName;
            set => Data.StatusName = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ProcurementName
        {
            get => Data.ProcurementName;
            set => Data.ProcurementName = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ProcurementNumber
        {
            get => Data.ProcurementNumber;
            set => Data.ProcurementNumber = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string CustomerReference
        {
            get => Data.CustomerReference;
            set => Data.CustomerReference = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(300, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ProcurementLink
        {
            get => Data.ProcurementLink;
            set => Data.ProcurementLink = MetadataCloneHelper.CopyText(value);
        }

        public DateTime? PublicationDate
        {
            get => Data.PublicationDate;
            set => Data.PublicationDate = value;
        }

        public DateTime? DecisionDate
        {
            get => Data.DecisionDate;
            set => Data.DecisionDate = value;
        }
    }

    public class PostProjectDTO : ProjectBaseData
    {
        public bool IsArchived { get; set; } = false;

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public Guid FolderId { get; set; }

        public int? OrganisationId { get; set; }
        public int? ProcurementMethodsId { get; set; }
        public int? ProcurementProcedureId { get; set; }
        public int? CompensationId { get; set; }
        public int? ContractId { get; set; }
        public int? TypeId { get; set; }
    }

    public class ProjectDetailsDTO : ProjectBaseData
    {
        public bool IsArchived { get; set; } = false;
        public string Folder { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Organisation { get; set; } = string.Empty;
        public string ProcurementMethods { get; set; } = string.Empty;
        public string Compensation { get; set; } = string.Empty;
        public string Contract { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? LastModified { get; set; }
    }

    public class SearchProjectDTO : ListProjectDTO
    {
        public Guid FolderId { get; set; }
    }

    public class ListProjectDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? StatusId { get; set; }
        public int? StatusSortOrder { get; set; }
        public string Color { get; set; } = string.Empty;
        public bool CountsAsSubmittedBid { get; set; }
        public bool CountsAsWonBid { get; set; }
        public bool CountsAsLostBid { get; set; }
        public string Responsible { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(2);
        public DateTime TenderDeadline { get; set; } = DateTime.Now;
        public DateTime TenderQA { get; set; } = DateTime.Now;
        public bool IsArchived { get; set; } = false;
        public int CalculationCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Developer { get; set; } = string.Empty;
        public string Organisation { get; set; } = string.Empty;
        public string ProcurementName { get; set; } = string.Empty;
        public string ProcurementNumber { get; set; } = string.Empty;
        public string CustomerReference { get; set; } = string.Empty;
        public string Contract { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Inspector { get; set; } = string.Empty;
        public string Designer { get; set; } = string.Empty;
        public string Supervisor { get; set; } = string.Empty;
        public string AddressText { get; set; } = string.Empty;
        public string ProcurementMethods { get; set; } = string.Empty;
        public string Compensation { get; set; } = string.Empty;
        public string ProcurementProcedure { get; set; } = string.Empty;
        public string ClientsManager { get; set; } = string.Empty;
        public DateTime? PublicationDate { get; set; }
        public DateTime? DecisionDate { get; set; }
    }
}
