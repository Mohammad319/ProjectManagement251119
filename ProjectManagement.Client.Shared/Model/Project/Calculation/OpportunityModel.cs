using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace ProjectManagement.Client.Shared.Model.Project.Calculation
{
    public class OpportunityModel : OpportunityBase
    {
        public int Id { get; set; }
        public bool ShowComment { get; set; }
        public OpportunityData Data { get; set; } = new();

    }
}
