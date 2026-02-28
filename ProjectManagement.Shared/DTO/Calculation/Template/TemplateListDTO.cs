using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace ProjectManagement.Shared.DTO.Calculation.Template
{
    public class NetColor
    {
        public string Header { get; set; } = "#e1f0ad";
        public string Note { get; set; } = "#f0ebeb";
        public string Border { get; set; } = "#000";
        public string BorderStyle { get; set; } = "dotted";
        public string Text { get; set; } = "#000";
        public string Task { get; set; } = TemplateConstBase.Task;
        public string SubTask { get; set; } = TemplateConstBase.SubTask;
        public string Resource { get; set; } = TemplateConstBase.Resource;
    }
    public class SummarySheetColor
    {
        public string Header { get; set; } = "#e1f0ad"; //3
        public string Note { get; set; } = "#f0ebeb";
        public string Border { get; set; } = "#000";
        public string BorderStyle { get; set; } = "dotted";
        public string Text { get; set; } = "#000";
        public string Sum { get; set; } = "#c4c7fe";
        public string Factor { get; set; } = "#c4c7fe"; //4
    }
    public class NetCalc
    {
        public NetColor Color { get; set; } = new();
        public List<NetColumnState> Columns { get; set; } = TemplateDefaults.NetCalc();
    }
    public class SummarySheet
    {
        public SummarySheetColor Color { get; set; } = new();
        public List<SummarySheetColumnState> Columns { get; set; } = TemplateDefaults.SummarySheet();
    }
    public class TemplateData
    {
        [JsonIgnore] public int MathRound { get; set; } = 2;
        [MaxLength(5)] public string Currency { get; set; } = "€";
        [MaxLength(15)] public string DateFormat { get; set; } = "dd.MM.yyyy";

        public NetCalc NetCalc { get; set; } = new NetCalc();
        public SummarySheet SummarySheet { get; set; } = new SummarySheet();
    }
    public class TemplateListPostDTO : TemplateData
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;

        [Range(0, 200, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public bool Active { get; set; } = true;
    }
    public class TemplateModelDTO : TemplateData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    public class TemplateListDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? TemplateId { get; set; }
    }
}
