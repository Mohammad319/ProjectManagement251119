using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public class ProjectEntity : IDataKeyFilterReadOnly
    {
        public Guid Id { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Code { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(2);


        //[MaxLength(25000, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public DateTime TenderDeadline { get; set; } = DateTime.Now;
        public DateTime TenderQA { get; set; } = DateTime.Now;
        public double Order { get; set; }
        ProjectData data = new();
        public ProjectData Data { get { data ??= new ProjectData(); return data; } set { data = value; } }

        [JsonIgnore] public int TenantId { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? LastModified { get; set; }
        public int? TypeId { get; set; }
        public TypeEntity Type { get; set; }
        public Guid FolderId { get; set; }
        public FolderEntity Folder { get; set; }
        public int? OrganisationId { get; set; }
        public OrganisationEntity Organisation { get; set; }
        public int? UserId { get; set; }
        public UserEntity User { get; set; }
        public int? ProcurementMethodsId { get; set; }
        public ProcurementMethodsEntity ProcurementMethods { get; set; }
        public int? CompensationId { get; set; }
        public CompensationEntity Compensation { get; set; }
        public int? ContractId { get; set; }
        public ContractEntity Contract { get; set; }
        public bool IsVisible { get; set; } = true;
        public ICollection<CalculationEntity> Calculations { get; set; }
    }
}
