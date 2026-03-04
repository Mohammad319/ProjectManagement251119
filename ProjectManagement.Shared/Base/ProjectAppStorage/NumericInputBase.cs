namespace ProjectManagement.Shared.Base.ProjectAppStorage
{
    public class NumericInputBase
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;

        public decimal? MinInputValue { get; set; }
        public decimal? MaxInputValue { get; set; }
        public string SectionKey { get; set; } = string.Empty;
    }
}