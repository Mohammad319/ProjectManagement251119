using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Demo
{
    public sealed class ConstructionDemoSeedResult
    {
        public int FoldersCreated { get; set; }
        public int ResourcesCreated { get; set; }
        public int PropertyGroupsCreated { get; set; }
        public int PropertiesCreated { get; set; }
        public int LookupsCreated { get; set; }
        public int UnitGroupsCreated { get; set; }
        public int TasksCreated { get; set; }
        public int AssignmentsCreated { get; set; }

        public bool HasChanges =>
            FoldersCreated > 0 ||
            ResourcesCreated > 0 ||
            PropertyGroupsCreated > 0 ||
            PropertiesCreated > 0 ||
            LookupsCreated > 0 ||
            UnitGroupsCreated > 0 ||
            TasksCreated > 0 ||
            AssignmentsCreated > 0;
    }

    public interface IConstructionDemoDataService
    {
        Task<ConstructionDemoSeedResult> SeedAsync(CancellationToken ct = default);
    }

    public sealed class ConstructionDemoDataService(IDbContextFactory<TaskResourceBlueprintsContext> contextFactory)
        : IConstructionDemoDataService
    {
        private static readonly DemoFolderSpec[] FolderSpecs =
        [
            new("building-materials", "Building Materials", null, 10, "Demo parent folder for construction materials."),
            new("cement", "Cement & Concrete", "building-materials", 20, "Binders, concrete mixes, and related items."),
            new("timber", "Timber & Formwork", "building-materials", 30, "Wood products and formwork items."),
            new("steel", "Reinforcement Steel", "building-materials", 40, "Steel reinforcement resources."),
            new("equipment", "Machines & Equipment", null, 50, "Demo parent folder for site equipment."),
            new("excavation-equipment", "Excavation Equipment", "equipment", 60, "Excavators and trenching equipment."),
            new("backfilling-equipment", "Backfilling Equipment", "equipment", 70, "Compaction and backfilling equipment."),
            new("lifting-equipment", "Lifting Equipment", "equipment", 80, "Cranes, forklifts, and lifting gear.")
        ];

        private static readonly DemoResourceSpec[] ResourceSpecs =
        [
            new("Portland Cement 42.5N", "cement", ResourceTypesEnum.Materials, Units.Ton, 1m, 110m, 100m, 10, "Demo material."),
            new("Ready-Mix Concrete C25/30", "cement", ResourceTypesEnum.Materials, Units.CubicMeter, 1m, 95m, 90m, 20, "Demo concrete mix."),
            new("Plywood Formwork Board", "timber", ResourceTypesEnum.Materials, Units.SquareMeter, 1m, 18m, 16m, 10, "Demo formwork board."),
            new("Timber Beam 5x10 cm", "timber", ResourceTypesEnum.Materials, Units.Meter, 1m, 7m, 6m, 20, "Demo timber beam."),
            new("Rebar 16 mm", "steel", ResourceTypesEnum.Materials, Units.Ton, 1m, 690m, 650m, 10, "Demo reinforcement bar."),
            new("Welded Wire Mesh", "steel", ResourceTypesEnum.Materials, Units.SquareMeter, 1m, 6m, 5m, 20, "Demo reinforcement mesh."),
            new("Crawler Excavator 20T", "excavation-equipment", ResourceTypesEnum.MachinesAndEquipments, "day", 1m, 650m, 600m, 10, "Demo excavation machine."),
            new("Mini Excavator 5T", "excavation-equipment", ResourceTypesEnum.MachinesAndEquipments, "day", 1m, 420m, 390m, 20, "Demo compact excavation machine."),
            new("Wheel Loader 3m3", "backfilling-equipment", ResourceTypesEnum.MachinesAndEquipments, "day", 1m, 520m, 480m, 10, "Demo loading machine."),
            new("Vibratory Roller 12T", "backfilling-equipment", ResourceTypesEnum.MachinesAndEquipments, "day", 1m, 480m, 450m, 20, "Demo compaction machine."),
            new("Tower Crane 8T", "lifting-equipment", ResourceTypesEnum.MachinesAndEquipments, "day", 1m, 1200m, 1100m, 10, "Demo lifting machine."),
            new("Forklift 3T", "lifting-equipment", ResourceTypesEnum.MachinesAndEquipments, "day", 1m, 300m, 270m, 20, "Demo logistics machine.")
        ];

        private static readonly DemoAttributeSetSpec[] AttributeSetSpecs =
        [
            new("General Information",
            [
                new("Supplier", DataType.Text, true, DefaultTextValue: "Preferred supplier"),
                new("Brand", DataType.Text, true, DefaultTextValue: "Demo brand"),
                new("Lead Time (days)", DataType.Number, true, DefaultNumericValue: 3m, StepValue: 1m, MaxNumericValue: 120m)
            ]),
            new("Technical Specification",
            [
                new("Density (kg/m3)", DataType.Number, true, DefaultNumericValue: 2400m, StepValue: 1m, MaxNumericValue: 5000m),
                new("Strength Class", DataType.Text, true, DefaultTextValue: "C25/30"),
                new("Thickness (mm)", DataType.Number, true, DefaultNumericValue: 100m, StepValue: 1m, MaxNumericValue: 2000m)
            ]),
            new("Execution & Safety",
            [
                new("Crew Size", DataType.Number, true, DefaultNumericValue: 4m, StepValue: 1m, MaxNumericValue: 50m),
                new("Requires Certified Operator", DataType.Text, true, DefaultTextValue: "Yes"),
                new("Inspection Interval (days)", DataType.Number, true, DefaultNumericValue: 7m, StepValue: 1m, MaxNumericValue: 365m)
            ])
        ];

        private static readonly DemoLookupSpec[] ActionSpecs =
        [
            new("Excavation", 10),
            new("Reinforcement", 20),
            new("Formwork", 30),
            new("Concrete Placement", 40),
            new("Backfilling", 50)
        ];

        private static readonly DemoLookupSpec[] LocationSpecs =
        [
            new("Foundation", 10),
            new("Basement", 20),
            new("Ground Floor", 30),
            new("External Works", 40),
            new("Roof", 50)
        ];

        private static readonly DemoLookupSpec[] FallSpecs =
        [
            new("Low Risk", 10),
            new("Medium Risk", 20),
            new("High Risk", 30)
        ];

        private static readonly DemoLookupSpec[] ActionTypeSpecs =
        [
            new("Manual", 10),
            new("Machine Assisted", 20),
            new("Inspection", 30),
            new("Finishing", 40)
        ];

        private static readonly DemoUnitGroupSpec[] UnitGroupSpecs =
        [
            new("Volume & Surface Works", BuildKeys(Units.CubicMeter, Units.SquareMeter, Units.Meter, Units.Piece, Units.Ton, Units.Kilogram)),
            new("Linear Measurements", BuildKeys(Units.Meter, Units.Kilometer, Units.Centimeter, Units.Millimeter))
        ];

        private static readonly DemoTaskSpec[] TaskSpecs =
        [
            new(
                "DEMO-EXC-001",
                "Foundation Excavation",
                "Excavation",
                "Foundation",
                "Medium Risk",
                "Machine Assisted",
                "Volume & Surface Works",
                Units.CubicMeter,
                120m,
                10,
                "Earthworks crew",
                "Sample task for excavation planning.",
                0m,
                0m,
                0m,
                ["equipment", "excavation-equipment", "backfilling-equipment"],
                ["Crawler Excavator 20T", "Wheel Loader 3m3"]),
            new(
                "DEMO-CON-001",
                "Lean Concrete Blinding",
                "Concrete Placement",
                "Foundation",
                "Low Risk",
                "Machine Assisted",
                "Volume & Surface Works",
                Units.CubicMeter,
                45m,
                20,
                "Concrete crew",
                "Sample task for concrete placement.",
                80m,
                0m,
                0m,
                ["building-materials", "cement"],
                ["Portland Cement 42.5N", "Ready-Mix Concrete C25/30"]),
            new(
                "DEMO-REB-001",
                "Footing Reinforcement",
                "Reinforcement",
                "Foundation",
                "Medium Risk",
                "Manual",
                "Volume & Surface Works",
                Units.Ton,
                12m,
                30,
                "Steel fixing crew",
                "Sample task for reinforcement works.",
                0m,
                0m,
                0m,
                ["building-materials", "steel"],
                ["Rebar 16 mm", "Welded Wire Mesh"]),
            new(
                "DEMO-FRM-001",
                "Column Formwork",
                "Formwork",
                "Ground Floor",
                "Medium Risk",
                "Manual",
                "Volume & Surface Works",
                Units.SquareMeter,
                180m,
                40,
                "Carpentry crew",
                "Sample task for shuttering and formwork.",
                150m,
                400m,
                3000m,
                ["building-materials", "timber"],
                ["Plywood Formwork Board", "Timber Beam 5x10 cm"]),
            new(
                "DEMO-BCK-001",
                "Foundation Backfilling",
                "Backfilling",
                "External Works",
                "Low Risk",
                "Machine Assisted",
                "Volume & Surface Works",
                Units.CubicMeter,
                90m,
                50,
                "External works crew",
                "Sample task for backfilling and compaction.",
                0m,
                0m,
                0m,
                ["equipment", "backfilling-equipment"],
                ["Wheel Loader 3m3", "Vibratory Roller 12T"])
        ];

        public async Task<ConstructionDemoSeedResult> SeedAsync(CancellationToken ct = default)
        {
            var result = new ConstructionDemoSeedResult();

            await using var db = await contextFactory.CreateDbContextAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var folders = await EnsureFoldersAsync(db, result, ct);
            var resources = await EnsureResourcesAsync(db, folders, result, ct);

            await EnsureAttributeSetsAsync(db, result, ct);

            var actions = await EnsureLookupsAsync<ActionEntity>(db, ActionSpecs, result, ct);
            var locations = await EnsureLookupsAsync<LocationEntity>(db, LocationSpecs, result, ct);
            var falls = await EnsureLookupsAsync<FallEntity>(db, FallSpecs, result, ct);
            var actionTypes = await EnsureLookupsAsync<ActionTypeEntity>(db, ActionTypeSpecs, result, ct);
            var unitGroups = await EnsureUnitGroupsAsync(db, result, ct);

            await EnsureTasksAsync(db, folders, resources, actions, locations, falls, actionTypes, unitGroups, result, ct);

            await transaction.CommitAsync(ct);
            return result;
        }

        private static async Task<Dictionary<string, ResourceCategory>> EnsureFoldersAsync(
            TaskResourceBlueprintsContext db,
            ConstructionDemoSeedResult result,
            CancellationToken ct)
        {
            var allFolders = await db.ResourceCategories.ToListAsync(ct);
            var foldersByKey = new Dictionary<string, ResourceCategory>(StringComparer.OrdinalIgnoreCase);

            foreach (var spec in FolderSpecs)
            {
                int? parentId = spec.ParentKey is null ? null : foldersByKey[spec.ParentKey].Id;

                var entity = allFolders.FirstOrDefault(x =>
                    x.ParentCategoryId == parentId &&
                    string.Equals(x.DisplayName, spec.DisplayName, StringComparison.OrdinalIgnoreCase));

                if (entity is null)
                {
                    entity = new ResourceCategory
                    {
                        DisplayName = spec.DisplayName,
                        Note = spec.Note,
                        IsVisible = true,
                        SortOrder = spec.SortOrder,
                        ParentCategoryId = parentId
                    };

                    db.ResourceCategories.Add(entity);
                    await db.SaveChangesAsync(ct);

                    allFolders.Add(entity);
                    result.FoldersCreated++;
                }

                foldersByKey[spec.Key] = entity;
            }

            return foldersByKey;
        }

        private static async Task<Dictionary<string, ResourceDefinition>> EnsureResourcesAsync(
            TaskResourceBlueprintsContext db,
            IReadOnlyDictionary<string, ResourceCategory> foldersByKey,
            ConstructionDemoSeedResult result,
            CancellationToken ct)
        {
            var allResources = await db.Resources.ToListAsync(ct);
            var resourcesByName = new Dictionary<string, ResourceDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (var spec in ResourceSpecs)
            {
                var folderId = foldersByKey[spec.FolderKey].Id;

                var entity = allResources.FirstOrDefault(x =>
                    x.FolderId == folderId &&
                    string.Equals(x.Name, spec.Name, StringComparison.OrdinalIgnoreCase));

                if (entity is null)
                {
                    entity = new ResourceDefinition
                    {
                        FolderId = folderId,
                        Name = spec.Name,
                        ResType = spec.ResourceType,
                        SortOrder = spec.SortOrder,
                        IsActive = true,
                        IsVisible = true,
                        AdminNote = spec.Note,
                        Data = BuildResourceMetadata(spec),
                        CalcResCost = BuildCalcResCost(spec)
                    };

                    db.Resources.Add(entity);
                    await db.SaveChangesAsync(ct);

                    allResources.Add(entity);
                    result.ResourcesCreated++;
                }

                resourcesByName[spec.Name] = entity;
            }

            return resourcesByName;
        }

        private static async Task EnsureAttributeSetsAsync(
            TaskResourceBlueprintsContext db,
            ConstructionDemoSeedResult result,
            CancellationToken ct)
        {
            var sets = await db.ResourceAttributeSets
                .Include(x => x.Attributes)
                .ToListAsync(ct);

            var hasChanges = false;

            foreach (var spec in AttributeSetSpecs)
            {
                var set = sets.FirstOrDefault(x =>
                    string.Equals(x.DisplayName, spec.DisplayName, StringComparison.OrdinalIgnoreCase));

                if (set is null)
                {
                    set = new ResourceAttributeSet
                    {
                        DisplayName = spec.DisplayName
                    };

                    db.ResourceAttributeSets.Add(set);
                    await db.SaveChangesAsync(ct);

                    sets.Add(set);
                    result.PropertyGroupsCreated++;
                }

                set.Attributes ??= [];

                foreach (var attrSpec in spec.Attributes)
                {
                    var existingAttribute = set.Attributes.FirstOrDefault(x =>
                        string.Equals(x.DisplayName, attrSpec.DisplayName, StringComparison.OrdinalIgnoreCase));

                    if (existingAttribute is not null)
                    {
                        continue;
                    }

                    var attribute = new ResourceAttribute
                    {
                        AttributeSetId = set.Id,
                        DisplayName = attrSpec.DisplayName,
                        IsUserEditable = attrSpec.IsUserEditable,
                        DataType = attrSpec.DataType,
                        DefaultTextValue = attrSpec.DefaultTextValue,
                        DefaultNumericValue = attrSpec.DefaultNumericValue,
                        StepValue = attrSpec.StepValue,
                        MaxNumericValue = attrSpec.MaxNumericValue
                    };

                    db.ResourceAttributes.Add(attribute);
                    set.Attributes.Add(attribute);

                    result.PropertiesCreated++;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task<Dictionary<string, TEntity>> EnsureLookupsAsync<TEntity>(
            TaskResourceBlueprintsContext db,
            IReadOnlyList<DemoLookupSpec> specs,
            ConstructionDemoSeedResult result,
            CancellationToken ct)
            where TEntity : TaskLookupBase, new()
        {
            var entities = await db.Set<TEntity>().ToListAsync(ct);
            var hasChanges = false;

            foreach (var spec in specs)
            {
                var entity = entities.FirstOrDefault(x =>
                    string.Equals(x.Name, spec.Name, StringComparison.OrdinalIgnoreCase));

                if (entity is not null)
                {
                    continue;
                }

                entity = new TEntity
                {
                    Name = spec.Name,
                    SortOrder = spec.SortOrder,
                    IsVisible = true
                };

                db.Set<TEntity>().Add(entity);
                entities.Add(entity);
                result.LookupsCreated++;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await db.SaveChangesAsync(ct);
            }

            return ToDictionary(entities, x => x.Name);
        }

        private static async Task<Dictionary<string, TaskUnitGroup>> EnsureUnitGroupsAsync(
            TaskResourceBlueprintsContext db,
            ConstructionDemoSeedResult result,
            CancellationToken ct)
        {
            var groups = await db.TaskUnitGroups.ToListAsync(ct);
            var hasChanges = false;

            foreach (var spec in UnitGroupSpecs)
            {
                var group = groups.FirstOrDefault(x =>
                    string.Equals(x.DisplayName, spec.DisplayName, StringComparison.OrdinalIgnoreCase));

                if (group is null)
                {
                    group = new TaskUnitGroup
                    {
                        DisplayName = spec.DisplayName,
                        Keys = spec.Keys.ToList()
                    };

                    db.TaskUnitGroups.Add(group);
                    groups.Add(group);
                    result.UnitGroupsCreated++;
                    hasChanges = true;
                    continue;
                }

                group.Keys ??= [];

                foreach (var key in spec.Keys)
                {
                    if (group.Keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    group.Keys.Add(key);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await db.SaveChangesAsync(ct);
            }

            return ToDictionary(groups, x => x.DisplayName ?? string.Empty);
        }

        private static async Task EnsureTasksAsync(
            TaskResourceBlueprintsContext db,
            IReadOnlyDictionary<string, ResourceCategory> foldersByKey,
            IReadOnlyDictionary<string, ResourceDefinition> resourcesByName,
            IReadOnlyDictionary<string, ActionEntity> actionsByName,
            IReadOnlyDictionary<string, LocationEntity> locationsByName,
            IReadOnlyDictionary<string, FallEntity> fallsByName,
            IReadOnlyDictionary<string, ActionTypeEntity> actionTypesByName,
            IReadOnlyDictionary<string, TaskUnitGroup> unitGroupsByName,
            ConstructionDemoSeedResult result,
            CancellationToken ct)
        {
            var codes = TaskSpecs.Select(x => x.Code).ToList();
            var tasks = await db.Tasks
                .Where(x => x.Code != null && codes.Contains(x.Code))
                .ToListAsync(ct);

            var taskChanges = false;

            foreach (var spec in TaskSpecs)
            {
                var task = tasks.FirstOrDefault(x =>
                    string.Equals(x.Code, spec.Code, StringComparison.OrdinalIgnoreCase));

                var visibleFolderIds = ResolveFolderIds(spec.VisibleFolderKeys, foldersByKey);
                var workloadThresholds = new List<decimal> { spec.Thickness, spec.Width, spec.Length };

                if (task is null)
                {
                    task = new TaskDefinition
                    {
                        Code = spec.Code,
                        Name = spec.DisplayName,
                        SortOrder = spec.SortOrder,
                        IsVisible = true,
                        IsActive = true,
                        Status = TaskStatusEnum.Ready,
                        Responsible = spec.Responsible,
                        FieldNotes = spec.Note,
                        ActionId = actionsByName[spec.ActionName].Id,
                        LocationId = locationsByName[spec.LocationName].Id,
                        FallId = fallsByName[spec.FallName].Id,
                        ActionTypeId = actionTypesByName[spec.ActionTypeName].Id,
                        TaskUnitGroupId = unitGroupsByName[spec.UnitGroupName].Id,
                        UnitCode = spec.UnitCode,
                        Quantity = spec.Quantity,
                        ChangeFactor1 = 1m,
                        ChangeFactor2 = 1m,
                        VisibleFolderIds = visibleFolderIds,
                        WorkloadThresholds = workloadThresholds
                    };

                    db.Tasks.Add(task);
                    tasks.Add(task);
                    result.TasksCreated++;
                    taskChanges = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(task.Name))
                {
                    task.Name = spec.DisplayName;
                    taskChanges = true;
                }

                if (task.SortOrder == 0)
                {
                    task.SortOrder = spec.SortOrder;
                    taskChanges = true;
                }

                if (task.Status == 0)
                {
                    task.Status = TaskStatusEnum.Ready;
                    taskChanges = true;
                }

                if (task.ActionId is null)
                {
                    task.ActionId = actionsByName[spec.ActionName].Id;
                    taskChanges = true;
                }

                if (task.LocationId is null)
                {
                    task.LocationId = locationsByName[spec.LocationName].Id;
                    taskChanges = true;
                }

                if (task.FallId is null)
                {
                    task.FallId = fallsByName[spec.FallName].Id;
                    taskChanges = true;
                }

                if (task.ActionTypeId is null)
                {
                    task.ActionTypeId = actionTypesByName[spec.ActionTypeName].Id;
                    taskChanges = true;
                }

                if (task.TaskUnitGroupId is null)
                {
                    task.TaskUnitGroupId = unitGroupsByName[spec.UnitGroupName].Id;
                    taskChanges = true;
                }

                if (string.IsNullOrWhiteSpace(task.UnitCode))
                {
                    task.UnitCode = spec.UnitCode;
                    taskChanges = true;
                }

                if (task.Quantity is null)
                {
                    task.Quantity = spec.Quantity;
                    taskChanges = true;
                }

                if (string.IsNullOrWhiteSpace(task.Responsible))
                {
                    task.Responsible = spec.Responsible;
                    taskChanges = true;
                }

                if (string.IsNullOrWhiteSpace(task.FieldNotes))
                {
                    task.FieldNotes = spec.Note;
                    taskChanges = true;
                }

                task.VisibleFolderIds ??= [];

                foreach (var folderId in visibleFolderIds)
                {
                    if (task.VisibleFolderIds.Contains(folderId))
                    {
                        continue;
                    }

                    task.VisibleFolderIds.Add(folderId);
                    taskChanges = true;
                }

                if (task.WorkloadThresholds is null || task.WorkloadThresholds.Count < 3)
                {
                    task.WorkloadThresholds = workloadThresholds;
                    taskChanges = true;
                }
            }

            if (taskChanges)
            {
                await db.SaveChangesAsync(ct);
            }

            await EnsureTaskAssignmentsAsync(db, tasks, resourcesByName, result, ct);
        }

        private static async Task EnsureTaskAssignmentsAsync(
            TaskResourceBlueprintsContext db,
            IReadOnlyList<TaskDefinition> tasks,
            IReadOnlyDictionary<string, ResourceDefinition> resourcesByName,
            ConstructionDemoSeedResult result,
            CancellationToken ct)
        {
            var taskIds = tasks.Select(x => x.Id).Where(x => x > 0).ToList();
            if (taskIds.Count == 0)
            {
                return;
            }

            var assignments = await db.TaskResourceAssignments
                .Where(x => taskIds.Contains(x.TaskId))
                .ToListAsync(ct);

            var hasChanges = false;

            foreach (var spec in TaskSpecs)
            {
                var task = tasks.FirstOrDefault(x =>
                    string.Equals(x.Code, spec.Code, StringComparison.OrdinalIgnoreCase));

                if (task is null)
                {
                    continue;
                }

                foreach (var resourceName in spec.ResourceNames)
                {
                    if (!resourcesByName.TryGetValue(resourceName, out var resource))
                    {
                        continue;
                    }

                    var exists = assignments.Any(x => x.TaskId == task.Id && x.ResourceId == resource.Id);
                    if (exists)
                    {
                        continue;
                    }

                    var assignment = new TaskResourceAssignment
                    {
                        TaskId = task.Id,
                        ResourceId = resource.Id,
                        ChangeFactor1 = resource.Data.ChangeFactor1,
                        ChangeFactor2 = resource.Data.ChangeFactor2,
                        CapWaste = resource.Data.CapWaste == 0m ? 1m : resource.Data.CapWaste,
                        BaseCost = resource.Data.BaseCost,
                        IsActive = true
                    };

                    db.TaskResourceAssignments.Add(assignment);
                    assignments.Add(assignment);
                    result.AssignmentsCreated++;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await db.SaveChangesAsync(ct);
            }
        }

        private static ResourceMetadata BuildResourceMetadata(DemoResourceSpec spec)
        {
            var metadata = new ResourceMetadata
            {
                Note = spec.Note ?? string.Empty,
                Quantity = spec.Quantity,
                Unit = spec.Unit,
                ChangeFactor1 = 1m,
                ChangeFactor2 = 1m,
                CapWaste = 1m,
                Cap = 1m,
                Waste = 1m,
                Cost = spec.Cost,
                BaseCost = spec.BaseCost
            };

            metadata.Normalize();
            return metadata;
        }

        private static CalcResCost BuildCalcResCost(DemoResourceSpec spec)
        {
            if (spec.ResourceType != ResourceTypesEnum.MachinesAndEquipments)
            {
                return new CalcResCost();
            }

            return new CalcResCost
            {
                Quantity = spec.Quantity,
                Day = 1,
                DayCost = spec.Cost
            };
        }

        private static List<int> ResolveFolderIds(
            IEnumerable<string> keys,
            IReadOnlyDictionary<string, ResourceCategory> foldersByKey)
        {
            var ids = new List<int>();

            foreach (var key in keys)
            {
                if (!foldersByKey.TryGetValue(key, out var folder))
                {
                    continue;
                }

                if (ids.Contains(folder.Id))
                {
                    continue;
                }

                ids.Add(folder.Id);
            }

            return ids;
        }

        private static Dictionary<string, TEntity> ToDictionary<TEntity>(
            IEnumerable<TEntity> entities,
            Func<TEntity, string> keySelector)
        {
            var dictionary = new Dictionary<string, TEntity>(StringComparer.OrdinalIgnoreCase);

            foreach (var entity in entities)
            {
                var key = keySelector(entity);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                dictionary[key] = entity;
            }

            return dictionary;
        }

        private static string[] BuildKeys(params string[] units) =>
            units
                .Select(UnitRulesCatalog.BuildKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        private sealed record DemoFolderSpec(string Key, string DisplayName, string? ParentKey, int SortOrder, string? Note);

        private sealed record DemoResourceSpec(
            string Name,
            string FolderKey,
            ResourceTypesEnum ResourceType,
            string Unit,
            decimal Quantity,
            decimal Cost,
            decimal? BaseCost,
            int SortOrder,
            string? Note);

        private sealed record DemoAttributeSetSpec(string DisplayName, IReadOnlyList<DemoAttributeSpec> Attributes);

        private sealed record DemoAttributeSpec(
            string DisplayName,
            DataType DataType,
            bool IsUserEditable = true,
            string? DefaultTextValue = null,
            decimal? DefaultNumericValue = null,
            decimal? StepValue = null,
            decimal? MaxNumericValue = null);

        private sealed record DemoLookupSpec(string Name, int SortOrder);

        private sealed record DemoUnitGroupSpec(string DisplayName, IReadOnlyList<string> Keys);

        private sealed record DemoTaskSpec(
            string Code,
            string DisplayName,
            string ActionName,
            string LocationName,
            string FallName,
            string ActionTypeName,
            string UnitGroupName,
            string UnitCode,
            decimal Quantity,
            int SortOrder,
            string Responsible,
            string Note,
            decimal Thickness,
            decimal Width,
            decimal Length,
            IReadOnlyList<string> VisibleFolderKeys,
            IReadOnlyList<string> ResourceNames);
    }
}
