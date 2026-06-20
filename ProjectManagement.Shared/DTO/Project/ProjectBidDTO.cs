using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.DTO.Project
{
    public class ProjectBidPostDTO
    {
        public string BidderName { get; set; } = string.Empty;

        /// <summary>
        /// Legacy single amount. Used when the project has no evaluation parts.
        /// When <see cref="Prices"/> contains price-part values the server recomputes
        /// Amount as the sum of all price parts.
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>Evaluation-part id → value for this bidder (price and point parts alike).</summary>
        public Dictionary<int, decimal>? Prices { get; set; }

        /// <summary>Mervärdeavdrag in percent (0–100).</summary>
        public decimal? DeductionPercent { get; set; }

        public string? Note { get; set; }

        /// <summary>Tilldelad. Flera anbudsgivare kan vara tilldelade (ramavtal).</summary>
        public bool IsAwarded { get; set; }

        /// <summary>Slutlig placering. Används som manuell placering när överskrivning är aktiv.</summary>
        public int? Placement { get; set; }

        /// <summary>
        /// Null ger bakåtkompatibilitet med äldre klienter (angiven placering tolkas manuellt).
        /// False återställer automatisk placering.
        /// </summary>
        public bool? IsPlacementManuallyOverridden { get; set; }

        /// <summary>Anbudsstatus: Giltigt eller Förkastat.</summary>
        public BidStatus Status { get; set; } = BidStatus.Valid;

        /// <summary>Förkastningsorsak (anges när status = Förkastat).</summary>
        public string? RejectionReason { get; set; }
    }

    public class ProjectBidListDTO
    {
        public int Id { get; set; }
        public string BidderName { get; set; } = string.Empty;

        /// <summary>Anbudssumma — sum of all price parts (or the legacy single amount).</summary>
        public decimal? Amount { get; set; }

        /// <summary>Evaluation-part id → value for this bidder (price and point parts alike).</summary>
        public Dictionary<int, decimal> Prices { get; set; } = new();

        /// <summary>Mervärdeavdrag in percent (0–100).</summary>
        public decimal? DeductionPercent { get; set; }

        public string? Note { get; set; }

        /// <summary>Tilldelad.</summary>
        public bool IsAwarded { get; set; }

        /// <summary>Slutlig placering som visas i UI och export.</summary>
        public int? Placement { get; set; }

        public int? AutoPlacement { get; set; }
        public int? ManualPlacement { get; set; }
        public bool IsPlacementManuallyOverridden { get; set; }

        /// <summary>Anbudsstatus: Giltigt eller Förkastat.</summary>
        public BidStatus Status { get; set; } = BidStatus.Valid;

        /// <summary>Förkastningsorsak.</summary>
        public string? RejectionReason { get; set; }

        /// <summary>Totalpoäng — sum of all point-part values. Null when there are no point parts.</summary>
        public decimal? TotalPoints { get; set; }

        public int SortOrder { get; set; }

        /// <summary>True när anbudet är giltigt och alltså räknas i jämförelse/placering.</summary>
        public bool IsValid => Status == BidStatus.Valid;

        /// <summary>Jämförelsesumma = Amount − (Amount × DeductionPercent / 100).</summary>
        public decimal? ComparisonAmount =>
            Amount.HasValue
                ? Amount.Value - Amount.Value * (DeductionPercent ?? 0) / 100m
                : null;
    }

    public class ProjectBidPriceColumnDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Typ av utvärderingsdel, t.ex. Pris eller Poäng.</summary>
        public BidPartType PartType { get; set; } = BidPartType.Price;

        public int SortOrder { get; set; }
    }

    public class ProjectBidPriceColumnPostDTO
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Typ av utvärderingsdel. Default Pris för bakåtkompatibilitet.</summary>
        public BidPartType PartType { get; set; } = BidPartType.Price;
    }

    /// <summary>
    /// One bid in one project, flattened for the "Anbudsjämförelse"-report so that
    /// bids for many projects can be fetched in a single round trip. Project name/code
    /// and folder are resolved client-side from the already-loaded project list.
    /// </summary>
    public class ProjectBidComparisonRowDTO
    {
        public Guid ProjectId { get; set; }

        /// <summary>Projektets beräkningsmetod (lägsta jämförelsesumma / högsta poäng / total kostnad).</summary>
        public BidEvaluationModel EvaluationModel { get; set; } = BidEvaluationModel.LowestComparison;

        /// <summary>Projektets utvärderingsgrund (Pris / Kostnad / Pris och kvalitet).</summary>
        public BidEvaluationBasis EvaluationBasis { get; set; } = BidEvaluationBasis.Price;

        public int BidId { get; set; }
        public string BidderName { get; set; } = string.Empty;

        /// <summary>Anbudssumma.</summary>
        public decimal? Amount { get; set; }

        /// <summary>Mervärdeavdrag i procent.</summary>
        public decimal? DeductionPercent { get; set; }

        /// <summary>Jämförelsesumma = Amount − (Amount × DeductionPercent / 100).</summary>
        public decimal? ComparisonAmount { get; set; }

        /// <summary>Totalpoäng — summa av poängdelarna (null när poängdelar saknas).</summary>
        public decimal? TotalPoints { get; set; }

        public bool IsAwarded { get; set; }
        public int? Placement { get; set; }
        public int? AutoPlacement { get; set; }
        public int? ManualPlacement { get; set; }
        public bool IsPlacementManuallyOverridden { get; set; }
        public BidStatus Status { get; set; } = BidStatus.Valid;
        public string? RejectionReason { get; set; }
        public string? Note { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>True när anbudet är giltigt (räknas i jämförelse/placering).</summary>
        public bool IsValid => Status == BidStatus.Valid;
    }

    /// <summary>Everything the bid window needs in one round trip.</summary>
    public class ProjectBidsViewDTO
    {
        /// <summary>Utvärderingsdelar (tidigare priskolumner) för projektet.</summary>
        public List<ProjectBidPriceColumnDTO> PriceColumns { get; set; } = [];

        public List<ProjectBidListDTO> Bids { get; set; } = [];

        /// <summary>Beräkningsmetod för projektet.</summary>
        public BidEvaluationModel EvaluationModel { get; set; } = BidEvaluationModel.LowestComparison;

        /// <summary>Utvärderingsgrund för projektet (Pris / Kostnad / Pris och kvalitet).</summary>
        public BidEvaluationBasis EvaluationBasis { get; set; } = BidEvaluationBasis.Price;
    }
}
