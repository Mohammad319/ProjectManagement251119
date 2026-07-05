using ProjectManagement.Shared.Constant;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Application
{
    public static class SelfInspectionTemplateTypes
    {
        public const string Checklist = "Checklista";
        public const string Handover = "Protokoll / överlämning";
        public const string RiskAnalysis = "Riskanalys";
    }

    public static class RiskAnalysisFieldKeys
    {
        public const string Risk = "risk";
        public const string Consequence = "konsekvens";
        public const string Category = "kategori";
        public const string Probability = "sannolikhet";
        public const string Impact = "konsekvensvarde";
        public const string RiskValue = "riskvarde";
        public const string RiskLevel = "riskniva";
        public const string RiskOwner = "riskagare";
        public const string RiskActivity = "riskaktivitet";
        public const string Responsible = "ansvarig";
        public const string FollowUpDate = "uppfoljningsdatum";
        public const string Comment = "kommentar";
        public const string Done = "klart";
    }

    public class ApplicationDataBase
    {
        public string Description { get; set; } = string.Empty;
        public string TemplateType { get; set; } = SelfInspectionTemplateTypes.Checklist;
        public string Purpose { get; set; } = string.Empty;
        public string LinkType { get; set; } = "Kalkyl";
        public bool AllDepartments { get; set; } = true;
        public bool RequiredBeforeOffer { get; set; }
        public int SortOrder { get; set; }
        public bool IsSystemTemplate { get; set; }
        public string SystemTemplateKey { get; set; } = string.Empty;
        public string CopiedFromSystemTemplateKey { get; set; } = string.Empty;
        public List<SelfInspectionSectionData> Sections { get; set; } = [];
    }

    public sealed class SelfInspectionSectionData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool CollapsedByDefault { get; set; }

        public SelfInspectionSectionData Clone()
        {
            return new SelfInspectionSectionData
            {
                Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
                Title = Title ?? string.Empty,
                Description = Description ?? string.Empty,
                SortOrder = SortOrder,
                IsVisible = IsVisible,
                CollapsedByDefault = CollapsedByDefault
            };
        }
    }
    public class ApplicationBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public int UserId { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }
}
