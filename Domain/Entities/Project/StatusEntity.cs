using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class StatusEntity : AuditableEntity<int>
    {
        /// <summary>
        /// ترتيب العرض لجميع المستخدمين.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// هل الحالة مرئية في الواجهة؟
        /// </summary>
        public bool IsVisible { get; set; } = true;

        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }

        /// <summary>
        /// اللون على هيئة HEX مثل #00FF00.
        /// </summary>
        [StringLength(
            7,
            ErrorMessageResourceName = ErrorsMessages.MaxLength,
            ErrorMessageResourceType = typeof(ResLocalize),
            MinimumLength = 7)]
        public string Color { get; set; } = "#00ff00";

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; set; } = [];
    }

}
