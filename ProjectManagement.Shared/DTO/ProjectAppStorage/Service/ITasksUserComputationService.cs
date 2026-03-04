using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.ProjectAppStorage;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace ProjectManagement.Shared.DTO.ProjectAppStorage.Service;

public class ResourceMenuGroup
{
    public int MenuId { get; set; }
    public List<ResourceDto> Resources { get; set; } = new();
    public int SelectedResourceId { get; set; }
    // فقط لمساعدة الريندر حتى لا نكرر الصف
    public bool Rendered { get; set; }
}

public sealed class UserAnswers
{
    public HashSet<int> SelectedChoiceOptionIds { get; } = [];
    public HashSet<int> SelectedResourceChoiceItemIds { get; } = [];
    public Dictionary<int, double?> NumericValuesByGroupId { get; } = [];
}
public interface ITasksUserComputationServiceWasm
{
    UserAnswers A(int taskId);
    void ReCalcCostResources(List<ResourceDto> resources);
    void ReCalcCapResources(List<ResourceDto> resources);
    void CalcQuantityResource(List<ResourceDto> resource, double? taskQuantity);
    bool BuildFinalRows(ProjectTaskDto task);
    bool Combine(bool a, bool b, ConditionLogic op);
    double ComputeOrThrow(string toUnit, string fromUnit, double quantity, IReadOnlyDictionary<ParamName, double> parameters);
}
public sealed class TasksUserComputationServiceWasm : ITasksUserComputationServiceWasm
{
    private readonly Dictionary<int, UserAnswers> _answers = [];
    public void CalcQuantityResource(List<ResourceDto> resources, double? taskQuantity)
    {
        if(!taskQuantity.HasValue) taskQuantity = 0;
        foreach (var resource in resources)
        {
            var baseCalc = (decimal)taskQuantity.Value * resource.Data.ChangeFactor1 * resource.Data.ChangeFactor2;

            if (resource.HasWast && resource.Data.CapWaste != 0)
                resource.Data.Quantity = baseCalc * (1m + resource.Data.CapWaste / 100m);
            else if (resource.HasCap && resource.Data.CapWaste != 0)
                resource.Data.Quantity = baseCalc / resource.Data.CapWaste;
            else resource.Data.Quantity = baseCalc;

        }
    }
    public void ReCalcCostResources(List<ResourceDto> resources)
    {
        foreach (var res in resources)
        {
            if (res.CostRole != null && res.Data.Quantity.HasValue)
                foreach (var item in res.CostRole)
                    if ((double)res.Data.Quantity.Value >= item.Min && (double)res.Data.Quantity.Value <= item.Max && item.Value.HasValue)
                    {
                        res.Data.Cost = (decimal)item.Value.Value;
                        break;
                    }
        }
    }
    public void ReCalcCapResources(List<ResourceDto> resources)
    {
        foreach (var res in resources)
            if ((res.ResType == ResourceTypesEnum.MachinesAndEquipments || res.ResType == ResourceTypesEnum.Worker)
                && res.CapRole != null && res.Data.Quantity.HasValue)
                foreach (var item in res.CapRole)
                    if ((double)res.Data.Quantity.Value >= item.Min && (double)res.Data.Quantity.Value <= item.Max && item.Value.HasValue)
                    {
                        res.Data.CapWaste = (decimal)item.Value.Value;
                        break;
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

    public double ComputeOrThrow(string toUnit, string fromUnit, double quantity, IReadOnlyDictionary<ParamName, double> parameters)
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
        foreach (var cond in task.Conditions ?? Enumerable.Empty<TaskConditionDto>())
        {
            var choiceOk = EvaluateChoices(cond, ans);
            var resOk = EvaluateResources(cond, ans);
            var numOk = EvaluateNumeric(cond, ans);

            if (cond.OptionRequirements.Count == 0) choiceOk = true;
            if (cond.ResourceRequirements.Count == 0) resOk = true;
            if (cond.NumericRequirements.Count == 0) numOk = true;

            bool cr = Combine(choiceOk, resOk, cond.OptionToResourceLogic);
            bool cn = Combine(choiceOk, numOk, cond.OptionToNumericLogic);
            bool nr = Combine(numOk, resOk, cond.NumericToResourceLogic);

            if (!(cr && cn && nr)) continue;

            foreach (var ra in cond.ConditionResourceAssignments ?? Enumerable.Empty<ResourceAssignmentDto>())
            {
                if (ra.Resource == null) continue;
                ra.Formulas = [];
                var resName = ra.Resource.Name;
                decimal cawaste = ra.Resource.Data.CapWaste;
                // حساب Cap/Waste بحسب الأدوار إن لزم
                if (ra.Resource.ResType == ResourceTypesEnum.MachinesAndEquipments ||
                    ra.Resource.ResType == ResourceTypesEnum.Worker)
                {
                    foreach (var item in ra.CapRole ?? Enumerable.Empty<RoleDTO>())
                    {
                        var q = ra.Resource.Data.Quantity;
                        if (q.HasValue && (double)q.Value >= item.Min && (double)q.Value <= item.Max && item.Value.HasValue)
                            cawaste = (decimal)item.Value.Value;
                    }
                }

                // تجميع الصيغ من الاختيارات والأرقام
                GetOptionFormulas(ra, ans.SelectedChoiceOptionIds);
                GetNumericFormulas(ra, ans.NumericValuesByGroupId);
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
                    Data = new Base.Calculation.ResourceMetadata()
                    {
                        CapWaste = cawaste,
                        ChangeFactor1 = ra.Resource.Data.ChangeFactor1,
                        ChangeFactor2 = ra.Resource.Data.ChangeFactor2,
                        BaseCost = ra.Resource.Data.BaseCost,
                        Unit = ra.Resource.Data.Unit,
                        Cost = ra.Resource.Data.Cost,
                        Quantity = ra.Resource.Data.Quantity,
                        Note = ra.Resource.Data.Note,
                        UpperNote = ra.Resource.Data.UpperNote,
                        CO2 = ra.Resource.Data.CO2,
                    },
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
    public bool Combine(bool a, bool b, ConditionLogic op)
        => op == ConditionLogic.And ? a && b : a || b;
    public void GetOptionFormulas(ResourceAssignmentDto res, HashSet<int> SelectedChoiceOptionIds)
    {
        List<string> Formulas = [];

        if (SelectedChoiceOptionIds == null || SelectedChoiceOptionIds.Count == 0) return;
        foreach (var bind in res.OptionResourceFormulas)
        {
            if (SelectedChoiceOptionIds.Any(x => x == bind.ChoiceOptionId))
            {
                Formulas.AddRange(bind.Formulas);
            }
        }
        res.Formulas ??= [];
        res.Formulas.AddRange(Formulas);
    }
    public void GetNumericFormulas(ResourceAssignmentDto res, Dictionary<int, double?> numericByGroup)
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

                    // إبقاء نفس المقارنة كما في صفحتك
                    return (!r.MaxAllowedValue.HasValue || val.Value <= r.MaxAllowedValue.Value)
                        && (!r.MinAllowedValue.HasValue || val.Value >= r.MinAllowedValue.Value);
                })
           );
}
