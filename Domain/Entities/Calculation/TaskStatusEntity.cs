using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.General;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TaskStatusEntity : AuditableEntity<int>, IListOrderDTO
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        [Required, StringLength(FieldLengths.ColorHex)]
        public string Color { get; private set; } = "#00ff00";

        public int SortOrder { get; private set; } = 0;

        public bool IsVisible { get; private set; } = true;

        [JsonIgnore]
        public ICollection<TaskEntity> Tasks { get; private set; } = [];

        // EF Constructor
        public TaskStatusEntity() { }

        // Domain constructor
        public TaskStatusEntity(string name, string color, int sortOrder, bool isVisible = true)
        {
            SetName(name);
            SetColor(color);
            SetSortOrder(sortOrder);
            SetVisibility(isVisible);
        }

        // -------------------
        // Domain behaviors
        // -------------------

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Task status name is required.");

            Name = name.Trim();
        }

        public void SetColor(string color)
        {
            // يمكنك إضافة تحقق لاحقًا (#RRGGBB)
            Color = color;
        }

        public void SetSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
                throw new ValidationException("SortOrder cannot be negative.");

            SortOrder = sortOrder;
        }

        public void SetVisibility(bool isVisible)
        {
            IsVisible = isVisible;
        }

        public void Update(string name, string color, int sortOrder, bool isVisible)
        {
            SetName(name);
            SetColor(color);
            SetSortOrder(sortOrder);
            SetVisibility(isVisible);
        }
    }
}
