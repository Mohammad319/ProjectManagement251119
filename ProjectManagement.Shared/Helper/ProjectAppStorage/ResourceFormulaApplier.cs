using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.DTO.ProjectAppStorage.Service;
using ProjectManagement.Shared.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string Chf1Legacy = "chf1";
        private const string Chf2Legacy = "chf2";
        private const string Cap = "cap";
        private const string Waste = "waste";
        private const string CapWaste = "capwaste";
        private const string Quantity = "quantity";
        private const string TaskThickness = "th";
        private const string TaskWidth = "w";
        private const string TaskLength = "l";
        private static readonly IReadOnlyDictionary<ParamName, decimal> EmptyTaskParameters = new Dictionary<ParamName, decimal>();

        public static void ApplyAll(IList<ResourceDto> rows, IReadOnlyDictionary<ParamName, decimal>? taskParameter)
        {
            if (rows is null || rows.Count == 0) return;

            var tp = taskParameter ?? EmptyTaskParameters;

            foreach (var r in rows)
                ApplyRow(r, tp);
        }

        public static HashSet<string> GetAssignedTargets(IEnumerable<string>? formulas)
        {
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (formulas is null)
                return targets;

            foreach (var raw in formulas)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var parts = raw.Replace(" ", string.Empty).Split('=', 2);
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
                    continue;

                targets.Add(NormalizeTarget(parts[0]));
            }

            return targets;
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
                [Chf1Legacy] = row.Data.ChangeFactor1,
                [Chf2Legacy] = row.Data.ChangeFactor2,
            };

            if (taskParameter.TryGetValue(ParamName.Thickness, out var th)) vars[TaskThickness] = th;
            if (taskParameter.TryGetValue(ParamName.Width, out var w)) vars[TaskWidth] = w;
            if (taskParameter.TryGetValue(ParamName.Length, out var l)) vars[TaskLength] = l;

            if (row.Properties != null)
            {
                foreach (var item in row.Properties)
                {
                    if (!TryGetPropertyNumericValue(item, out var val))
                        continue;

                    vars["p" + item.Id] = val;
                    if (!string.IsNullOrWhiteSpace(item.DisplayName))
                        vars[item.DisplayName] = val;
                }
            }

            // تُنفّذ الصيغ بنفس ترتيب القائمة القادمة من الأدمن.
            // لذلك يمكن للمعادلة اللاحقة أن تعتمد على نتيجة سابقة.
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

        private static string NormalizeTarget(string name)
            => name.ToLowerInvariant() switch
            {
                Chf1Legacy => Chf1,
                Chf2Legacy => Chf2,
                _ => name.ToLowerInvariant()
            };

        private static void SetVar(string name, decimal value, ResourceDto row, Dictionary<string, decimal> vars)
        {
            switch (NormalizeTarget(name))
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
                    row.Data.ChangeFactor1 = value;
                    vars[Chf1] = value;
                    vars[Chf1Legacy] = value;
                    break;

                case Chf2:
                    row.Data.ChangeFactor2 = value;
                    vars[Chf2] = value;
                    vars[Chf2Legacy] = value;
                    break;

                default:
                    vars[name] = value;
                    WritePropertyValue(name, value, row, vars);
                    break;
            }
        }

        private static bool TryGetPropertyNumericValue(ResourcePropertyBindDto property, out decimal value)
        {
            if (property.DataType == DTO.ProjectAppStorage.DataType.Number)
            {
                value = property.NumberDefault ?? 0m;
                return true;
            }

            if (NumericInputHelper.TryParseDecimal(property.TextDefault, out value))
                return true;

            value = default;
            return false;
        }

        private static void WritePropertyValue(string name, decimal value, ResourceDto row, Dictionary<string, decimal> vars)
        {
            var property = FindProperty(row, name);
            if (property is null)
                return;

            if (property.DataType == DTO.ProjectAppStorage.DataType.Number)
                property.NumberDefault = value;
            else
                property.TextDefault = value.ToString(CultureInfo.InvariantCulture);

            vars["p" + property.Id] = value;
            if (!string.IsNullOrWhiteSpace(property.DisplayName))
                vars[property.DisplayName] = value;
        }

        private static ResourcePropertyBindDto? FindProperty(ResourceDto row, string name)
        {
            if (row.Properties is null || row.Properties.Count == 0)
                return null;

            if (TryGetPropertyId(name, out var propertyId))
            {
                var byId = row.Properties.FirstOrDefault(x => x.Id == propertyId);
                if (byId is not null)
                    return byId;
            }

            return row.Properties.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.DisplayName) &&
                string.Equals(x.DisplayName, name, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryGetPropertyId(string name, out int propertyId)
        {
            propertyId = default;
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name[0] is not ('p' or 'P'))
                return false;

            return int.TryParse(name.AsSpan(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out propertyId);
        }
    }
}
