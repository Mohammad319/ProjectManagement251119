using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Base.Project;
using System;
using System.ComponentModel.DataAnnotations;
using ProjectManagement.Shared.DTO.App;
using System.Collections.Generic;
using ProjectManagement.Shared.Base.Organisation;

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
    }
    public class ProjectBaseData : ProjectBase
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
    public class SearchProjectDTO: ListProjectDTO
    {
        public Guid FolderId { get; set; }
    }
    public class ListProjectDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Order { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Responsible { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(2);
        public DateTime TenderDeadline { get; set; } = DateTime.Now;
        public DateTime TenderQA { get; set; } = DateTime.Now;

    }
}
