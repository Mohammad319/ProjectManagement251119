namespace ProjectManagement.Shared.Enums;

/// <summary>
/// Typ av utvärderingsdel (tidigare "priskolumn"). Datamodellen är inte
/// låst till bara pris – en utvärderingsdel kan t.ex. vara pris eller poäng.
/// </summary>
public enum BidPartType
{
    /// <summary>Prisdel – summeras till anbudssumman.</summary>
    Price = 0,

    /// <summary>Poängdel – summeras till totalpoängen.</summary>
    Points = 1
}
