using System.Runtime.CompilerServices;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Client.Extensions.CalcultationItemsOperation
{
    public static class CalcultationExtensions
    {
        // مفتاح سريع لتجميع عوامل الموارد (Factor lookup)
        // SortId عندك غالبًا nullable لذلك نخليه int?
        private readonly record struct FactorKey(int? SortId, ResourceTypesEnum ResourceType);

        // =============================
        // ExecuteCalculation (optimized)
        // =============================
        public static void ExecuteCalculation(this CalculationMVVM calc)
        {
            if (calc is null) return;

            calc.Factors ??= [];
            calc.QuanityList ??= [];

            InitializeCalculationOptimized(calc);
            ApplyFactorF(calc.Factors);
            AssignFactorsToResourcesOptimized(calc);
            FilterFactorsInPlace(calc);

            var sum = calc.Sum;
            if (sum != 0)
                calc.ProfitDecision = calc.Factors.Sum(x => x.Sum * x.Earnings) / sum;
            else
                calc.ProfitDecision = 0;
        }

        // =========================================================
        // InitializeCalculationOptimized
        // =========================================================
        private static void InitializeCalculationOptimized(CalculationMVVM calc)
        {
            var tasks = calc.Tasks;
            if (tasks is null || tasks.Count == 0)
                return;

            var quanityList = calc.QuanityList;

            // 1) CalcVaribles للـ root tasks فقط
            for (int i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                if (!t.TaskId.HasValue)
                    t.CalcVaribles(quanityList, null);
            }

            var factors = calc.Factors;

            // index سريع (SortId + ResourceType)
            var factorIndex = BuildFactorIndex(factors);

            // 2) مرّ على كل Task فيه Resources
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];

                if (task.Metadata?.Type == TaskType.CodeName)
                    continue;

                var resList = task.Resources;
                if (resList is null || resList.Count == 0)
                    continue;

                var taskQty = task.Metadata?.Quantity;
                var taskCap = task.Metadata?.Cap;
                var taskIsOH = task.Metadata?.IsOH == true;

                for (int r = 0; r < resList.Count; r++)
                {
                    var res = resList[r];

                    res.CalcVaribles(quanityList, taskQty, taskCap);

                    if (!res.Active)
                        continue;

                    // حاول تطابق دقيق مثل منطقك الأصلي
                    if (TryGetExactFactor(factors, res, out var existing) && existing is not null)
                    {
                        if (res.ResName != existing.ResName || res.Sort != existing.Sort)
                        {
                            existing.ResName = res.ResName;
                            existing.Sort = res.Sort;
                        }

                        existing.AddResValue(taskIsOH, res.NetCostTotaly);
                    }
                    else
                    {
                        // إنشاء عامل جديد
                        var newFactor = Factors.AddNewFactor(taskIsOH, res);
                        factors.Add(newFactor);

                        // تحديث index
                        var key = new FactorKey(newFactor.SortId, newFactor.ResourceType);
                        factorIndex[key] = newFactor;
                    }
                }
            }
        }

        // =========================================================
        // CalcEarningsForUnlockedRes (optimized)
        // =========================================================
        public static void CalcEarningsForUnlockedRes(this CalculationMVVM calc)
        {
            if (calc is null) return;

            var factors = calc.Factors;
            if (factors is null || factors.Count == 0)
                return;

            double lockedSumWeighted = 0;
            double unlockedSum = 0;

            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                if (f.IsLocked)
                    lockedSumWeighted += (f.Sum * f.Earnings);
                else
                    unlockedSum += f.Sum;
            }

            if (unlockedSum == 0)
                return;

            double e = ((calc.ProfitDecision * calc.Sum) - lockedSumWeighted) / unlockedSum;

            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                if (!f.IsLocked && f.EarningsValue > 0)
                    f.Earnings = e;
            }

            AssignFactorsToResourcesOptimized(calc);
        }

        // =========================================================
        // AssignFactorsToResourcesOptimized
        // - SortId nullable
        // - ResourceType = ResourceTypesEnum
        // =========================================================
        public static void AssignFactorsToResourcesOptimized(this CalculationMVVM calc)
        {
            if (calc is null) return;

            var tasks = calc.Tasks;
            var factors = calc.Factors;

            if (tasks is null || factors is null || factors.Count == 0)
                return;

            var factorIndex = BuildFactorIndex(factors);

            for (int i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];

                if (t?.Metadata?.Quantity.HasValue != true)
                    continue;

                var resList = t.Resources;
                if (resList is null || resList.Count == 0)
                    continue;

                for (int r = 0; r < resList.Count; r++)
                {
                    var res = resList[r];
                    var key = new FactorKey(res.ResourceSortId, res.ResType);

                    if (factorIndex.TryGetValue(key, out var factor) && factor is not null)
                    {
                        res.Factor = factor.Factor;
                    }
                }
            }
        }

        // =========================================================
        // Apply FactorF
        // =========================================================
        private static void ApplyFactorF(List<Factors> factors)
        {
            if (factors is null || factors.Count == 0)
                return;

            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                if (f.NetCostTotaly > 0)
                    f.FactorF(factors);
            }
        }

        // =========================================================
        // FilterFactorsInPlace
        // =========================================================
        private static void FilterFactorsInPlace(CalculationMVVM calc)
        {
            var factors = calc.Factors;
            if (factors is null || factors.Count == 0)
                return;

            int write = 0;
            for (int read = 0; read < factors.Count; read++)
            {
                var f = factors[read];
                if (f.NetCostTotaly > 0 || f.NetCostTotalyOH > 0)
                    factors[write++] = f;
            }

            if (write < factors.Count)
                factors.RemoveRange(write, factors.Count - write);
        }

        // =========================================================
        // BuildFactorIndex: (SortId + ResourceType)
        // SortId nullable
        // ResourceType = ResourceTypesEnum
        // =========================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<FactorKey, Factors> BuildFactorIndex(List<Factors> factors)
        {
            var dict = new Dictionary<FactorKey, Factors>(Math.Max(16, factors.Count * 2));
            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                var key = new FactorKey(f.SortId, f.ResourceType);
                dict[key] = f;
            }
            return dict;
        }

        // =========================================================
        // TryGetExactFactor (يحافظ على منطقك الأصلي)
        // - SortId nullable
        // - ResId nullable غالباً
        // - ResourceType = ResourceTypesEnum
        // =========================================================
        private static bool TryGetExactFactor(List<Factors> factors, ResourceListMVVM res, out Factors? factor)
        {
            factor = null;
            if (factors is null || factors.Count == 0)
                return false;

            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];

                if (f.SortId == res.ResourceSortId &&
                    f.ResId == res.ResourceTypeId &&
                    f.ResourceType.Equals(res.ResType))
                {
                    factor = f;
                    return true;
                }
            }

            return false;
        }
    }
}
