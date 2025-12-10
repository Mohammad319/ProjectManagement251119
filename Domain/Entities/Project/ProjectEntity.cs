using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public class ProjectEntity : AuditableEntity<Guid>
    {
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

        // المستخدم المرتبط (مالك/منشئ، اختياري)
        [ForeignKey(nameof(CreatedBy))]
        [JsonIgnore]
        public UserEntity? CreatedByUser { get; set; }
        [ForeignKey(nameof(UpdatedBy))]
        [JsonIgnore]
        public UserEntity? UpdatedByUser { get; set; }
        public int? ProcurementMethodId { get; set; }
        public ProcurementMethodEntity? ProcurementMethod { get; set; }

        public int? CompensationId { get; set; }
        public CompensationEntity? Compensation { get; set; }

        public int? ContractId { get; set; }
        public ContractEntity? Contract { get; set; }

        public bool IsVisible { get; set; } = true;

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; set; } = [];
    }
}
