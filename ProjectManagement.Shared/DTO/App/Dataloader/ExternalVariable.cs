using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.App.Dataloader
{
    public class ExternalVariable()
    {
        public decimal? Value { get; set; }
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string VariableName { get; set; } = string.Empty;
        public List<decimal> AllowedValues { get; set; } = [];
    }
}
