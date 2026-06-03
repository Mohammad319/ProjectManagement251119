using Domain.Entities.Base;
using Domain.Entities.Calculation;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class StatusEntity : ColoredListEntity
    {
        public bool IsApprovalStatus { get; private set; }
        public bool LocksCalculation { get; private set; }
        public bool AllowsProductionCalculation { get; private set; }
        public bool CountsAsSubmittedBid { get; private set; }
        public bool CountsAsWonBid { get; private set; }
        public bool CountsAsLostBid { get; private set; }
        public bool IsDefault { get; private set; }

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; private set; } = [];

        public StatusEntity() { }

        public StatusEntity(string name, string color, int sortOrder, bool isVisible = true)
            : base(name, color, sortOrder, isVisible) { }

        public void SetApprovalSettings(
            bool isApprovalStatus,
            bool locksCalculation,
            bool allowsProductionCalculation)
        {
            IsApprovalStatus = isApprovalStatus;
            LocksCalculation = locksCalculation;
            AllowsProductionCalculation = allowsProductionCalculation;
        }

        public void SetHitRateSettings(
            bool countsAsSubmittedBid,
            bool countsAsWonBid,
            bool countsAsLostBid)
        {
            CountsAsSubmittedBid = countsAsSubmittedBid || countsAsWonBid || countsAsLostBid;
            CountsAsWonBid = countsAsWonBid;
            CountsAsLostBid = !countsAsWonBid && countsAsLostBid;
        }

        public void SetIsDefault(bool isDefault) => IsDefault = isDefault;
    }
}
