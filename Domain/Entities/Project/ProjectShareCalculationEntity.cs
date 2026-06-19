using Domain.Entities.Base;
using Domain.Entities.Calculation;

namespace Domain.Entities.Project
{
    /// <summary>En vald kalkyl som ingår i en projektdelning (<see cref="ProjectShareEntity"/>).</summary>
    public sealed class ProjectShareCalculationEntity : IntBaseEntity
    {
        public int ProjectShareId { get; private set; }
        public ProjectShareEntity ProjectShare { get; private set; } = null!;

        public int CalculationId { get; private set; }
        public CalculationEntity Calculation { get; private set; } = null!;

        private ProjectShareCalculationEntity() { }

        public ProjectShareCalculationEntity(int calculationId)
        {
            CalculationId = calculationId;
        }
    }
}
