using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public class CompensationEntity : AuditableEntity<int>
    {
        public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;

    [Required(
        ErrorMessageResourceName = ErrorsMessages.FieldIsRequred,
        ErrorMessageResourceType = typeof(ResLocalize))]
    [MaxLength(
        80,
        ErrorMessageResourceName = ErrorsMessages.MaxLength,
        ErrorMessageResourceType = typeof(ResLocalize))]
    public string Name { get; set; } = string.Empty;

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
