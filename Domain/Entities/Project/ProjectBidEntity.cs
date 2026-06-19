using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
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

        /// <summary>
        /// Legacy "Vinnare"-flagga. Behålls för bakåtkompatibilitet och hålls
        /// synkad med <see cref="IsAwarded"/> (Tilldelad).
        /// </summary>
        public bool IsWinner { get; private set; }

        /// <summary>Tilldelad. I ramavtal kan flera anbudsgivare vara tilldelade.</summary>
        public bool IsAwarded { get; private set; }

        /// <summary>Manuell placering. Null innebär att placeringen beräknas automatiskt.</summary>
        public int? Placement { get; private set; }

        /// <summary>Anbudsstatus: Giltigt eller Förkastat.</summary>
        public BidStatus Status { get; private set; } = BidStatus.Valid;

        /// <summary>Förkastningsorsak. Fritext, anges när status = Förkastat.</summary>
        [MaxLength(FieldLengths.Note)]
        public string? RejectionReason { get; private set; }

        public int SortOrder { get; private set; }

        private ProjectBidEntity() { }

        public ProjectBidEntity(Guid projectId, string bidderName, decimal? amount, string? note, bool isAwarded, int sortOrder)
        {
            ProjectId = projectId;
            BidderName = Normalize(bidderName);
            Amount = amount;
            Note = NormalizeOptional(note);
            SetAwarded(isAwarded, isAwarded ? 1 : null);
            SortOrder = sortOrder;
        }

        public void Update(string bidderName, decimal? amount, string? note, bool isAwarded, int? placement)
        {
            BidderName = Normalize(bidderName);
            Amount = amount;
            Note = NormalizeOptional(note);
            SetAwarded(isAwarded, placement);
        }

        public void SetPrices(string? pricesJson, decimal? amount)
        {
            PricesJson = string.IsNullOrWhiteSpace(pricesJson) ? null : pricesJson;
            Amount = amount;
        }

        public void SetDeductionPercent(decimal? deductionPercent) =>
            DeductionPercent = deductionPercent;

        /// <summary>
        /// Sätter status. Förkastade anbud kan inte vara tilldelade – tilldelning
        /// och placering nollställs i så fall.
        /// </summary>
        public void SetStatus(BidStatus status, string? rejectionReason)
        {
            Status = status;
            RejectionReason = status == BidStatus.Rejected ? NormalizeOptional(rejectionReason) : null;

            if (status == BidStatus.Rejected)
            {
                SetAwarded(false, null);
                SetManualPlacement(null);
            }
        }

        /// <summary>
        /// Sätter tilldelning. Placering hanteras separat av <see cref="SetManualPlacement"/>.
        /// IsWinner hålls synkad för bakåtkompatibilitet.
        /// </summary>
        public void SetAwarded(bool isAwarded, int? placement)
        {
            IsAwarded = isAwarded;
            IsWinner = isAwarded;
        }

        public void SetManualPlacement(int? placement) =>
            Placement = placement is > 0 ? placement : null;

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
