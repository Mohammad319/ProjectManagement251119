using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using System.Runtime.CompilerServices;

namespace ProjectManagement.Client.Extensions.CalcultationItemsOperation
{
    public static class CalcultationExtensions
    {
        // مفتاح تجميع/بحث عوامل الموارد (Factor lookup)
        private readonly record struct FactorKey(int? ResId, int? SortId, ResourceTypesEnum ResourceType);
        private static void ComputeTaskAggregates(CalculationMVVM calc)
        {
            var roots = calc.RootTasks;
            if (roots is null || roots.Count == 0) return;

            // Post-order بدون recursion (stackين)
            var s1 = new Stack<TaskListMVVM>(roots.Count);
            var s2 = new Stack<TaskListMVVM>(roots.Count);

            for (int i = 0; i < roots.Count; i++)
                s1.Push(roots[i]);

            while (s1.Count > 0)
            {
                var t = s1.Pop();
                s2.Push(t);

                var children = t.Tasks;
                if (children is null) continue;

                for (int i = 0; i < children.Count; i++)
                    s1.Push(children[i]);
            }

            while (s2.Count > 0)
            {
                var t = s2.Pop();

                decimal netQ = 0;
                decimal netTot = 0;
                decimal apriceTot = 0;

                // nullable totals
                double totalCo2 = 0;
                bool hasCo2 = false;

                decimal baseCost = 0;
                bool hasBaseCost = false;

                // Children (active فقط)
                var children = t.Tasks;
                if (children is not null)
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        var c = children[i];
                        if (!c.Active) continue;

                        netQ += c.GetComputedNetCostQ();
                        netTot += c.GetComputedNetCostTotaly();
                        apriceTot += c.GetComputedApriceTotally();

                        var childTotalCo2 = c.GetComputedTotalCO2();
                        if (childTotalCo2.HasValue)
                        {
                            totalCo2 += childTotalCo2.Value;
                            hasCo2 = true;
                        }

                        var childBaseCost = c.GetComputedBaseCost();
                        if (childBaseCost.HasValue)
                        {
                            baseCost += childBaseCost.Value;
                            hasBaseCost = true;
                        }
                    }
                }

                // Resources (active فقط)
                var res = t.Resources;
                if (res is not null)
                {
                    for (int i = 0; i < res.Count; i++)
                    {
                        var r = res[i];
                        if (!r.Active) continue;

                        netQ += r.GetComputedNetCostQ();
                        netTot += r.GetComputedNetCostTotaly();
                        apriceTot += r.GetComputedApriceTotally();

                        var resourceTotalCo2 = r.GetComputedTotalCO2();
                        if (resourceTotalCo2.HasValue)
                        {
                            totalCo2 += resourceTotalCo2.Value;
                            hasCo2 = true;
                        }

                        var resourceBaseCost = r.GetComputedBaseCost();
                        if (resourceBaseCost.HasValue)
                        {
                            baseCost += resourceBaseCost.Value;
                            hasBaseCost = true;
                        }
                    }
                }

                t.SetComputedAggregates(
                    netQ,
                    netTot,
                    apriceTot,
                    hasCo2 ? totalCo2 : null,
                    hasBaseCost ? baseCost : null);
            }
        }

        // =============================
        // ExecuteCalculation (FAST)
        // =============================
        public static void ExecuteCalculation(this CalculationMVVM calc)
        {
            if (calc is null) return;

            calc.Factors ??= [];
            calc.QuanityList ??= [];
            calc.InvalidateAllCaches();

            // Index سريع للـQuanityList (بدل FirstOrDefault آلاف المرات)
            var qIndex = BuildQuantityIndex(calc.QuanityList);

            // إعادة بناء العوامل من الصفر (لتجنب تراكم عوامل قديمة بعد إعادة الحساب)
            //calc.Factors.Clear();

            // 1) احسب كميات الـTasks (DFS) + 2) احسب كميات الموارد واجمع العوامل في نفس المرور
            InitializeCalculationFast(calc, qIndex);

            // 3) احسب Factor لكل عامل بدون LINQ وبدون O(n^2)
            ApplyFactorF_Optimized(calc.Factors);

            // 4) وزّع الـFactor على الموارد (O(1) lookup)
            AssignFactorsToResourcesFast(calc);

            // 5) تنظيف عوامل غير مستخدمة (in-place)
            FilterFactorsInPlace(calc);

            // 6) ProfitDecision بدون LINQ
            var factors = calc.Factors;
            decimal sum = 0;
            decimal weighted = 0;
            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                var s = f.Sum;
                sum += s;
                weighted += s * f.Earnings;
            }
            calc.ProfitDecision = sum != 0 ? (weighted / sum) : 0;

            ComputeTaskAggregates(calc);
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

            decimal lockedSumWeighted = 0;
            decimal unlockedSum = 0;

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

            // نفس معادلتك
            decimal e = ((calc.ProfitDecision * calc.Sum) - lockedSumWeighted) / unlockedSum;

            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                if (!f.IsLocked && f.EarningsValue > 0)
                    f.Earnings = e;
            }

            // إعادة توزيع العوامل على الموارد بعد تغيير earnings
            AssignFactorsToResourcesFast(calc);
        }

        // =========================================================
        // InitializeCalculationFast
        // - يحسب كميات المهام (DFS) باستخدام qIndex
        // - يحسب كميات الموارد + يجمع Factors في نفس المرور
        // =========================================================
        private static void InitializeCalculationFast(CalculationMVVM calc, Dictionary<string, QuanityListDTO> qIndex)
        {
            var tasks = calc.Tasks;
            if (tasks is null || tasks.Count == 0)
                return;

            // 1) احسب كميات الـroot tasks فقط (ثم recursion يحسب الباقي)
            for (int i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                if (!t.TaskId.HasValue)
                    CalcTaskVariablesRecursive(t, qIndex, parentQuantity: null);
            }

            // 2) اجمع عوامل الموارد
            // 2) اجمع عوامل الموارد (استخدم الموجود من REST API وأنشئ فقط عند عدم الوجود)
            var factors = calc.Factors ??= new List<Factors>();

            // index من الموجود
            var factorIndex = new Dictionary<FactorKey, Factors>(Math.Max(256, factors.Count * 2));

            // ✅ صفّر قيم التجميع فقط (لا تلمس Earnings/IsLocked/Selected...)
            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];

                f.NetCostTotaly = 0;
                f.NetCostTotalyOH = 0;
                f.Factor = 0; // سيتحسب لاحقًا في ApplyFactorF_Optimized

                factorIndex[new FactorKey(f.ResId, f.SortId, f.ResourceType)] = f;
            }

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
                bool taskIsOH = task.Metadata?.IsOH == true;

                for (int r = 0; r < resList.Count; r++)
                {
                    var res = resList[r];

                    CalcResourceVariables(res, qIndex, taskQty, taskCap);

                    if (!res.Active)
                        continue;

                    var key = new FactorKey(res.ResourceTypeId, res.ResourceSortId, res.ResType);

                    if (!factorIndex.TryGetValue(key, out var f))
                    {
                        // ✅ أنشئ فقط إذا غير موجود في REST API
                        f = Factors.AddNewFactor(taskIsOH, res);
                        factors.Add(f);
                        factorIndex[key] = f;
                    }
                    else
                    {
                        // ✅ حدّث الاسم/التصنيف لو تغيروا
                        if (res.ResName != f.ResName) f.ResName = res.ResName ?? string.Empty;
                        if (res.Sort != f.Sort) f.Sort = res.Sort ?? string.Empty;

                        // ✅ اجمع في نفس الـ factor القادم من REST (يحافظ على Earnings=22)
                        f.AddResValue(taskIsOH, res.GetComputedNetCostTotaly());
                    }
                }
            }

        }

        // =========================================================
        // AssignFactorsToResourcesFast
        // - يبني index واحد ثم يمر على الموارد ويضع res.Factor
        // =========================================================
        public static void AssignFactorsToResourcesFast(CalculationMVVM calc)
        {
            var tasks = calc.Tasks;
            var factors = calc.Factors;

            if (tasks is null || factors is null || factors.Count == 0)
                return;

            var index = new Dictionary<FactorKey, Factors>(Math.Max(16, factors.Count * 2));
            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                index[new FactorKey(f.ResId, f.SortId, f.ResourceType)] = f;
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                var resList = t.Resources;
                if (resList is null || resList.Count == 0)
                    continue;

                for (int r = 0; r < resList.Count; r++)
                {
                    var res = resList[r];
                    var key = new FactorKey(res.ResourceTypeId, res.ResourceSortId, res.ResType);

                    if (index.TryGetValue(key, out var factor) && factor is not null)
                        res.SetComputedFactor(factor.Factor);
                }
            }
        }

        // =========================================================
        // ApplyFactorF_Optimized
        // - يحسب Factor لكل عنصر في O(n) بدل O(n^2)
        // - يحاكي منطق OHF/FactorF بدون LINQ
        // =========================================================
        // داخل CalcultationExtensions.cs :contentReference[oaicite:3]{index=3}

        private static void ApplyFactorF_Optimized(List<Factors> factors)
        {
            if (factors is null || factors.Count == 0) return;

            // sum(NetCostTotaly) مرة واحدة
            decimal sumNetCostAll = 0;
            for (int i = 0; i < factors.Count; i++)
                sumNetCostAll += factors[i].NetCostTotaly;

            // مجموع OH لعناصر Selected == "all"
            decimal totalOHAll = 0;
            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                if (f.Selected == "all" && f.NetCostTotalyOH > 0)
                    totalOHAll += f.NetCostTotalyOH * (1 + (f.Earnings / 100));
            }

            // ✅ تجميع OH المرتبط باستخدام FactorKey بدل string
            var relatedMap = new Dictionary<FactorKey, decimal>(Math.Max(16, factors.Count));

            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                if (f.NetCostTotalyOH <= 0) continue;

                var sel = f.Selected;
                if (string.IsNullOrEmpty(sel) || sel == "all") continue;

                // sel كان بالشكل "ResId,SortId"
                // ✅ parse سريع بدون Split allocations
                int comma = sel.IndexOf(',');
                if (comma <= 0 || comma >= sel.Length - 1) continue;

                if (!int.TryParse(sel.AsSpan(0, comma), out int resId)) continue;
                if (!int.TryParse(sel.AsSpan(comma + 1), out int sortId)) continue;

                var key = new FactorKey(resId, sortId, f.ResourceType);

                var add = f.NetCostTotalyOH * (1 + (f.Earnings / 100));
                relatedMap[key] = relatedMap.TryGetValue(key, out var cur) ? (cur + add) : add;
            }

            // حساب Factor لكل عامل
            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                if (f.NetCostTotaly <= 0) continue;

                decimal ohShare = (sumNetCostAll != 0)
                    ? (f.NetCostTotaly * totalOHAll) / sumNetCostAll
                    : 0;

                // ✅ related OH lookup بدون strings
                var key = new FactorKey(f.ResId, f.SortId, f.ResourceType);
                relatedMap.TryGetValue(key, out var relatedOH);

                f.Factor = ((f.NetCostTotaly * (1 + (f.Earnings / 100))) + relatedOH + ohShare) / f.NetCostTotaly;
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
        // Quantity index
        // =========================================================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<string, QuanityListDTO> BuildQuantityIndex(List<QuanityListDTO> list)
        {
            var dict = new Dictionary<string, QuanityListDTO>(StringComparer.Ordinal);
            if (list is null || list.Count == 0) return dict;

            for (int i = 0; i < list.Count; i++)
            {
                var q = list[i];
                if (!string.IsNullOrEmpty(q.Name))
                    dict[q.Name] = q;
            }
            return dict;
        }

        // =========================================================
        // Task variables (DFS) - بدون LINQ
        // =========================================================
        private static void CalcTaskVariablesRecursive(TaskListMVVM task, 
            Dictionary<string, QuanityListDTO> qIndex, decimal? parentQuantity)
        {
            var meta = task.Metadata;
            if (meta is null) return;

            if (meta.Type == TaskType.CodeName)
            {
                meta.Quantity = null;
            }
            else if (!string.IsNullOrEmpty(meta.QuantityParam))
            {
                if (qIndex.TryGetValue(meta.QuantityParam, out var param))
                    meta.Quantity = param.Quantity;
                else
                    meta.QuantityParam = ConstValues.FixedQ;
            }
            else
            {
                meta.Quantity = meta.ChangeFactor1 * meta.ChangeFactor2 * (parentQuantity ?? 0m);
            }

            if (task.Tasks is null || task.Tasks.Count == 0)
                return;

            var nextParent = meta.Quantity ?? parentQuantity;
            for (int i = 0; i < task.Tasks.Count; i++)
                CalcTaskVariablesRecursive(task.Tasks[i], qIndex, nextParent);
        }

        // =========================================================
        // Resource variables - بدون LINQ
        // =========================================================
        private static void CalcResourceVariables(
            ResourceListMVVM resource,
            Dictionary<string, QuanityListDTO> qIndex,
            decimal? taskQuantity,
            decimal? cap)
        {
            if (resource.Data is null)
                throw new InvalidOperationException("resource.Data must not be null.");

            var data = resource.Data;
            ApplyResourceParameterFactor(data);

            if (resource.HasCap && cap.HasValue)
                data.CapWaste = cap.Value;

            var effectiveTaskQuantity = taskQuantity ?? 0m;

            if (!string.IsNullOrEmpty(resource.QuantityParam))
            {
                if (qIndex.TryGetValue(resource.QuantityParam, out var matched))
                {
                    data.Quantity = matched.Quantity;
                }
                else
                {
                    data.QuantityParam = ConstValues.FixedQ;
                }
            }
            else
            {
                var baseCalc = effectiveTaskQuantity * data.ChangeFactor1 * data.ChangeFactor2;
                var capWaste = data.CapWaste;

                if (resource.HasWast && capWaste != 0)
                    data.Quantity = baseCalc * (1 + capWaste / 100);
                else if (resource.HasCap && capWaste != 0)
                    data.Quantity = baseCalc / capWaste;
                else
                    data.Quantity = baseCalc;
            }

            ApplyResourceTimedCost(data);
        }

        private static void ApplyResourceParameterFactor(ResourceMetadata data)
        {
            var parameters = data.Parameters;
            if (parameters is null || parameters.Count == 0)
                return;

            decimal product = 1m;
            for (int i = 0; i < parameters.Count; i++)
                product *= parameters[i].Value;

            data.ChangeFactor1 = RoundFactor(product);
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

            data.Cost = RoundMoney(total / quantity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal RoundMoney(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal RoundFactor(decimal value) =>
            Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }
}
