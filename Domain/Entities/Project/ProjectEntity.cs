using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public class ProjectEntity : AuditableEntity<Guid>
    {
        [Required(
            ErrorMessageResourceName = ErrorsMessages.FieldIsRequred,
            ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(
            80,
            ErrorMessageResourceName = ErrorsMessages.MaxLength,
            ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; } = string.Empty;

        [MaxLength(
            80,
            ErrorMessageResourceName = ErrorsMessages.MaxLength,
            ErrorMessageResourceType = typeof(ResLocalize))]
        public string? Code { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(2);

        public DateTime TenderDeadline { get; set; } = DateTime.UtcNow;

        public DateTime TenderQA { get; set; } = DateTime.UtcNow;

        // ترتيب العرض العام
        public double SortOrder { get; set; }

        private ProjectData? _data;
        public ProjectData Metadata
        {
            get => _data ??= new ProjectData();
            set => _data = value;
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
        public int? UserId { get; set; }
        public UserEntity? User { get; set; }

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
