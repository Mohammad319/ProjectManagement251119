using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class TypeEntity : AuditableEntity<int>
    {
        /// <summary>
        /// ترتيب العرض للجميع (ليس لكل مستخدم).
        /// </summary>
        public int SortOrder { get; set; }

        public bool IsVisible { get; set; } = true;

        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }


        /// <summary>
        /// اللون المميز، HEX format. مثال: #00ff00
        /// </summary>
        [StringLength(
            7,
            ErrorMessageResourceName = ErrorsMessages.MaxLength,
            ErrorMessageResourceType = typeof(ResLocalize),
            MinimumLength = 7)]
        public string Color { get; set; } = "#00ff00";

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; set; } = [];

        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; set; } = [];
    }

}
