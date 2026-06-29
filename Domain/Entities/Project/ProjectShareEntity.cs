using Domain.Entities.Base;
using Domain.Entities.Users;

namespace Domain.Entities.Project
{
    /// <summary>
    /// Intern delning av ett projekt. En rad = ett projekt delat med EN mottagare
    /// (antingen en användare ELLER en avdelning) med en roll. Vilka kalkyler som
    /// faktiskt visas styrs av <see cref="Calculations"/>. Hålls separat från
    /// ShareCalc (som är delning på kalkyl-/avdelningsnivå).
    /// </summary>
    public sealed class ProjectShareEntity : AuditableEntity<int>
    {
        public Guid ProjectId { get; private set; }
        public ProjectEntity Project { get; private set; } = null!;

        // Exakt en av dessa är satt (mottagare = användare ELLER avdelning).
        public int? SharedWithUserId { get; private set; }
        public UserEntity? SharedWithUser { get; private set; }

        public int? DepartmentId { get; private set; }
        public DepartmentEntity? Department { get; private set; }

        /// <summary>Beviljad roll, t.ex. <c>PMRolesConst.Tenant.Viewer</c> eller <c>Manger</c>.</summary>
        public string Role { get; private set; } = string.Empty;

        /// <summary>Delningen gäller till och med detta UTC-datum. <see langword="null"/> = tills vidare.</summary>
        public DateTime? ValidUntil { get; private set; }

        /// <summary>
        /// Delningsomfattning. <see langword="true"/> = "Alla kalkyler i projektet": delningen omfattar
        /// ALLA icke-privata kalkyler i projektet, inklusive nya som skapas senare (kalkyllistan
        /// <see cref="Calculations"/> ignoreras då av åtkomstreglerna). <see langword="false"/> =
        /// "Valda kalkyler": endast de uttryckligen valda kalkylerna i <see cref="Calculations"/>.
        /// Privata kalkyler delas aldrig i något läge.
        /// </summary>
        public bool AllCalculations { get; private set; }

        public ICollection<ProjectShareCalculationEntity> Calculations { get; private set; }
            = new List<ProjectShareCalculationEntity>();

        private ProjectShareEntity() { }

        private ProjectShareEntity(Guid projectId, int? sharedWithUserId, int? departmentId, string role)
        {
            ProjectId = projectId;
            SharedWithUserId = sharedWithUserId;
            DepartmentId = departmentId;
            Role = role ?? string.Empty;
        }

        public static ProjectShareEntity ForUser(Guid projectId, int userId, string role)
            => new(projectId, userId, null, role);

        public static ProjectShareEntity ForDepartment(Guid projectId, int departmentId, string role)
            => new(projectId, null, departmentId, role);

        public void SetRole(string role) => Role = role ?? string.Empty;

        public void SetValidUntil(DateTime? validUntil) => ValidUntil = validUntil?.Date;

        /// <summary>Sätter delningsomfattningen (true = alla kalkyler i projektet, false = valda kalkyler).</summary>
        public void SetAllCalculations(bool allCalculations) => AllCalculations = allCalculations;

        /// <summary>Ersätter listan av valda kalkyler som ingår i delningen.</summary>
        public void ReplaceCalculations(IEnumerable<int>? calculationIds)
        {
            Calculations.Clear();
            foreach (var id in (calculationIds ?? Enumerable.Empty<int>()).Distinct())
                Calculations.Add(new ProjectShareCalculationEntity(id));
        }
    }
}
