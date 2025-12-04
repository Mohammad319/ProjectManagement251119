using ProjectManagement.Shared.Constant;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Calculation.Template
{
    public class TemplateData
    {
        [JsonIgnore] public int MathRound { get; set; } = 2;
        [MaxLength(5)] public string Currency { get; set; } = "€";
        [MaxLength(15)] public string DateFormat { get; set; } = "dd.MM.yyyy";
        public List<int> NetWidth { get; set; } = [50,50,70,120,100,180,120,110,70,45,60,125,125,55,45,85,100,80,100,90,60,90,120,130
            ,70,100,80,110,85,100,60,60,80];
        public List<int> FreezList { get; set; } = [];

        public List<int> NetOrder { get; set; } = [0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23
        ,24,25,26,27,28,29,30,31,32];
        public List<string> NetColor { get; set; } = [ "dotted", "#000" ,"#000" ,"#e1f0ad",
            TemplateConstBase.Task,TemplateConstBase.SubTask ,TemplateConstBase.Resource, "#f0ebeb"];

        public List<int> SSOrder { get; set; } = [0, 1, 2, 3, 4, 5, 6, 7, 8];
        public List<int> SSWidth { get; set; } = [100, 80, 60, 60, 60, 100, 60, 60, 90, 80, 60, 60, 120, 80, 80];
        //0 borderStyle,1 borderColor,2 font,3 Header,4factor, ,5 Sum,
        public List<string> SSColor { get; set; } = ["dotted", "#000", "#000", "#e5f3e6", "#c4c7fe", "#c4c7fe"];

    }
    public class TemplateListPostDTO: TemplateData
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; }

        [Range(0, 200, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public bool Active { get; set; } = true;
    }
    public class TemplateModelDTO : TemplateData
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class TemplateListDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? TemplateId { get; set; }
    }
}
