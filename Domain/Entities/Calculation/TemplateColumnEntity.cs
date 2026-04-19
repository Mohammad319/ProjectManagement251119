using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TemplateColumnEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        [JsonIgnore]
        public bool IsVisible { get; private set; } = true;

        [JsonIgnore]
        public int? DepartmentId { get; private set; }

        [JsonIgnore]
        public DepartmentEntity? Department { get; private set; }

        public List<NetColumnState> Columns { get; private set; } = TemplateDefaults.NetCalc();

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; private set; } = [];

        private TemplateColumnEntity() { }

        public TemplateColumnEntity(string name, bool isVisible, int? departmentId, IEnumerable<NetColumnState>? columns)
        {
            SetName(name);
            IsVisible = isVisible;
            DepartmentId = departmentId;
            SetColumns(columns);
        }

        public void Update(string name, bool isVisible, int? departmentId, IEnumerable<NetColumnState>? columns)
        {
            SetName(name);
            IsVisible = isVisible;
            DepartmentId = departmentId;
            SetColumns(columns);
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Template column name is required.");

            Name = name.Trim();
        }

        public void SetVisibility(bool isVisible)
        {
            IsVisible = isVisible;
        }

        public void SetDepartment(int? departmentId)
        {
            DepartmentId = departmentId;
        }

        public void SetColumns(IEnumerable<NetColumnState>? columns)
        {
            Columns = TemplateDefaults.EnsureNetCalcColumns(columns);
        }

        public List<NetColumnState> GetColumnsSnapshot()
            => TemplateDefaults.EnsureNetCalcColumns(Columns);
    }
}
