using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class OpportunityData
    {
        public double? ProbabilityWorth { get; set; }
        [Range(-999, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double? ProbabilityPercent { get; set; }
        public double? ProbabilityBest { get; set; }
        public double? Value { get; set; }
        public string Comment { get; set; }

    }
    public class PostOpportunityDTO : OpportunityBase
    {
        public OpportunityData Metadata { get; set; } = new();
    }
}
