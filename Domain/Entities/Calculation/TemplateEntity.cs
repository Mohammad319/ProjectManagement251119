using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TemplateEntity : AuditableEntity<int>
    {
        /// <summary>
        /// Template name.
        /// </summary>
        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }


        /// <summary>
        /// Extra metadata for the template.
        /// </summary>
        private TemplateData? _metadata;
        public TemplateData Metadata
        {
            get => _metadata ??= new TemplateData();
            set => _metadata = value;
        }

        /// <summary>
        /// Whether this template is visible in UI.
        /// </summary>
        [JsonIgnore]
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Optional department that owns this template.
        /// </summary>
        [JsonIgnore]
        public int? DepartmentId { get; set; }

        [JsonIgnore]
        public DepartmentEntity? Department { get; set; }

        /// <summary>
        /// Calculations created from this template.
        /// </summary>
        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; set; } = [];
    }
}
