namespace TaskResourceBlueprints.Dto.Resource
{
    public sealed class ResourceLookupDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Group { get; init; } // اختياري (مثلاً اسم الفولدر)
    }
    public sealed record ResourceRowDto(
        int Id,
        string Name,
        string ResourceType, // أو enum->string
        string? UnitCode,
        double? Quantity,
        double ChangeFactor1,
        double ChangeFactor2,
        double WasteFactor,  // كان CapWaste
        decimal? Cost,
        decimal? BaseCost,
        bool IsAssigned      // هل مربوط بالمهمة؟
    );
}
