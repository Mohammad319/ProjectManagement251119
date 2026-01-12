using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public class ListCalculationMVVM : ListCalculationDTO
    {
        public bool IsDragOver { get; set; }
    }

    public readonly record struct FlatItem(
        int Index,
        TaskListMVVM? Task,
        ResourceListMVVM? Resource,
        int Depth)
    {
        public bool IsTask => Task is not null;
        public bool IsResource => Resource is not null;

        // مفتاح سريع وثابت لـ Virtualize و @key
        public long Key =>
            Task is not null ? (1L << 60) | (uint)Task.Id :
            Resource is not null ? (2L << 60) | (uint)Resource.Id :
            0;

        public string VersionKey =>
            Task is not null ? $"T_{Task.Id}" :
            Resource is not null ? $"R_{Resource.Id}" :
            "X";
    }

    public class CalculationMVVM
    {
        [JsonIgnore] public bool LastHubChangeAffectsCalc { get; set; } = false;
        [JsonIgnore] public bool FlatListDirty { get; set; } = true;
        [JsonIgnore] public int GridVersion { get; private set; } = 1;
        public void BumpGridVersion() => GridVersion++;

        // ParentId -> Children Tasks
        [JsonIgnore] public Dictionary<int, List<TaskListMVVM>> ChildrenLookup { get; private set; } = new();

        // Root tasks (TaskId == null)
        [JsonIgnore] public List<TaskListMVVM> RootTasks { get; private set; } = new();

        // أقصى عمق مستخدم لحساب عرض العمود الأول
        [JsonIgnore] public int MaxDepth { get; private set; }

        // القائمة المسطّحة المستخدمة في Virtualize
        public List<FlatItem>? AllFlatItems { get; set; }

        // ====== Indexes (أهم تحسين للسرعة) ======
        [JsonIgnore] public Dictionary<int, TaskListMVVM> TaskById { get; private set; } = new();
        [JsonIgnore] public Dictionary<int, ResourceListMVVM> ResourceById { get; private set; } = new();

        // OfferId -> Offer (لمنع SelectMany داخل OfferHub)
        [JsonIgnore] public Dictionary<int, ListOfferMVVM> OfferById { get; private set; } = new();

        // ====== خصائص الحساب ======
        public int Id { get; set; }
        public double Tax { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Supervisor { get; set; } = string.Empty;
        public string Inspector { get; set; } = string.Empty;
        public string Compensation { get; set; } = string.Empty;
        public string Contract { get; set; } = string.Empty;

        public int? TemplateId { get; set; }

        public bool Tap1 { get; set; } = true;
        public bool Tap2 { get; set; } = true;
        public bool Tap3 { get; set; } = true;
        public bool Tap4 { get; set; } = true;
        public bool Tap5 { get; set; } = true;
        public bool Tap6 { get; set; } = true;

        public List<QuanityListDTO> QuanityList { get; set; } = [];
        public virtual List<TaskListMVVM> Tasks { get; set; } = [];
        public List<Factors> Factors { get; set; } = [];

        public double AdditionalCostEarnings { get; set; } = 10;
        public double ResourcePriceSum { get; set; } = 0;

        public double Sum => Factors.Sum(x => x.Sum);
        public double SumFactorsEV => Factors.Sum(x => x.EarningsValue);
        public double SumFactorsPrice => Factors.Sum(x => x.Price);

        public double ProfitDecision { get; set; }
        public double TenderExcelTax { get; set; }
        public double TenderInclTax { get; set; }

        public bool OnlyActive { get; set; }
        public bool ShowTasks { get; set; } = true;
        public bool ShowResources { get; set; } = true;
        public bool ShowComment { get; set; } = true;

        public bool OHFactors { get; set; }

        public List<HourlyPriceListGroupDTO> HourlyPriceList { get; set; } = [];
        public List<OpportunityModel> Opportunities { get; set; } = [];

        public TemplateMVVM Template { get; set; } = new();
        public FilterVM? FilterVM { get; set; }

        // ====== إشعار الجدول بالتحديث ======
        public event Action? OnChangeInCalculation;

        public void NotifyGridRefresh(bool flatListDirty = false)
        {
            if (flatListDirty)
                FlatListDirty = true;

            OnChangeInCalculation?.Invoke();
        }

        // ============================================================
        //  إعادة بناء الشجرة + indexes (يُستدعى عند التغييرات البنيوية)
        // ============================================================
        public void RebuildHierarchyAndIndexes()
        {
            BuildTaskHierarchy();
            RebuildIndexes();
            FlatListDirty = true;
        }

        // ====== بناء شجرة المهام ======
        public void BuildTaskHierarchy()
        {
            ChildrenLookup = new Dictionary<int, List<TaskListMVVM>>(Math.Max(16, Tasks.Count));
            RootTasks = new List<TaskListMVVM>(Math.Max(16, Tasks.Count / 3));
            MaxDepth = 0;

            // 1) نظّف وربط مبدئي
            for (int i = 0; i < Tasks.Count; i++)
            {
                var t = Tasks[i];
                t.Tasks ??= [];
                t.Tasks.Clear();

                if (t.TaskId is null)
                {
                    RootTasks.Add(t);
                }
                else
                {
                    int pid = t.TaskId.Value;
                    if (!ChildrenLookup.TryGetValue(pid, out var list))
                    {
                        list = new List<TaskListMVVM>(4);
                        ChildrenLookup[pid] = list;
                    }
                    list.Add(t);
                }
            }

            // 2) اربط children لكل task
            for (int i = 0; i < Tasks.Count; i++)
            {
                var t = Tasks[i];
                if (ChildrenLookup.TryGetValue(t.Id, out var children))
                    t.Tasks = children;
                else
                    t.Tasks = [];
            }
        }

        // ====== بناء indexes ======
        public void RebuildIndexes()
        {
            TaskById = new Dictionary<int, TaskListMVVM>(Tasks.Count);
            ResourceById = new Dictionary<int, ResourceListMVVM>(Math.Max(16, Tasks.Count * 4));
            OfferById = new Dictionary<int, ListOfferMVVM>(Math.Max(16, Tasks.Count * 4));

            for (int i = 0; i < Tasks.Count; i++)
            {
                var t = Tasks[i];
                TaskById[t.Id] = t;

                // resources
                if (t.Resources is not null)
                {
                    for (int r = 0; r < t.Resources.Count; r++)
                    {
                        var res = t.Resources[r];
                        ResourceById[res.Id] = res;

                        if (res.Offers is not null)
                        {
                            for (int o = 0; o < res.Offers.Count; o++)
                            {
                                var off = res.Offers[o];
                                OfferById[off.Id] = off;
                            }
                        }
                    }
                }
            }
        }

        public bool TryGetTask(int id, out TaskListMVVM? task) =>
            TaskById.TryGetValue(id, out task);

        public bool TryGetResource(int id, out ResourceListMVVM? res) =>
            ResourceById.TryGetValue(id, out res);

        public bool TryGetOffer(int offerId, out ListOfferMVVM? offer) =>
            OfferById.TryGetValue(offerId, out offer);

        // ====== عمليات بنيوية تُستخدم من الخدمات ======

        public void AddTasks(IEnumerable<TaskListMVVM> tasks)
        {
            Tasks.AddRange(tasks);
            RebuildHierarchyAndIndexes();
        }

        public void RemoveTasks(IEnumerable<int> ids)
        {
            // إزالة مباشرة من قائمة Tasks (قد تكون كثيرة، لكنه يحدث عادةً أقل من Update)
            var set = ids is HashSet<int> hs ? hs : new HashSet<int>(ids);

            for (int i = Tasks.Count - 1; i >= 0; i--)
            {
                if (set.Contains(Tasks[i].Id))
                    Tasks.RemoveAt(i);
            }

            RebuildHierarchyAndIndexes();
        }

        public void Add(ResourceListMVVM res)
        {
            // إضافة مورد إلى task parent
            if (!TryGetTask(res.TaskId, out var task) || task == null)
            {
                // fallback (لو indexes غير جاهزة لأي سبب)
                task = Tasks.FirstOrDefault(t => t.Id == res.TaskId);
                if (task == null) return;
            }

            task.Resources ??= [];
            task.Resources.Add(res);

            // تحديث indexes بشكل incremental (بدون full rebuild)
            ResourceById[res.Id] = res;
            if (res.Offers is not null)
            {
                for (int i = 0; i < res.Offers.Count; i++)
                    OfferById[res.Offers[i].Id] = res.Offers[i];
            }

            FlatListDirty = true;
        }

        public void AddRangeResources(IEnumerable<ResourceListMVVM> resources)
        {
            foreach (var res in resources)
                Add(res);
        }

        public void RemoveResources(IEnumerable<int> ids)
        {
            foreach (var id in ids)
            {
                if (!TryGetResource(id, out var res) || res == null)
                    continue;

                // إزالة من parent list
                if (TryGetTask(res.TaskId, out var task) && task?.Resources is not null)
                {
                    for (int i = task.Resources.Count - 1; i >= 0; i--)
                    {
                        if (task.Resources[i].Id == id)
                        {
                            task.Resources.RemoveAt(i);
                            break;
                        }
                    }
                }

                // إزالة من indexes
                ResourceById.Remove(id);

                if (res.Offers is not null)
                {
                    for (int i = 0; i < res.Offers.Count; i++)
                        OfferById.Remove(res.Offers[i].Id);
                }
            }

            FlatListDirty = true;
        }

        // ====== Invalidate + Calculation ======
        // بما أنك تحتاج إعادة حساب كاملة عند أي تغيير رقمي:
        // نُبقي invalidation كامل، لكن ننفذه مرة واحدة بعد تجميع الأحداث (Batching).
        public void InvalidateAllCaches()
        {
            for (int i = 0; i < Tasks.Count; i++)
            {
                var t = Tasks[i];
                t.InvalidateCache();

                if (t.Resources is null) continue;
                for (int r = 0; r < t.Resources.Count; r++)
                {
                    t.Resources[r].InvalidateCache();
                }
            }
        }

        // ====== بناء القائمة المسطّحة ======
        public List<FlatItem> BuildFlatList()
        {
            MaxDepth = 0;

            // capacity تقريبي لتقليل realloc
            var flat = new List<FlatItem>(Math.Max(256, Tasks.Count * 2));

            int index = 0;
            BuildFlatListInternal(RootTasks, 0, flat, ref index);
            return flat;
        }

        private void BuildFlatListInternal(
            List<TaskListMVVM> tasks,
            int depth,
            List<FlatItem> flat,
            ref int index)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];

                // بدل LINQ Where: if مباشر (أقل GC)
                if (!task.FilterVisible) continue;
                if (task.IsOH != OHFactors) continue;
                if (OnlyActive && !task.Active) continue;

                // صف المهمة
                flat.Add(new FlatItem(index++, task, null, depth));
                if (depth > MaxDepth) MaxDepth = depth;

                // لو المهمة مغلقة لا نضيف أولادها
                if (!task.CollSpan)
                    continue;

                // الموارد التابعة للمهمة
                if (task.Resources is not null)
                {
                    for (int r = 0; r < task.Resources.Count; r++)
                    {
                        var res = task.Resources[r];
                        if (!res.FilterVisible) continue;

                        flat.Add(new FlatItem(index++, null, res, depth + 1));
                        if (depth + 1 > MaxDepth) MaxDepth = depth + 1;
                    }
                }

                // المهام الفرعية
                if (task.Tasks is not null && task.Tasks.Count > 0)
                {
                    if (depth + 1 > MaxDepth) MaxDepth = depth + 1;
                    BuildFlatListInternal(task.Tasks, depth + 1, flat, ref index);
                }
            }
        }
    }
}
