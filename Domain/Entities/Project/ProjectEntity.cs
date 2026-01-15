using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{public static class ProjectMappingExtensions
{
    public static ProjectData ToMetadata(this PostProjectDTO dto)
    {
        return new ProjectData
        {
            Procurement = dto.Procurement,
            ProjectManager = dto.ProjectManager,
            Notes = dto.Notes,
            ClientsContactPersonTender = dto.ClientsContactPersonTender,
            Address = dto.Address,
            ClientsManager = dto.ClientsManager,
            Contacts = dto.Contacts,
            Designer = dto.Designer,
            Developer = dto.Developer,
            Inspector = dto.Inspector,
            OverviewInfoProject = dto.OverviewInfoProject,
            Responsibles = dto.Responsibles,
            Supervisor = dto.Supervisor
        };
    }
}

    public sealed class ProjectEntity : AuditableSoftDeletableEntity<Guid>
    {
        [Range(0, 5)]
        public int Priority { get; set; } = 3;
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(FieldLengths.Code)]
        public string? Code { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(2);

        public DateTime TenderDeadline { get; set; } = DateTime.UtcNow;

        public DateTime TenderQA { get; set; } = DateTime.UtcNow;

        // ترتيب العرض العام
        public double SortOrder { get; set; }
        public int DepartmentId { get; private set; }


        private ProjectData? _metadata;
        public ProjectData Metadata
        {
            get => _metadata ??= new ProjectData();
            set => _metadata = value;
        }

        // نوع المشروع (اختياري)
        public int? ProjectTypeId { get; set; }
        public TypeEntity? ProjectType { get; set; }

        // مجلد المشروع (أساسي)
        public Guid FolderId { get; set; }
        public FolderEntity Folder { get; set; } = null!;

        // المنظمة المالكة (اختيارية)
        public int? OrganisationId { get; set; }
        public OrganisationEntity? Organisation { get; set; }

        public int? ProcurementMethodId { get; set; }
        public ProcurementMethodEntity? ProcurementMethod { get; set; }

        public int? CompensationId { get; set; }
        public CompensationEntity? Compensation { get; set; }

        public int? ContractId { get; set; }
        public ContractEntity? Contract { get; set; }

        public bool IsVisible { get; set; } = true;

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; set; } = [];

        private ProjectEntity() { } // EF

        public static ProjectEntity Create(
            PostProjectDTO dto,
            Guid folderId,
            int createdBy,
            double sortOrder)
        {
            var project = new ProjectEntity
            {
                FolderId = folderId,
                CreatedBy = createdBy,
                SortOrder = sortOrder
            };
            project.Update(dto);
            return project;
        }

        public void Update(PostProjectDTO dto)
        {
            Name = dto.Name;
            Code = dto.Code;
            StartDate = dto.StartDate;
            EndDate = dto.EndDate;
            TenderDeadline = dto.TenderDeadline;
            TenderQA = dto.TenderQA;
            IsVisible = dto.IsVisible;

            ProjectTypeId = dto.TypeId;
            OrganisationId = dto.OrganisationId;
            ProcurementMethodId = dto.ProcurementMethodsId;
            CompensationId = dto.CompensationId;
            ContractId = dto.ContractId;

            Metadata = dto.ToMetadata();
        }

        public void UpdateOrder(double newOrder) => SortOrder = newOrder;
    }
}
