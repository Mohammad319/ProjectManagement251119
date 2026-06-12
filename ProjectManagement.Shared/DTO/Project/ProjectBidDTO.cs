namespace ProjectManagement.Shared.DTO.Project
{
    public class ProjectBidPostDTO
    {
        public string BidderName { get; set; } = string.Empty;

        /// <summary>
        /// Legacy single amount. Used when the project has no price columns.
        /// When <see cref="Prices"/> contains values the server recomputes
        /// Amount as the sum of all price parts.
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>Price-column id → amount for this bidder.</summary>
        public Dictionary<int, decimal>? Prices { get; set; }

        /// <summary>Mervärdeavdrag in percent (0–100).</summary>
        public decimal? DeductionPercent { get; set; }

        public string? Note { get; set; }
        public bool IsWinner { get; set; }
    }

    public class ProjectBidListDTO
    {
        public int Id { get; set; }
        public string BidderName { get; set; } = string.Empty;

        /// <summary>Anbudssumma — sum of all price parts (or the legacy single amount).</summary>
        public decimal? Amount { get; set; }

        /// <summary>Price-column id → amount for this bidder.</summary>
        public Dictionary<int, decimal> Prices { get; set; } = new();

        /// <summary>Mervärdeavdrag in percent (0–100).</summary>
        public decimal? DeductionPercent { get; set; }

        public string? Note { get; set; }
        public bool IsWinner { get; set; }
        public int SortOrder { get; set; }

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
        public int SortOrder { get; set; }
    }

    public class ProjectBidPriceColumnPostDTO
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Everything the bid window needs in one round trip.</summary>
    public class ProjectBidsViewDTO
    {
        public List<ProjectBidPriceColumnDTO> PriceColumns { get; set; } = [];
        public List<ProjectBidListDTO> Bids { get; set; } = [];
    }
}
