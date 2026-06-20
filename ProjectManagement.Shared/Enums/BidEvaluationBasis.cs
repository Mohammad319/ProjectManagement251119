namespace ProjectManagement.Shared.Enums;

/// <summary>
/// Utvärderingsgrund för ett projekts anbud. Avgör hur anbuden jämförs och styr
/// vilka beräkningsmetoder (<see cref="BidEvaluationModel"/>) som är tillgängliga.
/// </summary>
public enum BidEvaluationBasis
{
    /// <summary>Pris — anbuden jämförs utifrån pris (lägst jämförelsesumma vinner).</summary>
    Price = 0,

    /// <summary>Kostnad — anbuden jämförs utifrån total kostnad, inte bara pris.</summary>
    Cost = 1,

    /// <summary>Pris och kvalitet — både pris och kvalitet påverkar resultatet
    /// (mervärdeavdrag eller poäng).</summary>
    PriceQuality = 2
}
