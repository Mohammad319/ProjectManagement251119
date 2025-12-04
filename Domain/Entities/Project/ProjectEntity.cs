using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.DTO.Project;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public class ProjectEntity : ProjectBase, IDataKeyFilterReadOnly
    {
        public Guid Id { get; set; }
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
