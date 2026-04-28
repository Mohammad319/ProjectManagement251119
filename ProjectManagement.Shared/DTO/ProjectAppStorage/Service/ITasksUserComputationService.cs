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
    UserAnswers A(int taskId);
    void ReCalcCostResources(List<ResourceDto> resources);
    void ReCalcCapResources(List<ResourceDto> resources);
    void CalcQuantityResource(List<ResourceDto> resource, decimal? taskQuantity);
    void RefreshResources(List<ResourceDto> resources, decimal? taskQuantity, IReadOnlyDictionary<ParamName, decimal>? taskParameters);
    bool BuildFinalRows(ProjectTaskDto task);
    bool Combine(bool a, bool b, ConditionLogic op);
    decimal ComputeOrThrow(string toUnit, string fromUnit, decimal quantity, IReadOnlyDictionary<ParamName, decimal> parameters);
}

public sealed class TasksUserComputationServiceWasm : ITasksUserComputationServiceWasm
{
    private readonly Dictionary<int, UserAnswers> _answers = [];

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
                resource.Data.Quantity = baseCalc * (1m + resource.Data.CapWaste / 100m);
            else if (resource.HasCap && resource.Data.CapWaste != 0)
                resource.Data.Quantity = baseCalc / resource.Data.CapWaste;
            else
                resource.Data.Quantity = baseCalc;

            ApplyResourceTimedCost(resource.Data);
        }
    }

    public void ReCalcCostResources(List<ResourceDto> resources)
    {
        foreach (var res in resources)
        {
            if (!res.Data.Quantity.HasValue) continue;

            foreach (var item in res.CostRole)
                if (res.Data.Quantity.Value >= item.Min && res.Data.Quantity.Value <= item.Max && item.Value.HasValue)
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
            if (!((res.ResType == ResourceTypesEnum.MachinesAndEquipments || res.ResType == ResourceTypesEnum.Worker)
                && res.Data.Quantity.HasValue))
                continue;

            foreach (var item in res.CapRole)
                if (res.Data.Quantity.Value >= item.Min && res.Data.Quantity.Value <= item.Max && item.Value.HasValue)
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

    public UserAnswers A(int taskId)
    {
        if (!_answers.TryGetValue(taskId, out var a))
        {
            a = new UserAnswers();
            _answers[taskId] = a;
        }
        return a;
    }

    public decimal ComputeOrThrow(string toUnit, string fromUnit, decimal quantity, IReadOnlyDictionary<ParamName, decimal> parameters)
    {
        if (string.IsNullOrWhiteSpace(toUnit) || string.IsNullOrWhiteSpace(fromUnit))
            throw new ArgumentException(SharedText("SelectUnitsFirst", "Select the conversion units first."));
        if (quantity <= 0)
            throw new ArgumentException(SharedText("InvalidQuantity", "Invalid quantity."));

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

    private static void ApplyUserEdits(ResourceDto source, ResourceDto target)
    {
        target.IsAdded = source.IsAdded;
        target.Name = source.Name;
        target.NameUserValue = source.NameUserValue;
        target.ResourceSortId = source.ResourceSortId;
        target.ResourceTypeId = source.ResourceTypeId;
        target.AccountId = source.AccountId;
        target.StatusId = source.StatusId;
        target.Active = source.Active;

        target.Data.Unit = source.Data.Unit;
        target.Data.Note = source.Data.Note;
        target.Data.UpperNote = source.Data.UpperNote is null ? [] : [.. source.Data.UpperNote];
        target.Data.PriceSub = source.Data.PriceSub;
        target.Data.ChangeFactor1 = source.Data.ChangeFactor1;
        target.Data.ChangeFactor2 = source.Data.ChangeFactor2;
        target.Data.CapWaste = source.Data.CapWaste;
        target.Data.Cap = source.Data.Cap;
        target.Data.Waste = source.Data.Waste;
        target.Data.Cost = source.Data.Cost;
        target.Data.BaseCost = source.Data.BaseCost;
        target.Data.CO2 = source.Data.CO2;

        target.Properties = MergeProperties(target.Properties, source.Properties);
    }

    private static List<ResourcePropertyBindDto> MergeProperties(
        IEnumerable<ResourcePropertyBindDto>? templateProperties,
        IEnumerable<ResourcePropertyBindDto>? editedProperties)
    {
        var editedById = (editedProperties ?? Enumerable.Empty<ResourcePropertyBindDto>())
            .ToDictionary(p => p.Id);

        var merged = new List<ResourcePropertyBindDto>();
        foreach (var property in templateProperties ?? Enumerable.Empty<ResourcePropertyBindDto>())
        {
            var clone = CloneProperty(property);
            if (editedById.TryGetValue(property.Id, out var edited))
            {
                clone.TextDefault = edited.TextDefault;
                clone.NumberDefault = edited.NumberDefault;
            }

            merged.Add(clone);
        }

        return merged;
    }

    private static List<ResourcePropertyBindDto> CloneProperties(IEnumerable<ResourcePropertyBindDto>? properties)
        => (properties ?? Enumerable.Empty<ResourcePropertyBindDto>())
            .Select(CloneProperty)
            .ToList();

    private static ResourcePropertyBindDto CloneProperty(ResourcePropertyBindDto property)
        => new()
        {
            Id = property.Id,
            DisplayName = property.DisplayName,
            IsUserEditable = property.IsUserEditable,
            DataType = property.DataType,
            MaxNumericValue = property.MaxNumericValue,
            TextDefault = property.TextDefault,
            NumberDefault = property.NumberDefault
        };

    private void RefreshDerivedValues(List<ResourceDto> resources, decimal? taskQuantity)
    {
        CalcQuantityResource(resources, taskQuantity);
        ReCalcCapResources(resources);
        CalcQuantityResource(resources, taskQuantity);
        ReCalcCostResources(resources);
    }

    private void ApplyFormulaDrivenRecalculation(List<ResourceDto> resources, decimal? taskQuantity, IReadOnlyDictionary<ParamName, decimal>? taskParameters)
    {
        if (resources.All(r => r.Formulas is null || r.Formulas.Count == 0))
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
        var assignsCost = targets.Contains("cost");
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

        if (!assignsCost && (assignsQuantity || assignsChangeFactor || assignsCapWaste))
            ReCalcCostResources(singleResource);
    }

    private static void ApplyResourceParameterFactor(ResourceMetadata data)
    {
        var parameters = data.Parameters;
        if (parameters is null || parameters.Count == 0)
            return;

        decimal product = 1m;
        for (int i = 0; i < parameters.Count; i++)
            product *= parameters[i].Value;

        data.ChangeFactor1 = Math.Round(product, 4, MidpointRounding.AwayFromZero);
    }

    private static void ApplyResourceTimedCost(ResourceMetadata data)
    {
        data.SyncTimesWithQuantity();
        var times = data.Times;
        if (times is null || times.Count == 0)
            return;

        var quantity = data.Quantity ?? 0m;
        if (quantity <= 0m)
        {
            data.Cost = 0m;
            return;
        }

        decimal total = 0m;
        for (int i = 0; i < times.Count; i++)
            total += times[i].Quantity * times[i].Cost;

        data.Cost = Math.Round(total / quantity, 2, MidpointRounding.AwayFromZero);
    }

    public bool Combine(bool a, bool b, ConditionLogic op)
        => op == ConditionLogic.And ? a && b : a || b;

    public void GetOptionFormulas(ResourceAssignmentDto res, HashSet<int> SelectedChoiceOptionIds)
    {
        if (SelectedChoiceOptionIds.Count == 0) return;

        List<string> formulas = [];
        foreach (var bind in res.OptionResourceFormulas)
            if (SelectedChoiceOptionIds.Contains(bind.ChoiceOptionId))
                formulas.AddRange(bind.Formulas);

        res.Formulas ??= [];
        res.Formulas.AddRange(formulas);
    }

    public void GetNumericFormulas(ResourceAssignmentDto res, Dictionary<int, decimal?> numericByGroup)
    {
        if (numericByGroup is null || numericByGroup.Count == 0) return;
        if (res.NumericResourceFormulas is null || res.NumericResourceFormulas.Count == 0) return;

        List<string> formulas = [];

        foreach (var bind in res.NumericResourceFormulas)
        {
            if (!numericByGroup.TryGetValue(bind.NumericId, out var v) || !v.HasValue)
                continue;

            bool geMin = !bind.MinInputValue.HasValue || v.Value >= bind.MinInputValue.Value;
            bool leMax = !bind.MaxInputValue.HasValue || v.Value <= bind.MaxInputValue.Value;

            if (geMin && leMax)
                formulas.AddRange(bind.Formulas ?? Enumerable.Empty<string>());
        }

        res.Formulas ??= [];
        res.Formulas.AddRange(formulas);
    }

    public bool EvaluateChoices(TaskConditionDto cond, UserAnswers ans)
        => cond.OptionRequirements.Count == 0 || cond.OptionRequirements
           .GroupBy(r => r.SetKey)
           .All(set => set.Any(r => ans.SelectedChoiceOptionIds.Contains(r.OptionItemId)));

    public bool EvaluateResources(TaskConditionDto cond, UserAnswers ans)
        => cond.ResourceRequirements.Count == 0 || cond.ResourceRequirements
           .GroupBy(r => r.SetKey)
           .All(set => set.Any(r => ans.SelectedResourceChoiceItemIds.Contains(r.ResourceOptionItemId)));

    public bool EvaluateNumeric(TaskConditionDto cond, UserAnswers ans)
        => cond.NumericRequirements.Count == 0 || cond.NumericRequirements
           .GroupBy(r => r.SetKey)
           .All(set =>
                set.Any(r =>
                {
                    if (!ans.NumericValuesByGroupId.TryGetValue(r.NumericInputId, out var val) || val is null)
                        return false;

                    return (!r.MaxAllowedValue.HasValue || val.Value <= r.MaxAllowedValue.Value)
                        && (!r.MinAllowedValue.HasValue || val.Value >= r.MinAllowedValue.Value);
                })
           );

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

        switch (variableName.Trim().ToLowerInvariant())
        {
            case "thickness":
                return task.ParameterValues.TryGetValue(ParamName.Thickness, out var thickness)
                    ? thickness
                    : null;

            case "width":
                return task.ParameterValues.TryGetValue(ParamName.Width, out var width)
                    ? width
                    : null;

            case "length":
                return task.ParameterValues.TryGetValue(ParamName.Length, out var length)
                    ? length
                    : null;

            case "density":
                return task.ParameterValues.TryGetValue(ParamName.Density, out var density)
                    ? density
                    : null;

            case "quantity":
                return task.Quantity;

            case "cost":
                return task.BaseResources.Sum(r => r.Data.Cost);

            case "basecost":
                return task.BaseResources.Sum(r => r.Data.BaseCost.GetValueOrDefault());

            case "priceproduction":
            case "price production":
                return task.PriceProduction;

            default:
                return null;
        }
    }
}
