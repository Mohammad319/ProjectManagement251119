using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Shared.Helper.ProjectAppStorage
{
    /// <summary>
    /// يطبّق صيغ ResourceDto.Formulas على ResourceDto.Data باستخدام decimal.
    /// </summary>
    public static class ResourceFormulaApplier
    {
        private const string BaseCost = "basecost";
        private const string Cost = "cost";
        private const string Chf1 = "ch1";
        private const string Chf2 = "ch2";
        private const string Cap = "cap";
        private const string Waste = "waste";
        private const string CapWaste = "capwaste";
        private const string Quantity = "quantity";
        private const string TaskThickness = "th";
        private const string TaskWidth = "w";
        private const string TaskLength = "l";

                public static void ApplyAll(IList<ResourceDto> rows, Dictionary<ParamName, decimal> taskParameter)
        {
            if (rows is null || rows.Count == 0) return;

            var tp = taskParameter is null
                ? new Dictionary<ParamName, decimal>()
                : taskParameter.ToDictionary(k => k.Key, v => v.Value);

            foreach (var r in rows)
                ApplyRow(r, tp);
        }

        public static void ApplyRow(ResourceDto row, IReadOnlyDictionary<ParamName, decimal> taskParameter)
        {
            if (row is null || row.Formulas is null || row.Formulas.Count == 0) return;

            // توافق: بعض الأماكن تستخدم CapWaste كقيمة واحدة
            var capValue = row.Data.Cap != 0m ? row.Data.Cap : row.Data.CapWaste;
            var wasteValue = row.Data.Waste != 0m ? row.Data.Waste : row.Data.CapWaste;

            var vars = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                [BaseCost] = row.Data.BaseCost ?? 0m,
                [Cost] = row.Data.Cost,
                [Quantity] = row.Data.Quantity ?? 0m,
                [Cap] = capValue,
                [Waste] = wasteValue,
                [CapWaste] = row.Data.CapWaste,
                [Chf1] = row.Data.ChangeFactor1,
                [Chf2] = row.Data.ChangeFactor2,
            };

            if (taskParameter != null)
            {
                if (taskParameter.TryGetValue(ParamName.Thickness, out var th)) vars[TaskThickness] = th;
                if (taskParameter.TryGetValue(ParamName.Width, out var w)) vars[TaskWidth] = w;
                if (taskParameter.TryGetValue(ParamName.Length, out var l)) vars[TaskLength] = l;
            }

            // p{id} defaults (number props)
            var numberProps = row.Properties?.Where(x => x.DataType == DTO.ProjectAppStorage.DataType.Number);
            if (numberProps != null)
            {
                foreach (var item in numberProps)
                {
                    var val = (item.NumberDefault.HasValue && item.NumberDefault.Value > 0)
                        ? item.NumberDefault.Value
                        : 0m;

                    vars["p" + item.Id] = val;
                }
            }

            foreach (var raw in row.Formulas)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var formula = raw.Replace(" ", "");
                var parts = formula.Split('=', 2);
                if (parts.Length != 2) continue;

                var lhs = parts[0];
                var rhs = parts[1];

                if (ExpressionEvaluator.TryEval(rhs, vars, out var value))
                    SetVar(lhs, value, row, vars);
            }

            // write back
            if (vars.TryGetValue(BaseCost, out var b)) row.Data.BaseCost = b;
            if (vars.TryGetValue(Cost, out var c)) row.Data.Cost = c;
            if (vars.TryGetValue(Quantity, out var q)) row.Data.Quantity = q;

            if (vars.TryGetValue(Cap, out var cap)) row.Data.Cap = cap;
            if (vars.TryGetValue(Waste, out var waste)) row.Data.Waste = waste;
            if (vars.TryGetValue(CapWaste, out var capw)) row.Data.CapWaste = capw;

            if (vars.TryGetValue(Chf1, out var k1)) row.Data.ChangeFactor1 = k1;
            if (vars.TryGetValue(Chf2, out var k2)) row.Data.ChangeFactor2 = k2;
        }

        private static void SetVar(string name, decimal value, ResourceDto row, Dictionary<string, decimal> vars)
        {
            switch (name.ToLowerInvariant())
            {
                case BaseCost:
                    row.Data.BaseCost = value; vars[BaseCost] = value; break;

                case Cost:
                    row.Data.Cost = value; vars[Cost] = value; break;

                case Quantity:
                    row.Data.Quantity = value; vars[Quantity] = value; break;

                // توافق: cap/waste تاريخيًا كانت تعني CapWaste
                case Cap:
                    row.Data.Cap = value;
                    row.Data.CapWaste = value;
                    vars[Cap] = value;
                    vars[CapWaste] = value;
                    break;

                case Waste:
                    row.Data.Waste = value;
                    row.Data.CapWaste = value;
                    vars[Waste] = value;
                    vars[CapWaste] = value;
                    break;

                case CapWaste:
                    row.Data.CapWaste = value;
                    vars[CapWaste] = value;
                    break;

                case Chf1:
                    row.Data.ChangeFactor1 = value; vars[Chf1] = value; break;

                case Chf2:
                    row.Data.ChangeFactor2 = value; vars[Chf2] = value; break;

                default:
                    vars[name] = value; // متغيرات مثل p12 ...
                    break;
            }
        }
    }
}