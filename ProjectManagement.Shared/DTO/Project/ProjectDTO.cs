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
        [Range(0, 5, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int Priority { get; set; } = 3;

        public List<AddressDTO> Address { get; set; } = [];
        public List<string> Notes { get; set; } = [];
        public List<string> Responsibles { get; set; } = [];

        public List<UnderContactOrganisationBase> Contacts { get; set; } = [];
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Developer { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsManager { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ProjectManager { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Designer { get; set; }
        public Procurement Procurement { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Supervisor { get; set; }
        public string OverviewInfoProject { get; set; }
        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsContactPersonTender { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Inspector { get; set; }

    }
    public class ProjectBaseData : ProjectBase
    {
        public List<AddressDTO> Address { get; set; } = [];
        public List<string> Notes { get; set; } = [];
        public List<string> Responsibles { get; set; } = [];

        public List<UnderContactOrganisationBase> Contacts { get; set; } = [];
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Developer { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsManager { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ProjectManager { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Designer { get; set; }
        public Procurement Procurement { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Supervisor { get; set; }
        public string OverviewInfoProject { get; set; }
        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsContactPersonTender { get; set; }
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Inspector { get; set; }
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
        public string Folder { get; set; }

        public string Status { get; set; }
        public string Organisation { get; set; }
        public string ProcurementMethods { get; set; }
        public string Compensation { get; set; }
        public string Contract { get; set; }
        public string Type { get; set; }
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
        public string Name { get; set; }
        public double Order { get; set; }
        public string Code { get; set; }
        public string Status { get; set; }
        public string Color { get; set; }
        public string Responsible { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(2);
        public DateTime TenderDeadline { get; set; } = DateTime.Now;
        public DateTime TenderQA { get; set; } = DateTime.Now;

    }
}
