namespace Application.Feature.Calculation.Opportunity;

public sealed class OpportunityListItemDto
{
    public int Id { get; set; }
    public int CalculationId { get; set; }
    public string OpportunitiesRisks { get; set; } = string.Empty;
    public string OpportunityType { get; set; } = string.Empty;
    public double? ProbabilityWorth { get; set; }
    public double? ProbabilityPercent { get; set; }
    public double? ProbabilityBest { get; set; }
    public double? Value { get; set; }
    public string Comment { get; set; } = string.Empty;
}
