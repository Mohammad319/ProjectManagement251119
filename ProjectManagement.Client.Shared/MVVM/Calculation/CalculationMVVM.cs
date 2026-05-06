using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public enum CalculationFactorDisplayMode
    {
        NetCal = 0,
        OH = 1,
        All = 2
    }

    public class ListCalculationMVVM : ListCalculationDTO
    {
        public bool IsDragOver { get; set; }
    }

    public readonly record struct FlatItem(
        int Index,
        TaskListMVVM? Task,
        ResourceListMVVM? Resource,
        int Depth,
        bool ParentActive = true)
    {
        public bool IsTask => Task is not null;
        public bool IsResource => Resource is not null;

        // مفتاح سريع وثابت لـ Virtualize و @key
        public long Key =>
            Task is not null ? (1L << 60) | (uint)Task.Id :
            Resource is not null ? (2L << 60) | (uint)Resource.Id :
            0;
    }

    public class CalculationMVVM
    {
        [JsonIgnore] public bool LastHubChangeAffectsCalc { get; set; } = false;
        [JsonIgnore] public bool FlatListDirty { get; set; } = true;
        private bool _structureFlatListDirty = true;

        // ParentId -> Children Tasks
        [JsonIgnore] public Dictionary<int, List<TaskListMVVM>> ChildrenLookup { get; private set; } = new();

        // Root tasks (TaskId == null)
        [JsonIgnore] public List<TaskListMVVM> RootTasks { get; private set; } = new();

        // أقصى عمق مستخدم لحساب عرض العمود الأول
        [JsonIgnore] public int MaxDepth { get; private set; }

        // القائمة المسطّحة المستخدمة في Virtualize
        public List<FlatItem>? AllFlatItems { get; set; }
        private List<FlatItem>? _structureFlatItems;

        // ====== Indexes (أهم تحسين للسرعة) ======
        [JsonIgnore] public Dictionary<int, TaskListMVVM> TaskById { get; private set; } = new();
        [JsonIgnore] public Dictionary<int, ResourceListMVVM> ResourceById { get; private set; } = new();

        // OfferId -> Offer (لمنع SelectMany داخل OfferHub)
        [JsonIgnore] public Dictionary<int, ListOfferMVVM> OfferById { get; private set; } = new();

        // ====== خصائص الحساب ======
        public int Id { get; set; }
        public int Tax { get; set; }

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
        public int? TemplateColumnId { get; set; }
        public SortConfig Sort { get; set; } = new();

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
        public decimal ResourcePriceSum { get; set; } = 0;

        public decimal Sum => Factors.Sum(x => x.Sum);
        public decimal SumFactorsEV => Factors.Sum(x => x.EarningsValue);
        public decimal SumFactorsPrice => Factors.Sum(x => x.Price);

        public decimal ProfitDecision { get; set; }
        public decimal TenderExcelTax { get; set; }
        public decimal TenderInclTax { get; set; }

        public bool OnlyActive { get; set; }
        public bool ShowTasks { get; set; } = true;
        public bool ShowResources { get; set; } = true;
        public bool ShowOnlyCodeTextTasks { get; set; } = true;

        private bool _ohFactors;
        [JsonIgnore] private CalculationFactorDisplayMode _factorDisplayMode;

        public bool OHFactors
        {
            get => _ohFactors;
            set
            {
                _ohFactors = value;

                if (_factorDisplayMode != CalculationFactorDisplayMode.All)
                    _factorDisplayMode = value ? CalculationFactorDisplayMode.OH : CalculationFactorDisplayMode.NetCal;
            }
        }

        [JsonIgnore]
        public CalculationFactorDisplayMode FactorDisplayMode
        {
            get => _factorDisplayMode == CalculationFactorDisplayMode.All
                ? CalculationFactorDisplayMode.All
                : (_ohFactors ? CalculationFactorDisplayMode.OH : CalculationFactorDisplayMode.NetCal);
            set
            {
                _factorDisplayMode = value;

                if (value != CalculationFactorDisplayMode.All)
                    _ohFactors = value == CalculationFactorDisplayMode.OH;
            }
        }

        public List<HourlyPriceListGroupDTO> HourlyPriceList { get; set; } = [];
        public List<OpportunityModel> Opportunities { get; set; } = [];

        public TemplateMVVM Template { get; set; } = new();
        public FilterVM? FilterVM { get; set; }
        public DisplayOptionsPresetStore DisplayPresets { get; set; } = new();

        // ====== إشعار الجدول بالتحديث ======
        public event Action? OnChangeInCalculation;

        public void NotifyGridRefresh(bool flatListDirty = false, bool structureFlatListDirty = false)
        {
            if (structureFlatListDirty)
                _structureFlatListDirty = true;

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
            MarkStructureDirty();
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
                        res.TaskId = t.Id;
                        res.SyncOfferSelection();

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
            var set = ids is HashSet<int> hs ? hs : [.. ids];

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
            res.SyncOfferSelection();

            // تحديث indexes بشكل incremental (بدون full rebuild)
            ResourceById[res.Id] = res;
            if (res.Offers is not null)
            {
                for (int i = 0; i < res.Offers.Count; i++)
                    OfferById[res.Offers[i].Id] = res.Offers[i];
            }

            MarkStructureDirty();
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

            MarkStructureDirty();
        }

        public bool TryToggleTaskCollapse(TaskListMVVM task)
        {
            if (task is null || _structureFlatItems is null || _structureFlatListDirty)
                return false;

            int taskIndex = FindTaskFlatIndex(task.Id, _structureFlatItems);
            if (taskIndex < 0)
                return false;

            int depth = _structureFlatItems[taskIndex].Depth;
            task.Ui.CollSpan = !task.Ui.CollSpan;

            if (!task.Ui.CollSpan)
                RemoveTaskDescendants(_structureFlatItems, taskIndex, depth);
            else
                InsertTaskDescendants(_structureFlatItems, task, taskIndex, depth);

            ReindexFlatItems(_structureFlatItems, taskIndex + 1);
            FlatListDirty = true;
            return true;
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
            if (!ShowTasks && !ShowResources && !ShowOnlyCodeTextTasks)
            {
                MaxDepth = 0;
                return [];
            }

            EnsureStructureFlatList();
            if (_structureFlatItems is null || _structureFlatItems.Count == 0)
            {
                MaxDepth = 0;
                return [];
            }

            MaxDepth = 0;

            int estimatedCapacity = Math.Max(256, _structureFlatItems.Count);
            var flat = new List<FlatItem>(estimatedCapacity);
            bool[] branchVisibleByDepth = new bool[Math.Max(8, MaxDepth + Tasks.Count + 4)];
            bool[] branchActiveByDepth = new bool[branchVisibleByDepth.Length];

            int index = 0;
            for (int i = 0; i < _structureFlatItems.Count; i++)
            {
                var item = _structureFlatItems[i];
                if (item.IsTask)
                {
                    var task = item.Task!;
                    bool parentBranchVisible = item.Depth == 0 || branchVisibleByDepth[item.Depth - 1];
                    bool parentBranchActive = item.Depth == 0 || branchActiveByDepth[item.Depth - 1];
                    bool branchVisible = parentBranchVisible && IsTaskBranchVisible(task);
                    bool branchActive = parentBranchActive && task.Active;
                    branchVisibleByDepth[item.Depth] = branchVisible;
                    branchActiveByDepth[item.Depth] = branchActive;

                    if (!branchVisible || !ShouldShowTaskRow(task))
                        continue;
                    if (task.Type != TaskType.CodeName && !ShowTasks)
                        continue;

                    flat.Add(new FlatItem(index++, task, null, item.Depth, parentBranchActive));
                    if (item.Depth > MaxDepth)
                        MaxDepth = item.Depth;
                }
                else if (item.IsResource)
                {
                    bool parentBranchVisible = item.Depth == 0 || branchVisibleByDepth[item.Depth - 1];
                    bool parentBranchActive = item.Depth == 0 || branchActiveByDepth[item.Depth - 1];
                    if (!parentBranchVisible || !ShowResources)
                        continue;

                    var resource = item.Resource!;
                    if (!resource.Ui.FilterVisible)
                        continue;

                    if (OnlyActive && !resource.Active)
                        continue;

                    flat.Add(new FlatItem(index++, null, resource, item.Depth, parentBranchActive));
                    if (item.Depth > MaxDepth)
                        MaxDepth = item.Depth;
                }
            }

            return flat;
        }

        private int FindTaskFlatIndex(int taskId, List<FlatItem> flatItems)
        {
            for (int i = 0; i < flatItems.Count; i++)
            {
                if (flatItems[i].Task?.Id == taskId)
                    return i;
            }

            return -1;
        }

        private void RemoveTaskDescendants(List<FlatItem> flatItems, int taskIndex, int depth)
        {
            int start = taskIndex + 1;
            int count = 0;

            while (start + count < flatItems.Count && flatItems[start + count].Depth > depth)
                count++;

            if (count > 0)
                flatItems.RemoveRange(start, count);
        }

        private void InsertTaskDescendants(List<FlatItem> flatItems, TaskListMVVM task, int taskIndex, int depth)
        {
            List<FlatItem> descendants = [];
            int index = taskIndex + 1;
            BuildStructureDescendants(task, depth + 1, descendants, ref index);

            if (descendants.Count > 0)
                flatItems.InsertRange(taskIndex + 1, descendants);
        }

        private SortConfig? CurrentSort => Sort;

        private void BuildStructureFlatListInternal(
            List<TaskListMVVM> tasks,
            int depth,
            List<FlatItem> flat,
            ref int index)
        {
            var sorted = ApplyTaskSort(tasks, CurrentSort);
            foreach (var task in sorted)
            {
                flat.Add(new FlatItem(index++, task, null, depth));

                if (!task.Ui.CollSpan)
                    continue;

                BuildStructureDescendants(task, depth + 1, flat, ref index);
            }
        }

        private void BuildStructureDescendants(
            TaskListMVVM task,
            int depth,
            List<FlatItem> flat,
            ref int index)
        {
            if (task.Resources is not null && task.Resources.Count > 0)
            {
                var sortedResources = ApplyResourceSort(task.Resources, CurrentSort);
                foreach (var res in sortedResources)
                    flat.Add(new FlatItem(index++, null, res, depth));
            }

            if (task.Tasks is null || task.Tasks.Count == 0)
                return;

            var sortedChildren = ApplyTaskSort(task.Tasks, CurrentSort);
            foreach (var child in sortedChildren)
            {
                flat.Add(new FlatItem(index++, child, null, depth));

                if (!child.Ui.CollSpan)
                    continue;

                BuildStructureDescendants(child, depth + 1, flat, ref index);
            }
        }

        private static IEnumerable<TaskListMVVM> ApplyTaskSort(IList<TaskListMVVM> tasks, SortConfig? sort)
        {
            if (sort?.TaskColumn is not NetColumnId col)
                return tasks
                    .OrderByDescending(t => t.Order)
                    .ThenBy(t => t.Id);

            Func<TaskListMVVM, IComparable> key = col switch
            {
                NetColumnId.Name        => t => t.Name,
                NetColumnId.Code        => t => t.Code,
                NetColumnId.Quantity    => t => (IComparable)(t.Quantity ?? 0m),
                NetColumnId.Unit        => t => t.Unit,
                NetColumnId.NetCostQ    => t => t.NetCostQ,
                NetColumnId.TotalNetCost => t => t.NetCostTotaly,
                NetColumnId.PriceTotaly  => t => t.ApriceTotally,
                NetColumnId.ChangeFactor1 => t => t.ChangeFactor1,
                NetColumnId.ChangeFactor2 => t => t.ChangeFactor2,
                NetColumnId.Status       => t => t.Status,
                NetColumnId.Responsible  => t => t.Responsible,
                _                        => t => (IComparable)t.Order,
            };

            return sort.TaskDescending
                ? tasks.OrderByDescending(key).ThenByDescending(t => t.Order).ThenBy(t => t.Id)
                : tasks.OrderBy(key).ThenByDescending(t => t.Order).ThenBy(t => t.Id);
        }

        private static IEnumerable<ResourceListMVVM> ApplyResourceSort(IList<ResourceListMVVM> resources, SortConfig? sort)
        {
            if (sort?.ResourceColumn is not NetColumnId col)
                return resources
                    .OrderByDescending(r => r.Order)
                    .ThenBy(r => r.Id);

            Func<ResourceListMVVM, IComparable> key = col switch
            {
                NetColumnId.Name              => r => r.Name,
                NetColumnId.Quantity          => r => (IComparable)(r.Quantity ?? 0m),
                NetColumnId.Unit              => r => r.Unit,
                NetColumnId.Cost              => r => r.Cost,
                NetColumnId.NetCostQ          => r => r.NetCostQ,
                NetColumnId.ChangeFactor1     => r => r.ChangeFactor1,
                NetColumnId.Account           => r => r.Account ?? string.Empty,
                NetColumnId.ResourceTypeSystem => r => r.ResType.ToString(),
                NetColumnId.Status            => r => r.Status ?? string.Empty,
                _                             => r => (IComparable)0,
            };

            return sort.ResourceDescending
                ? resources.OrderByDescending(key).ThenByDescending(r => r.Order).ThenBy(r => r.Id)
                : resources.OrderBy(key).ThenByDescending(r => r.Order).ThenBy(r => r.Id);
        }

        private void ReindexFlatItems(List<FlatItem> flatItems, int startIndex)
        {
            for (int i = startIndex; i < flatItems.Count; i++)
            {
                var item = flatItems[i];
                flatItems[i] = new FlatItem(i, item.Task, item.Resource, item.Depth, item.ParentActive);
            }
        }

        private bool IsTaskBranchVisible(TaskListMVVM task) =>
            task.Ui.FilterVisible &&
            MatchesFactorDisplay(task.IsOH) &&
            (!OnlyActive || task.Active);

        public DisplayOptionsPreset BuildDisplayOptionsPreset(bool showComments, bool showResourceVariables)
        {
            var preset = new DisplayOptionsPreset
            {
                ShowTasks = ShowTasks,
                ShowResources = ShowResources,
                ShowComments = showComments,
                ShowResourceVariables = showResourceVariables,
                OnlyActive = OnlyActive,
            };

            for (int i = 0; i < Tasks.Count; i++)
            {
                var task = Tasks[i];
                if (!task.Active)
                    preset.InactiveTaskIds.Add(task.Id);

                if (task.Resources is null)
                    continue;

                for (int r = 0; r < task.Resources.Count; r++)
                {
                    var resource = task.Resources[r];
                    if (!resource.Active)
                        preset.InactiveResourceIds.Add(resource.Id);
                }
            }

            return preset;
        }

        public void ApplyPresetActiveOverrides(DisplayOptionsPreset preset)
        {
            var inactiveTaskIds = preset.InactiveTaskIds is null
                ? new HashSet<int>()
                : new HashSet<int>(preset.InactiveTaskIds);

            var inactiveResourceIds = preset.InactiveResourceIds is null
                ? new HashSet<int>()
                : new HashSet<int>(preset.InactiveResourceIds);

            for (int i = 0; i < Tasks.Count; i++)
            {
                var task = Tasks[i];
                task.SetActiveOverride(!inactiveTaskIds.Contains(task.Id));

                if (task.Resources is null)
                    continue;

                for (int r = 0; r < task.Resources.Count; r++)
                {
                    var resource = task.Resources[r];
                    resource.SetActiveOverride(!inactiveResourceIds.Contains(resource.Id));
                }
            }
        }

        public void ClearPresetActiveOverrides()
        {
            for (int i = 0; i < Tasks.Count; i++)
            {
                var task = Tasks[i];
                task.SetActiveOverride(null);

                if (task.Resources is null)
                    continue;

                for (int r = 0; r < task.Resources.Count; r++)
                    task.Resources[r].SetActiveOverride(null);
            }
        }

        public void ToggleTaskActiveInPreset(int taskId, bool showComments, bool showResourceVariables)
        {
            var preset = GetOrCreateActivePreset(showComments, showResourceVariables);
            var task = FindInStructure(taskId, isTask: true)?.Task;
            if (task == null) return;
            bool newActive = !task.Active;
            task.SetActiveOverride(newActive);
            if (!newActive) { if (!preset.InactiveTaskIds.Contains(taskId)) preset.InactiveTaskIds.Add(taskId); }
            else preset.InactiveTaskIds.Remove(taskId);
        }

        public void ToggleResourceActiveInPreset(int resourceId, bool showComments, bool showResourceVariables)
        {
            var preset = GetOrCreateActivePreset(showComments, showResourceVariables);
            var resource = FindInStructure(resourceId, isTask: false)?.Resource;
            if (resource == null) return;
            bool newActive = !resource.Active;
            resource.SetActiveOverride(newActive);
            if (!newActive) { if (!preset.InactiveResourceIds.Contains(resourceId)) preset.InactiveResourceIds.Add(resourceId); }
            else preset.InactiveResourceIds.Remove(resourceId);
        }

        private FlatItem? FindInStructure(int id, bool isTask)
        {
            EnsureStructureFlatList();
            foreach (var item in _structureFlatItems!)
            {
                if (isTask && item.IsTask && item.Task!.Id == id) return item;
                if (!isTask && item.IsResource && item.Resource!.Id == id) return item;
            }
            return null;
        }

        private DisplayOptionsPreset GetOrCreateActivePreset(bool showComments, bool showResourceVariables)
        {
            var preset = DisplayOptionsPresetState.GetActivePreset(DisplayPresets);
            if (preset != null) return preset;
            var created = DisplayOptionsPresetState.SavePreset(
                DisplayPresets,
                "Default",
                BuildDisplayOptionsPreset(showComments, showResourceVariables));
            ApplyPresetActiveOverrides(created);
            return created;
        }

        private bool ShouldShowTaskRow(TaskListMVVM task) =>
            ShowOnlyCodeTextTasks || task.Type != TaskType.CodeName;

        public bool MatchesFactorDisplay(bool isOH) =>
            FactorDisplayMode switch
            {
                CalculationFactorDisplayMode.OH => isOH,
                CalculationFactorDisplayMode.All => true,
                _ => !isOH
            };

        private void EnsureStructureFlatList()
        {
            if (!_structureFlatListDirty && _structureFlatItems is not null)
                return;

            int index = 0;
            var flat = new List<FlatItem>(Math.Max(256, Tasks.Count + ResourceById.Count));
            BuildStructureFlatListInternal(RootTasks, 0, flat, ref index);
            _structureFlatItems = flat;
            _structureFlatListDirty = false;
        }

        private void MarkStructureDirty()
        {
            _structureFlatListDirty = true;
            FlatListDirty = true;
        }
    }
}
