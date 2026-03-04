using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ProjectManagement.Shared.DTO.App.Dataloader
{
    public class CalcResCost
    {
        public bool IsPercent { get; set; } = true;
        public int Morgen { get; set; } = 0;
        public decimal MorgenCost { get; set; }
        public int Day { get; set; } = 0;
        public decimal DayCost { get; set; }
        public int Evening { get; set; }
        public decimal EveningCost { get; set; }
        public decimal? Quantity { get; set; }

        public bool CheckInput(decimal? quantity)
        {
            return IsPercent ? MorgenCost + DayCost <= 100 :
                MorgenCost + DayCost <= quantity.GetValueOrDefault();
        }
        public decimal CalcCost() {
            return (Morgen * MorgenCost)+ (Day * DayCost) + (Evening * EveningCost);
        }
        public void SetQuantity(decimal q)
        {
            Quantity = q;
        }
    }
    public class ResourceDLBase
    {
        public ResourceTypesEnum ResType { get; set; }
        public string Name { get; set; } = null!;
        [Range(0, double.MaxValue)] public double SortOrder { get; set; }
        private ResourceMetadata _data = new();
        public ResourceMetadata Data
        {
            get => _data ??= new ResourceMetadata();
            set => _data = value;
        }
    }
    public class ResourceDLModel : ResourceDLBase
    {
        public int Id { get; set; }
        public int? GroupId { get; set; }
        public bool Active { get; set; } = true;
        public bool IsVisible { get; set; } = true;

    }
    public class ResourceEXDto : ResourceDLBase
    {
        [JsonIgnore] public CalcResCost CalcResCost { get; set; } = new();

        [JsonIgnore] public List<decimal> Values { get; set; } = [];
        public ResourceEXDto(){  }

        public void Backup()
        {
            Values = [];
            Values.Add(Data.CapWaste);
            Values.Add(Data.ChangeFactor1);
            Values.Add(Data.ChangeFactor2);
            Values.Add(Data.BaseCost.HasValue ? Data.BaseCost.Value : 0);
            Values.Add(Data.Cost);
        }
        public int Id { get; set; }
        public string Group { get; set; } = string.Empty;
        public List<ConditionDto> ConditionEffect { get; set; } = new();
        public List<RoleDTO> CapRole { get; set; } = [];
        public List<int> ConditionTaskIds { get; set; } = new();
        public Dictionary<string, decimal> GetVariables()
        {
            return new Dictionary<string, decimal>
        {
            { "quantity", Data.Quantity.HasValue ? Data.Quantity.Value : 0 },
            { "basecost", Data.BaseCost.HasValue ?Data.BaseCost.Value : 0 },
            { "cap", Data.CapWaste },
            { "waste", Data.CapWaste },
            { "cost",  Data.Cost },
            { "chf1", Data.ChangeFactor1 },
            { "chf2", Data.ChangeFactor2 }
        };
        }
        public static List<string> ExtractVariables(string expression)
        {
            var matches = Regex.Matches(expression, @"[a-zA-Z_][a-zA-Z0-9_]*");
            return matches.Cast<Match>()
                          .Select(m => m.Value)
                          .Distinct()
                          .ToList();
        }
        private string ReplaceVariables(string expression, Dictionary<string, decimal> variables)
        {
            foreach (var kvp in variables)
            {
                // استخدم Regex لضمان الاستبدال الكامل للكلمة (بدون استبدال داخل كلمة أخرى)
                expression = Regex.Replace(expression, $@"\b{Regex.Escape(kvp.Key)}\b", kvp.Value.ToString(CultureInfo.InvariantCulture));
            }
            return expression;
        }
        public static double EvaluateExpression(string expression)
        {
            // تأكد من استخدام الفاصلة العشرية الصحيحة للثقافة الحالية (مثلاً النقطة بدلاً من الفاصلة)
            var culture = CultureInfo.InvariantCulture;

            try
            {
                var dt = new DataTable();
                var result = dt.Compute(expression, "");
                return Convert.ToDouble(result, culture);
            }
            catch (System.Exception ex)
            {
                throw new InvalidOperationException($"خطأ في تقييم المعادلة: '{expression}'", ex);
            }
        }
        public void SetVariable(string name, decimal value)
        {
            switch (name.ToLower())
            {
                case "quantity": Data.Quantity = value; break;
                case "chf1": Data.ChangeFactor1 = value; break;
                case "chf2": Data.ChangeFactor2 = value; break;
                case "cap": Data.CapWaste = value; break;
                case "waste": Data.CapWaste = value; break;
                case "basecost": Data.BaseCost = value; break;

                default: Console.WriteLine($"⚠️ المتغير {name} غير معرف داخل المورد."); break;
            }
        }
        public void Calculate(string targetVariable, string formula, Dictionary<string, decimal> externalVars)
        {
            Dictionary<string, decimal> resourceVariables = GetVariables();
            Dictionary<string, decimal> allVariables;
            if (externalVars != null)
                allVariables = resourceVariables
                    .Concat(externalVars.ToDictionary(x => x.Key, x => x.Value))
                    .GroupBy(x => x.Key.ToLower())
                    .ToDictionary(g => g.Key, g => g.First().Value);
            else allVariables = resourceVariables;
            string expressionWithValues = ReplaceVariables(formula, allVariables);
            double result = EvaluateExpression(expressionWithValues);
            SetVariable(targetVariable, (decimal)result);
        }
    }
}
