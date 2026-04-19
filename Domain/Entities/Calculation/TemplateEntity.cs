using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TemplateEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        private TemplateMetadataData? _metadata;
        public TemplateMetadataData Metadata
        {
            get => _metadata ??= new TemplateMetadataData();
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
            DepartmentId = departmentId;
            _metadata = TemplateMetadataMapper.Build(metadata);
        }

        public TemplateData GetMetadataSnapshot(IEnumerable<NetColumnState>? columns = null)
            => TemplateMetadataMapper.BuildTemplateData(_metadata, columns);

        public void UpdateMetadata(TemplateData metadata)
        {
            _metadata = TemplateMetadataMapper.Build(metadata);
        }

        public void UpdateMetadata(Action<TemplateData> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            _metadata = TemplateMetadataMapper.Build(snapshot);
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
