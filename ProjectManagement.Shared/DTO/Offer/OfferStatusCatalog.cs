namespace ProjectManagement.Shared.DTO.Offer;

public static class OfferStatusCatalog
{
    public static IReadOnlyList<string> All { get; } =
    [
        "Förbereds",
        "Skickad UE/Leverantör räknar",
        "Offerten fått – ej komplett",
        "Offerten fått – komplett",
        "Uppdaterad i kalkylen"
    ];
}
