using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class StatusEntity : AuditableEntity<int>, IListOrderDTO
    {
        /// <summary>
        /// ترتيب العرض لجميع المستخدمين.
        /// </summary>
        public int SortOrder { get; private set; }

        /// <summary>
        /// هل الحالة مرئية في الواجهة؟
        /// </summary>
        public bool IsVisible { get; private set; } = true;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// اللون على هيئة HEX مثل #00FF00.
        /// </summary>
        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
        public string Color { get; private set; } = "#00ff00";

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; set; } = [];

        public void Update(string name, string color, int sortOrder, bool isVisible)
        {
            Name = name;
            Color = color;
            SortOrder = sortOrder;
            IsVisible = isVisible;
        }
    }

}
