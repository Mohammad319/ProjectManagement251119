using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TemplateEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        private TemplateData? _metadata;
        public TemplateData Metadata
        {
            get => _metadata ??= new TemplateData();
            private set => _metadata = TemplateMetadataMapper.Build(value);
        }

        [JsonIgnore]
        public bool IsVisible { get; private set; } = true;

        [JsonIgnore]
        public int? DepartmentId { get; private set; }

        [JsonIgnore]
        public DepartmentEntity? Department { get; private set; }

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; private set; } = [];

        private TemplateEntity() { }

        public TemplateEntity(string name, bool isVisible, int? departmentId)
        {
            SetName(name);
            IsVisible = isVisible;
            DepartmentId = departmentId;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Template name is required.");

            Name = name.Trim();
        }

        public void Update(string name, bool isVisible, int? departmentId, TemplateData metadata)
        {
            SetName(name);
            IsVisible = isVisible;
            //DepartmentId = departmentId;
            Metadata = metadata;
        }

        public TemplateData GetMetadataSnapshot()
            => TemplateMetadataMapper.Build(_metadata);

        public void UpdateMetadata(TemplateData metadata)
        {
            Metadata = metadata;
        }

        public void UpdateMetadata(Action<TemplateData> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            Metadata = snapshot;
        }

        public void SetVisibility(bool isVisible)
        {
            IsVisible = isVisible;
        }

        public void SetDepartment(int? departmentId)
        {
            DepartmentId = departmentId;
        }
    }
}
