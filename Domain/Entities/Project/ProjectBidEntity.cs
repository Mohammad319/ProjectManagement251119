using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public sealed class ProjectBidEntity : AuditableEntity<int>
    {
        public Guid ProjectId { get; private set; }

        [JsonIgnore]
        public ProjectEntity Project { get; private set; } = null!;

        [Required, MaxLength(FieldLengths.LongName)]
        public string BidderName { get; private set; } = string.Empty;

        public decimal? Amount { get; private set; }

        [MaxLength(FieldLengths.Note)]
        public string? Note { get; private set; }

        public bool IsWinner { get; private set; }

        public int SortOrder { get; private set; }

        private ProjectBidEntity() { }

        public ProjectBidEntity(Guid projectId, string bidderName, decimal? amount, string? note, bool isWinner, int sortOrder)
        {
            ProjectId = projectId;
            BidderName = Normalize(bidderName);
            Amount = amount;
            Note = NormalizeOptional(note);
            IsWinner = isWinner;
            SortOrder = sortOrder;
        }

        public void Update(string bidderName, decimal? amount, string? note, bool isWinner)
        {
            BidderName = Normalize(bidderName);
            Amount = amount;
            Note = NormalizeOptional(note);
            IsWinner = isWinner;
        }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
