using ProjectManagement.Shared.Base.Calculation;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class IncomeListDTO : IncomeBase
    {
        public int Id { get; set; }
    }

    public class IncomePostDTO : IncomeBase
    {
        public int CalculationId { get; set; }
    }
}
