using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.Project
{
    /// <summary>Mottagartyp för en intern projektdelning.</summary>
    public enum ProjectShareRecipientType
    {
        User = 0,
        Department = 1
    }

    /// <summary>En befintlig projektdelning (för delningsdialogens lista).</summary>
    public sealed class ProjectShareListItemDTO
    {
        public int Id { get; set; }
        public ProjectShareRecipientType RecipientType { get; set; }
        public int? UserId { get; set; }
        public int? DepartmentId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<int> CalculationIds { get; set; } = [];
    }

    /// <summary>Skapa eller uppdatera en projektdelning. <see cref="Id"/> = null ⇒ ny delning.</summary>
    public sealed class ProjectShareUpsertDTO
    {
        public int? Id { get; set; }
        public ProjectShareRecipientType RecipientType { get; set; }
        public int? UserId { get; set; }
        public int? DepartmentId { get; set; }
        public string Role { get; set; } = string.Empty;
        public List<int> CalculationIds { get; set; } = [];
    }
}
