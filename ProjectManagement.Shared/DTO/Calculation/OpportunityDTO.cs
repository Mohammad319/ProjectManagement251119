using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Helper;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class OpportunityData
    {
        public decimal? ProbabilityWorth { get; set; }

        [Range(typeof(decimal), "-999", "999", ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public decimal? ProbabilityPercent { get; set; }

        public decimal? ProbabilityBest { get; set; }
        public decimal? Value { get; set; }
        public string Comment { get; set; } = string.Empty;

        public OpportunityData Clone()
        {
            return new OpportunityData
            {
                ProbabilityWorth = ProbabilityWorth,
                ProbabilityPercent = ProbabilityPercent,
                ProbabilityBest = ProbabilityBest,
                Value = Value,
                Comment = MetadataCloneHelper.CopyText(Comment)
            };
        }
    }

    public class PostOpportunityDTO : OpportunityBase
    {
        private OpportunityData? metadata = new();

        public OpportunityData Metadata
        {
            get
            {
                metadata ??= new OpportunityData();
                return metadata;
            }
            set => metadata = value?.Clone() ?? new OpportunityData();
        }

        [JsonIgnore]
        public OpportunityData Data
        {
            get => Metadata;
            set => Metadata = value;
        }
    }

    public class OpportunityListDTO : OpportunityBase
    {
        public int Id { get; set; }
        public int CalculationId { get; set; }

        private OpportunityData? metadata = new();

        public OpportunityData Metadata
        {
            get
            {
                metadata ??= new OpportunityData();
                return metadata;
            }
            set => metadata = value?.Clone() ?? new OpportunityData();
        }

        [JsonIgnore]
        public OpportunityData Data
        {
            get => Metadata;
            set => Metadata = value;
        }
    }
}
