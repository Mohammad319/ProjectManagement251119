namespace TaskResourceBlueprints.Dto.Resource
{
    public sealed record ResourceRowDto(
        int Id,
        string Name,
        string ResourceType, // أو enum->string
        string? UnitCode,
        double? Quantity,
        double ChangeFactor1,
        double ChangeFactor2,
        double WasteFactor,  // كان CapWaste
        double? Cost,
        double? BaseCost,
        bool IsAssigned      // هل مربوط بالمهمة؟
    );
}
