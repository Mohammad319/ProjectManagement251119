namespace ProjectManagement.Shared.DTO.Project
{
    public class ProjectBidPostDTO
    {
        public string BidderName { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Note { get; set; }
        public bool IsWinner { get; set; }
    }

    public class ProjectBidListDTO
    {
        public int Id { get; set; }
        public string BidderName { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Note { get; set; }
        public bool IsWinner { get; set; }
        public int SortOrder { get; set; }
    }
}
