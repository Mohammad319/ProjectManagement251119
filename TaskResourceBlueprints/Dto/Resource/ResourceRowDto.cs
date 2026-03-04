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
        decimal? Quantity,
        decimal ChangeFactor1,
        decimal ChangeFactor2,
        decimal WasteFactor,  // كان CapWaste
        decimal? Cost,
        decimal? BaseCost,
        bool IsAssigned      // هل مربوط بالمهمة؟
    );
}
