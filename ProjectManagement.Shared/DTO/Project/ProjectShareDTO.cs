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

    /// <summary>
    /// En mottagare av åtkomst till ett projekt (för "Åtkomst"-kolumnens sammanfattning och
    /// åtkomstöversikt). Beskriver en enskild extra delning – inte normal avdelningsåtkomst.
    /// </summary>
    public sealed class ProjectAccessRecipientDTO
    {
        public ProjectShareRecipientType Type { get; set; }
        public int? UserId { get; set; }
        public int? DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        /// <summary>Antal kalkyler denna mottagare har delats med.</summary>
        public int CalcCount { get; set; }
    }

    /// <summary>
    /// Kompakt åtkomstsammanfattning per projekt för projektlistans "Åtkomst"-kolumn och filter.
    /// Skiljer normal avdelningsåtkomst (<see cref="ViaDepartment"/>) från extra delning
    /// (<see cref="Recipients"/>).
    /// </summary>
    public sealed class ProjectAccessSummaryDTO
    {
        /// <summary>Den aktuella användaren ser projektet via normal avdelningsåtkomst (egen avdelning, skapare eller admin).</summary>
        public bool ViaDepartment { get; set; }

        /// <summary>Antal delbara kalkyler i projektet (icke-privata, aktuella versioner) – nämnaren i "3/5 kalkyler".</summary>
        public int ShareableCalcCount { get; set; }

        /// <summary>Extra delningar (användare/avdelningar). Tom lista = ingen extra delning.</summary>
        public List<ProjectAccessRecipientDTO> Recipients { get; set; } = [];
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
