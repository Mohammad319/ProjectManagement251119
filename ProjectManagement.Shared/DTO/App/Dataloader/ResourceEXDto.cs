using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
using ProjectManagement.Shared.Resource;
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
    public class ResourceDLBase
    {
        public ResourceTypesEnum ResType { get; set; }
        public string Name { get; set; } = null!;
        [Range(0, int.MaxValue)] public int SortOrder { get; set; }
        public decimal? Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
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
        private static string SharedText(string key, string fallback)
            => ResLocalize.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;

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
            var cap = Data.Cap != 0m ? Data.Cap : Data.CapWaste;
            var waste = Data.Waste != 0m ? Data.Waste : Data.CapWaste;

            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["quantity"] = Quantity ?? 0m,
                ["basecost"] = Data.BaseCost ?? 0m,
                ["cost"] = Data.Cost,
                ["chf1"] = Data.ChangeFactor1,
                ["chf2"] = Data.ChangeFactor2,
                ["cap"] = cap,
                ["waste"] = waste,
                ["capwaste"] = Data.CapWaste
            };
        }

        public void SetVariable(string name, decimal value)
        {
            switch (name.ToLowerInvariant())
            {
                case "quantity": Quantity = value; break;
                case "chf1": Data.ChangeFactor1 = value; break;
                case "chf2": Data.ChangeFactor2 = value; break;

                // Backward compatibility
                case "cap": Data.Cap = value; Data.CapWaste = value; break;
                case "waste": Data.Waste = value; Data.CapWaste = value; break;
                case "capwaste": Data.CapWaste = value; break;

                case "basecost": Data.BaseCost = value; break;
                case "cost": Data.Cost = value; break;

                default:
                    System.Diagnostics.Trace.TraceWarning(
                        SharedText("UndefinedResourceVariable", "The variable {0} is not defined inside the resource."),
                        name);
                    break;
            }
        }

        public void Calculate(string targetVariable, string formula, Dictionary<string, decimal> externalVars)
        {
            var all = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in GetVariables())
                all[kv.Key] = kv.Value;

            if (externalVars != null)
                foreach (var kv in externalVars)
                    all[kv.Key] = kv.Value;

            if (!ExpressionEvaluator.TryEval(formula, all, out var result))
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    SharedText("FormulaEvaluationFailed", "Failed to evaluate the formula: '{0}'"),
                    formula));

            SetVariable(targetVariable, result);
        }

   }
}
