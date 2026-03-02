using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Shared.Helper.ProjectAppStorage
{
    /// <summary>
    /// يطبّق صيغ FinalRow.Formulas على خصائص FinalRow (basecost, cost, quantity, capWaste, ch1, ch2).
    /// </summary>
    public static class ResourceFormulaApplier
    {
        const string BaseCost = "basecost";
        const string Cost = "cost";
        const string Chf1 = "ch1";
        const string Chf2 = "ch2";
        const string Cap = "cap";
        const string Waste = "waste";
        const string Quantity = "quantity";
        const string TaskThickness = "th";
        const string TaskWidth = "w";
        const string TaskLength = "l";

        public static void ApplyAll(IList<ResourceDto> rows, Dictionary<ParamName, double> taskParameter)
        {
            if (rows is null || rows.Count == 0) return;
            foreach (var r in rows)
                ApplyRow(r, taskParameter);
        }

        public static void ApplyRow(ResourceDto row, Dictionary<ParamName, double> taskParameter)
        {
            if (row is null || row.Formulas is null || row.Formulas.Count == 0) return;
            var vars = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                // Money fields are stored as decimal in ResourceMetadata; the formula engine works in double.
                [BaseCost] = row.Data.BaseCost.HasValue ? (double)row.Data.BaseCost.Value : 0d,
                [Cost] = (double)row.Data.Cost,
                [Quantity] = row.Data.Quantity ?? 0,
                [Cap] = row.Data.CapWaste,
                [Waste] = row.Data.CapWaste,
                [Chf1] = row.Data.ChangeFactor1,
                [Chf2] = row.Data.ChangeFactor2,
            };
            if (taskParameter.TryGetValue(ParamName.Thickness, out var th))
                vars[TaskThickness] = th;
            if (taskParameter.TryGetValue(ParamName.Width, out var tsw))
                vars[TaskWidth] = tsw;
            if (taskParameter.TryGetValue(ParamName.Length, out var tsl))
                vars[TaskLength] = tsl;

            var paramNames = row.Properties.Where(x => x.DataType == DTO.ProjectAppStorage.DataType.Number
            );
            if (paramNames != null) foreach (var item in paramNames)
                {
                    vars.Add("p" + item.Id, item.NumberDefault.HasValue && item.NumberDefault >0?
                        item.NumberDefault.Value:0);
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
                {
                    SetVar(lhs, value, row, vars);
                }
            }

            row.Data.BaseCost = vars.TryGetValue(BaseCost, out var b) ? (decimal)b : row.Data.BaseCost;
            row.Data.Cost = vars.TryGetValue(Cost, out var c) ? (decimal)c : row.Data.Cost;
            row.Data.Quantity = vars.TryGetValue(Quantity, out var q) ? q : row.Data.Quantity;
            row.Data.CapWaste = vars.TryGetValue(Cap, out var w) ? w : row.Data.CapWaste;
            row.Data.CapWaste = vars.TryGetValue(Waste, out var cap) ? cap : row.Data.CapWaste;
            row.Data.ChangeFactor1 = vars.TryGetValue(Chf1, out var k1) ? k1 : row.Data.ChangeFactor1;
            row.Data.ChangeFactor2 = vars.TryGetValue(Chf2, out var k2) ? k2 : row.Data.ChangeFactor2;
        }

        private static void SetVar(string name, double value, ResourceDto row, Dictionary<string, double> vars)
        {
            switch (name.ToLowerInvariant())
            {
                case BaseCost: row.Data.BaseCost = (decimal)value; vars[BaseCost] = value; break;
                case Cost: row.Data.Cost = (decimal)value; vars[Cost] = value; break;
                case Quantity: row.Data.Quantity = value; vars[Quantity] = value; break;
                case Waste: row.Data.CapWaste = value; vars[Waste] = value; break;
                case Cap: row.Data.CapWaste = value; vars[Cap] = value; break;
                case Chf1: row.Data.ChangeFactor1 = value; vars[Chf1] = value; break;
                case Chf2: row.Data.ChangeFactor2 = value; vars[Chf2] = value; break;
                default:
                    vars[name] = value;
                    break;
            }
        }
    }
}
