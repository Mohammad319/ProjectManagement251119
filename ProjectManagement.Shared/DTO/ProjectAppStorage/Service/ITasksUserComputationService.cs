using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Base.ProjectAppStorage;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
using ProjectManagement.Shared.Resource;
using System.Globalization;

namespace ProjectManagement.Shared.DTO.ProjectAppStorage.Service;

public class ResourceMenuGroup
{
    public int MenuId { get; set; }
    public List<ResourceDto> Resources { get; set; } = new();
    public int SelectedResourceId { get; set; }
    public bool Rendered { get; set; }
}

public sealed class UserAnswers
{
    public HashSet<int> SelectedChoiceOptionIds { get; } = [];
    public HashSet<int> SelectedResourceChoiceItemIds { get; } = [];

    public Dictionary<int, decimal?> NumericValuesByGroupId { get; } = [];
}

public interface ITasksUserComputationServiceWasm
{
    void ReCalcCostResources(List<ResourceDto> resources);
    void ReCalcCapResources(List<ResourceDto> resources);
    void CalcQuantityResource(List<ResourceDto> resource, decimal? taskQuantity);
    void RefreshResources(List<ResourceDto> resources, decimal? taskQuantity, IReadOnlyDictionary<ParamName, decimal>? taskParameters);
    bool BuildFinalRows(ProjectTaskDto task);
    decimal ComputeOrThrow(string toUnit, string fromUnit, decimal quantity, IReadOnlyDictionary<ParamName, decimal> parameters);
}

public sealed class TasksUserComputationServiceWasm : ITasksUserComputationServiceWasm
{
    private static string SharedText(string key, string fallback)
        => ResLocalize.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;

    public void CalcQuantityResource(List<ResourceDto> resources, decimal? taskQuantity)
    {
        taskQuantity ??= 0m;

        foreach (var resource in resources)
        {
            ApplyResourceParameterFactor(resource.Data);
            var baseCalc = taskQuantity.Value * resource.Data.ChangeFactor1 * resource.Data.ChangeFactor2;

            if (resource.HasWast && resource.Data.CapWaste != 0)
                resource.Quantity = baseCalc * (1m + resource.Data.CapWaste / 100m);
            else if (resource.HasCap && resource.Data.CapWaste != 0)
                resource.Quantity = baseCalc / resource.Data.CapWaste;
            else
                resource.Quantity = baseCalc;

            ApplyResourceTimedCost(resource.Data, resource.Quantity);
        }
    }

    public void ReCalcCostResources(List<ResourceDto> resources)
    {
        foreach (var res in resources)
        {
            if (!res.Quantity.HasValue) continue;
            var q = res.Quantity.Value;
            foreach (var item in res.CostRole)
                if (q >= item.Min && q <= item.Max && item.Value.HasValue)
                {
                    res.Data.Cost = item.Value.Value;
                    break;
                }
        }
    }

    public void ReCalcCapResources(List<ResourceDto> resources)
    {
        foreach (var res in resources)
        {
            if (!res.HasCap || !res.Quantity.HasValue)
                continue;
            var q = res.Quantity.Value;
            foreach (var item in res.CapRole)
                if (q >= item.Min && q <= item.Max && item.Value.HasValue)
                {
                    res.Data.CapWaste = item.Value.Value;
                    break;
                }
        }
    }

    public void RefreshResources(List<ResourceDto> resources, decimal? taskQuantity, IReadOnlyDictionary<ParamName, decimal>? taskParameters)
    {
        if (resources is null || resources.Count == 0)
            return;

        RefreshDerivedValues(resources, taskQuantity);
        ApplyFormulaDrivenRecalculation(resources, taskQuantity, taskParameters);
    }

    public decimal ComputeOrThrow(string toUnit, string fromUnit, decimal quantity, IReadOnlyDictionary<ParamName, decimal> parameters)
    {
        if (string.IsNullOrWhiteSpace(toUnit) || string.IsNullOrWhiteSpace(fromUnit))
            throw new ArgumentException(SharedText("SelectUnitsFirst", "Select the conversion units first."));
        if (quantity <= 0)
            throw new ArgumentException(SharedText("InvalidQuantity", "Invalid quantity."));

        if (string.Equals(
            UnitRulesCatalog.BuildKey(toUnit),
            UnitRulesCatalog.BuildKey(fromUnit),
            StringComparison.OrdinalIgnoreCase))
        {
            return quantity;
        }

        if (!UnitRulesCatalog.TryGet(toUnit, fromUnit, out var rule))
            throw new ArgumentException(string.Format(
                CultureInfo.CurrentCulture,
                SharedText("MissingUnitConversionRule", "No conversion rule exists from {0} to {1}."),
                fromUnit,
                toUnit));

        var missing = rule.Params.Select(p => p.Key).Where(k => !parameters.ContainsKey(k)).ToList();
        if (missing.Count > 0)
            throw new ArgumentException(string.Format(
                CultureInfo.CurrentCulture,
                SharedText("MissingValues", "Missing values: {0}"),
                string.Join(", ", missing)));

        return rule.Compute(quantity, parameters);
    }

    public bool BuildFinalRows(ProjectTaskDto task)
    {
        task.ResultResources = [];
        return true;
    }

    private void RefreshDerivedValues(List<ResourceDto> resources, decimal? taskQuantity)
    {
        CalcQuantityResource(resources, taskQuantity);
        ReCalcCapResources(resources);
        CalcQuantityResource(resources, taskQuantity);
        ReCalcCostResources(resources);
    }

    private void ApplyFormulaDrivenRecalculation(List<ResourceDto> resources, decimal? taskQuantity, IReadOnlyDictionary<ParamName, decimal>? taskParameters)
    {
        if (!resources.Any(r => r.Formulas?.Count > 0))
            return;

        ResourceFormulaApplier.ApplyAll(resources, taskParameters);

        foreach (var resource in resources)
        {
            var targets = ResourceFormulaApplier.GetAssignedTargets(resource.Formulas);
            if (targets.Count == 0)
                continue;

            RefreshFormulaDrivenResource(resource, taskQuantity, targets);
        }

        ResourceFormulaApplier.ApplyAll(resources, taskParameters);
    }

    private void RefreshFormulaDrivenResource(ResourceDto resource, decimal? taskQuantity, IReadOnlySet<string> targets)
    {
        var singleResource = new List<ResourceDto> { resource };
        var assignsQuantity = targets.Contains("quantity");
        var assignsCapWaste = targets.Contains("cap") || targets.Contains("waste") || targets.Contains("capwaste");
        var assignsChangeFactor = targets.Contains("ch1") || targets.Contains("ch2");

        if (!assignsQuantity && (assignsChangeFactor || assignsCapWaste))
            CalcQuantityResource(singleResource, taskQuantity);

        if (!assignsCapWaste && (assignsQuantity || assignsChangeFactor))
        {
            ReCalcCapResources(singleResource);

            if (!assignsQuantity)
                CalcQuantityResource(singleResource, taskQuantity);
        }

        if (!targets.Contains("cost") && (assignsQuantity || assignsChangeFactor || assignsCapWaste))
            ReCalcCostResources(singleResource);
    }

    private static void ApplyResourceParameterFactor(ResourceMetadata data)
    {
        var parameters = data.Parameters;
        if (parameters is null || parameters.Count == 0)
            return;

        data.ChangeFactor1 = Math.Round(
            parameters.Aggregate(1m, (acc, p) => acc * p.Value),
            4, MidpointRounding.AwayFromZero);
    }

    private static void ApplyResourceTimedCost(ResourceMetadata data, decimal? quantity)
    {
        data.SyncTimesWithQuantity(quantity);
        var times = data.Times;
        if (times is null || times.Count == 0)
            return;

        var resolvedQuantity = quantity ?? 0m;
        if (resolvedQuantity <= 0m)
        {
            data.Cost = 0m;
            return;
        }

        data.Cost = Math.Round(
            times.Sum(t => t.Quantity * t.Cost) / resolvedQuantity,
            2, MidpointRounding.AwayFromZero);
    }

    public bool EvaluateVariables(ProjectTaskDto task, TaskConditionDto cond)
        => cond.VariableRequirements.Count == 0 || cond.VariableRequirements
            .GroupBy(r => r.SetKey)
            .All(set => set.Any(r => EvaluateSingleVariable(task, r)));

    private static bool EvaluateSingleVariable(ProjectTaskDto task, ConditionVariableRequirementDto rule)
    {
        var variableValue = GetVariableValue(task, rule.VariableName);
        if (!variableValue.HasValue)
            return false;

        return (!rule.MaxAllowedValue.HasValue || variableValue.Value <= rule.MaxAllowedValue.Value)
            && (!rule.MinAllowedValue.HasValue || variableValue.Value >= rule.MinAllowedValue.Value);
    }

    private static decimal? GetVariableValue(ProjectTaskDto task, string? variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
            return null;

        return variableName.Trim().ToLowerInvariant() switch
        {
            "thickness" => task.ParameterValues.TryGetValue(ParamName.Thickness, out var t) ? t : null,
            "width" => task.ParameterValues.TryGetValue(ParamName.Width, out var w) ? w : null,
            "length" => task.ParameterValues.TryGetValue(ParamName.Length, out var l) ? l : null,
            "density" => task.ParameterValues.TryGetValue(ParamName.Density, out var d) ? d : null,
            "quantity" => task.Quantity,
            "cost" => task.BaseResources.Sum(r => r.Data.Cost),
            "basecost" => task.BaseResources.Sum(r => r.Data.BaseCost.GetValueOrDefault()),
            "priceproduction" or "price production" => task.PriceProduction,
            _ => null,
        };
    }
}
