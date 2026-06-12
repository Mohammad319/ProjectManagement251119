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

        /// <summary>JSON dictionary of price-column id → amount. Null for legacy single-amount bids.</summary>
        public string? PricesJson { get; private set; }

        /// <summary>Mervärdeavdrag in percent (0–100). Null/0 means no deduction.</summary>
        public decimal? DeductionPercent { get; private set; }

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

        public void SetPrices(string? pricesJson, decimal? amount)
        {
            PricesJson = string.IsNullOrWhiteSpace(pricesJson) ? null : pricesJson;
            Amount = amount;
        }

        public void SetDeductionPercent(decimal? deductionPercent) =>
            DeductionPercent = deductionPercent;

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
