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

        public Procurement Procurement { get; set; }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Supervisor { get; set; } = string.Empty;

        public string OverviewInfoProject { get; set; } = string.Empty;

        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsContactPersonTender { get; set; } = string.Empty;

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Inspector { get; set; } = string.Empty;

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
                Procurement = Procurement,
                Supervisor = MetadataCloneHelper.CopyText(Supervisor),
                OverviewInfoProject = MetadataCloneHelper.CopyText(OverviewInfoProject),
                ClientsContactPersonTender = MetadataCloneHelper.CopyText(ClientsContactPersonTender),
                Inspector = MetadataCloneHelper.CopyText(Inspector)
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

        public Procurement Procurement
        {
            get => Data.Procurement;
            set => Data.Procurement = value;
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
    }

    public class PostProjectDTO : ProjectBaseData
    {
        public bool IsVisible { get; set; } = true;

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public Guid FolderId { get; set; }

        public int? OrganisationId { get; set; }
        public int? ProcurementMethodsId { get; set; }
        public int? CompensationId { get; set; }
        public int? ContractId { get; set; }
        public int? TypeId { get; set; }
    }

    public class ProjectDetailsDTO : ProjectBaseData
    {
        public bool IsVisible { get; set; } = true;
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
        public string Color { get; set; } = string.Empty;
        public string Responsible { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(2);
        public DateTime TenderDeadline { get; set; } = DateTime.Now;
        public DateTime TenderQA { get; set; } = DateTime.Now;
        public bool IsVisible { get; set; } = true;
    }
}
