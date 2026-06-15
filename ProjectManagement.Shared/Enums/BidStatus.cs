namespace ProjectManagement.Shared.Enums;

/// <summary>
/// Status för en anbudsgivare i Anbud-fönstret.
/// </summary>
public enum BidStatus
{
    /// <summary>Giltigt anbud – räknas i jämförelse och placering.</summary>
    Valid = 0,

    /// <summary>Förkastat anbud – visas men räknas inte som giltigt.</summary>
    Rejected = 1
}
