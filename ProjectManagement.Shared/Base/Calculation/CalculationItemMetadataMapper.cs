namespace ProjectManagement.Shared.Base.Calculation
{
    public static class CalculationItemMetadataMapper
    {
        public static TaskMetadata CloneTaskMetadata(TaskMetadata? metadata)
        {
            var clone = metadata?.Clone() ?? new TaskMetadata();
            clone.Note = NormalizeOptional(clone.Note);
            clone.Unit = NormalizeOptional(clone.Unit);
            clone.BaseUnit = string.IsNullOrWhiteSpace(clone.BaseUnit) ? null : clone.BaseUnit.Trim();
            clone.Code = NormalizeOptional(clone.Code);
            clone.Responsible = NormalizeOptional(clone.Responsible);
            clone.QuantityParam = NormalizeOptional(clone.QuantityParam);
            clone.Normalize();
            return clone;
        }

        public static TaskMetadata BuildTaskMetadata(
            TaskMetadata? metadata,
            string? note,
            string? unit,
            string? code,
            bool isActive,
            TaskType type,
            bool isOH)
        {
            var clone = CloneTaskMetadata(metadata);
            clone.Note = NormalizeOptional(note);
            clone.Unit = NormalizeOptional(unit);
            clone.Code = NormalizeOptional(code);
            clone.IsActive = isActive;
            clone.Type = type;
            clone.IsOH = isOH;
            clone.Normalize();
            return clone;
        }

        public static ResourceMetadata CloneResourceMetadata(ResourceMetadata? metadata)
        {
            var clone = metadata?.Clone() ?? new ResourceMetadata();
            clone.Note = NormalizeOptional(clone.Note);
            clone.Unit = NormalizeOptional(clone.Unit);
            clone.QuantityParam = NormalizeOptional(clone.QuantityParam);
            clone.Normalize();
            return clone;
        }

        public static ResourceMetadata BuildResourceMetadata(
            ResourceMetadata? metadata,
            string? note,
            string? unit)
        {
            var clone = CloneResourceMetadata(metadata);
            clone.Note = NormalizeOptional(note);
            clone.Unit = NormalizeOptional(unit);
            clone.Normalize();
            return clone;
        }

        private static string NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
