using ProjectManagement.Shared.Base.Calculation;

namespace Persistence.Service.CalculationItems.Calculation;

/// <summary>
/// Flat row for optimized Calculation Page query (Task + Resource + Offer).
/// Types are aligned with your domain entities:
/// - TaskMetadata / ResourceMetadata
/// - ResourceTypesEnum
/// </summary>
internal sealed class CalcPageFlatRow
{
    // --------------------
    // Task
    // --------------------
    public int TaskId { get; init; }
    public int? ParentTaskId { get; init; }
    public string TaskName { get; init; } = string.Empty;
    public double TaskOrder { get; init; }
    public int? TaskStatusId { get; init; }
    public string TaskStatusName { get; init; } = string.Empty;
    public string TaskStatusColor { get; init; } = string.Empty;
    public int? TaskOpportunityId { get; init; }
    public string TaskOpportunity { get; init; } = string.Empty;
    public TaskMetadata TaskMetadata { get; init; } = new();

    // --------------------
    // Resource (nullable due to LEFT JOIN)
    // --------------------
    public int? ResourceId { get; init; }
    public string? ResourceName { get; init; }
    public bool? ResourceIsActive { get; init; }
    public ResourceTypesEnum? ResType { get; init; }

    public int? ResourceSortId { get; init; }
    public int? ResourceTypeId { get; init; }
    public int? ResourceAccountId { get; init; }
    public int? ResourceStatusId { get; init; }
    public int? ResourcePrimaryOfferId { get; init; }

    public double? ResourceOrder { get; init; }
    public int? ResourceOpportunityId { get; init; }
    public string? ResourceOpportunity { get; init; }

    public string? ResourceStatusColor { get; init; }
    public string? ResourceStatus { get; init; }
    public string? ResourceSort { get; init; }
    public string? ResourceTypeName { get; init; }
    public string? ResourceAccount { get; init; }
    public string? ResourceAccountCode { get; init; }

    public ResourceMetadata? ResourceMetadata { get; init; }

    // --------------------
    // Offer (nullable due to LEFT JOIN)
    // --------------------
    public int? OfferId { get; init; }
    public decimal OfferBaseCost { get; init; }
    public decimal OfferCost { get; init; }
    public string? OfferComment { get; init; }
    public DateTime? OfferDate { get; init; }
    public int? OfferOrganisationId { get; init; }
    public string? OfferOrganisation { get; init; }
    public string? OfferSubCategory { get; init; }
    public string? OfferCategory { get; init; }
}
