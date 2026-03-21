using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Base.ProjectAppStorage;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper.ProjectAppStorage;

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
    bool BuildFinalRows(ProjectTaskDto task);
    bool Combine(bool a, bool b, ConditionLogic op);
    decimal ComputeOrThrow(string toUnit, string fromUnit, decimal quantity, IReadOnlyDictionary<ParamName, decimal> parameters);
}

public sealed class TasksUserComputationServiceWasm : ITasksUserComputationServiceWasm
{
    private readonly Dictionary<int, UserAnswers> _answers = [];

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
            throw new ArgumentException("اختر وحدتي التحويل أولاً.");
        if (quantity <= 0)
            throw new ArgumentException("الكمية غير صالحة.");

        if (!UnitRulesCatalog.TryGet(toUnit, fromUnit, out var rule))
            throw new ArgumentException($"لا توجد قاعدة تحويل من {fromUnit} إلى {toUnit}.");

        var missing = rule.Params.Select(p => p.Key).Where(k => !parameters.ContainsKey(k)).ToList();
        if (missing.Count > 0)
            throw new ArgumentException($"القيم الناقصة: {string.Join(", ", missing)}");

        return rule.Compute(quantity, parameters);
    }

    public bool BuildFinalRows(ProjectTaskDto task)
    {
        task.ResultResources = [];

        var ans = A(task.Id);
        foreach (var cond in task.Conditions)
        {
            var choiceOk = EvaluateChoices(cond, ans);
            var resOk = EvaluateResources(cond, ans);
            var numOk = EvaluateNumeric(cond, ans);
            var varOk = EvaluateVariables(task, cond);

            if (cond.OptionRequirements.Count == 0) choiceOk = true;
            if (cond.ResourceRequirements.Count == 0) resOk = true;
            if (cond.NumericRequirements.Count == 0) numOk = true;
            if (cond.VariableRequirements.Count == 0) varOk = true;

            bool cr = Combine(choiceOk, resOk, cond.OptionToResourceLogic);
            bool cn = Combine(choiceOk, numOk, cond.OptionToNumericLogic);
            bool nr = Combine(numOk, resOk, cond.NumericToResourceLogic);

            if (!(cr && cn && nr && varOk))
                continue;

            foreach (var ra in cond.ConditionResourceAssignments)
            {
                if (ra.Resource == null) continue;

                ra.Formulas = [];
                var resName = ra.Resource.Name;

                decimal cawaste = ra.Resource.Data.CapWaste;

                if (ra.Resource.ResType == ResourceTypesEnum.MachinesAndEquipments ||
                    ra.Resource.ResType == ResourceTypesEnum.Worker)
                {
                    foreach (var item in ra.CapRole)
                    {
                        var q = ra.Resource.Data.Quantity;
                        if (q.HasValue && q.Value >= item.Min && q.Value <= item.Max && item.Value.HasValue)
                            cawaste = item.Value.Value;
                    }
                }

                GetOptionFormulas(ra, ans.SelectedChoiceOptionIds);
                GetNumericFormulas(ra, ans.NumericValuesByGroupId);

                var resourceData = ra.Resource.Data.Clone();
                resourceData.CapWaste = cawaste;

                ResourceDto res = new()
                {
                    Id = ra.Resource.Id,
                    Name = resName,
                    ResourceSource = ResourceSource.FromCodition,
                    NameUserValue = ra.Resource.NameUserValue,
                    ResourceSortId = ra.Resource.ResourceSortId,
                    ResourceTypeId = ra.Resource.ResourceTypeId,
                    AccountId = ra.Resource.AccountId,
                    StatusId = ra.Resource.StatusId,
                    CalcResCost = ra.Resource.CalcResCost,
                    MenuId = ra.Resource.MenuId,
                    Data = resourceData,
                    ResType = ra.Resource.ResType,
                    Active = ra.Resource.Active,
                    Formulas = ra.Formulas,
                    Properties = ra.Resource.Properties,
                };

                task.ResultResources.Add(res);
            }
        }

        ReCalcCostResources(task.ResultResources);
        ReCalcCapResources(task.ResultResources);
        CalcQuantityResource(task.ResultResources, task.Quantity);

        ResourceFormulaApplier.ApplyAll(task.ResultResources, task.ParameterValues);
        return true;
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

    private static double? GetVariableValue(ProjectTaskDto task, string? variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
            return null;

        switch (variableName.Trim().ToLowerInvariant())
        {
            case "thickness":
                return task.ParameterValues.TryGetValue(ParamName.Thickness, out var thickness)
                    ? (double)thickness
                    : null;

            case "width":
                return task.ParameterValues.TryGetValue(ParamName.Width, out var width)
                    ? (double)width
                    : null;

            case "length":
                return task.ParameterValues.TryGetValue(ParamName.Length, out var length)
                    ? (double)length
                    : null;

            case "density":
                return task.ParameterValues.TryGetValue(ParamName.Density, out var density)
                    ? (double)density
                    : null;

            case "quantity":
                return task.Quantity.HasValue ? (double)task.Quantity.Value : null;

            case "cost":
                return (double)task.BaseResources.Sum(r => r.Data.Cost);

            case "basecost":
                return (double)task.BaseResources.Sum(r => r.Data.BaseCost.GetValueOrDefault());

            default:
                return null;
        }
    }
}
