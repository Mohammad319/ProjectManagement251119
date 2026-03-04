using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.App.Dataloader
{
    public class ExternalVariable()
    {
        public decimal? Value { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string VariableName { get; set; } = string.Empty;
        public List<double> AllowedValues { get; set; } = [];
    }
}
