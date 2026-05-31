using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class PostTaskStatusDTO :  TaskStatusBase
    {
        public bool IsVisible { get; set; } = true;
        public bool IsApprovalStatus { get; set; }
        public bool LocksCalculation { get; set; }
        public bool AllowsProductionCalculation { get; set; }
        public bool CountsAsSubmittedBid { get; set; }
        public bool CountsAsWonBid { get; set; }
        public bool CountsAsLostBid { get; set; }
    }
}
