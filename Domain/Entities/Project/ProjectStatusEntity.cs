using Domain.Entities.Base;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class ProjectStatusEntity : ColoredListEntity
    {
        public bool CountsAsSubmittedBid { get; private set; }
        public bool CountsAsWonBid { get; private set; }
        public bool CountsAsLostBid { get; private set; }
        public bool IsDefault { get; private set; }

        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; private set; } = [];

        public ProjectStatusEntity() { }

        public ProjectStatusEntity(string name, string color, int sortOrder, bool isVisible = true)
            : base(name, color, sortOrder, isVisible) { }

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
