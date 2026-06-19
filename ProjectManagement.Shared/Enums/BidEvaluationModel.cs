namespace ProjectManagement.Shared.Enums;

/// <summary>
/// Utvärderingsmodell för ett projekts anbud.
/// </summary>
public enum BidEvaluationModel
{
    /// <summary>Lägsta jämförelsesumma vinner (pris-baserad utvärdering).</summary>
    LowestComparison = 0,

    /// <summary>Högsta totalpoäng vinner (poäng-baserad utvärdering).</summary>
    HighestPoints = 1,

    /// <summary>Lägsta totala anbudskostnad vinner.</summary>
    LowestTotalCost = 2
}
