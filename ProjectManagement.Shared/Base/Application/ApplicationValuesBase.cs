using ProjectManagement.Shared.Constant;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Application
{
    public class ApplicationValuesData
    {
        public Dictionary<Guid, string> Attributes { get; set; } = [];
        public List<RiskAnalysisRowData> RiskRows { get; set; } = [];

        public ApplicationValuesData Clone()
        {
            return new ApplicationValuesData
            {
                Attributes = Attributes?.ToDictionary(x => x.Key, x => x.Value ?? string.Empty) ?? [],
                RiskRows = RiskRows?.Select(x => x.Clone()).ToList() ?? []
            };
        }
    }

    public sealed class RiskAnalysisRowData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Risk { get; set; } = string.Empty;
        public string Consequence { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Probability { get; set; } = 1;
        public int Impact { get; set; } = 1;
        public string RiskOwner { get; set; } = string.Empty;
        public string RiskActivity { get; set; } = string.Empty;
        public string Responsible { get; set; } = string.Empty;
        public DateTime? FollowUpDate { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool Done { get; set; }

        public int RiskValue => Math.Clamp(Probability, 1, 5) * Math.Clamp(Impact, 1, 5);
        public string RiskLevel => RiskValue switch
        {
            <= 4 => "Låg",
            <= 9 => "Måttlig",
            <= 15 => "Hög",
            _ => "Mycket hög"
        };

        public bool RequiresAction => RiskValue >= 10;

        public RiskAnalysisRowData Clone()
        {
            return new RiskAnalysisRowData
            {
                Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
                Risk = Risk ?? string.Empty,
                Consequence = Consequence ?? string.Empty,
                Category = Category ?? string.Empty,
                Probability = Math.Clamp(Probability, 1, 5),
                Impact = Math.Clamp(Impact, 1, 5),
                RiskOwner = RiskOwner ?? string.Empty,
                RiskActivity = RiskActivity ?? string.Empty,
                Responsible = Responsible ?? string.Empty,
                FollowUpDate = FollowUpDate,
                Comment = Comment ?? string.Empty,
                Done = Done
            };
        }
    }
    public class ApplicationValuesBase
    {
        public int UserId { get; set; }
        ApplicationValuesData? data;
        public ApplicationValuesData Data { get { data ??= new ApplicationValuesData(); return data; } set { data = value?.Clone() ?? new ApplicationValuesData(); } }

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        [MaxLength(50, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Responsible { get; set; } = string.Empty;
        public DateTime LastUpdate { get; set; } = DateTime.Now;

    }
}
