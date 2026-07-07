using Application.Feature.Application.Commands;
using Application.Feature.Identity.Department.Queries;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.ApplicationTemplate
{
    public partial class ApplicationFormUI
    {
        [Parameter] public ApplicationDTO ApplicationUpdate { get; set; } = new();
        [Parameter] public EventCallback<bool> Callback { get; set; }

        private List<ListDTO> Departments = [];
        private bool IsLoading;
        private bool _isEnsuringTemplateShape;
        private string? _saveError;

        // Local, editor-only UI state (not persisted): collapsible major panels + per-section collapse.
        private bool _infoOpen = true;
        private bool _structureOpen = true;
        private bool _columnsOpen = true;
        private readonly HashSet<Guid> _collapsedSections = [];
        private readonly HashSet<Guid> _openColumnEditors = [];
        private Guid? _conditionEditRowId;
        private bool _collapseInitialized;

        private bool IsSectionExpanded(SelfInspectionSectionData section) => !_collapsedSections.Contains(section.Id);

        private void ToggleSection(SelfInspectionSectionData section)
        {
            if (!_collapsedSections.Remove(section.Id))
                _collapsedSections.Add(section.Id);
        }

        private bool IsColumnsEditorOpen(SelfInspectionSectionData section) => _openColumnEditors.Contains(section.Id);

        private void ToggleColumnsEditor(SelfInspectionSectionData section)
        {
            if (!_openColumnEditors.Remove(section.Id))
                _openColumnEditors.Add(section.Id);
        }

        private void ToggleRowCondition(RowDTO row)
            => _conditionEditRowId = _conditionEditRowId == row.ID ? null : row.ID;

        // Seed the editor's collapse state once from each section's "Kollapsad som standard" flag.
        private void InitCollapseState()
        {
            if (_collapseInitialized)
                return;

            foreach (var section in ApplicationUpdate.Data.Sections)
                if (section.CollapsedByDefault)
                    _collapsedSections.Add(section.Id);

            _collapseInitialized = true;
        }

        private const string SaveFailedMessage =
            "Det gick inte att spara egenkontrollmallen. Kontrollera obligatoriska fält och försök igen.";

        private static readonly IReadOnlyList<string> TemplateTypes =
        [
            SelfInspectionTemplateTypes.Checklist,
            SelfInspectionTemplateTypes.Handover,
            SelfInspectionTemplateTypes.RiskAnalysis
        ];

        private static readonly IReadOnlyList<string> FieldTypes =
        [
            "Text",
            "Lång text",
            "Tal",
            "Datum",
            "Datum och tid",
            "Ja/Nej",
            "Checkbox",
            "Dropdown",
            "Person / ansvarig",
            "Beräknat fält"
        ];

        // "Alla avdelningar" is the first choice in the department dropdown (value 0).
        private int DepartmentSelection
        {
            get => ApplicationUpdate.Data.AllDepartments ? 0 : ApplicationUpdate.DepartmentId;
            set
            {
                if (value <= 0)
                {
                    ApplicationUpdate.Data.AllDepartments = true;
                }
                else
                {
                    ApplicationUpdate.Data.AllDepartments = false;
                    ApplicationUpdate.DepartmentId = value;
                }
            }
        }

        private IReadOnlyList<SelfInspectionSectionData> SectionsForUi
        {
            get
            {
                if (!_isEnsuringTemplateShape)
                    EnsureTemplateShape();
                return ApplicationUpdate.Data.Sections
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Title)
                    .ToList();
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            Departments = await Dispatcher.Send(new GetDepartmentsAsListQuery()) ?? [];

            ApplicationUpdate.Data ??= new ApplicationDataDTO();
            ApplicationUpdate.Data.Rows ??= [];

            if (string.IsNullOrWhiteSpace(ApplicationUpdate.Data.TemplateType))
                ApplicationUpdate.Data.TemplateType = SelfInspectionTemplateTypes.Checklist;
            if (string.IsNullOrWhiteSpace(ApplicationUpdate.Data.Purpose))
                ApplicationUpdate.Data.Purpose = ApplicationUpdate.Data.Description ?? string.Empty;
            // Koppling has been removed from the UI; keep a stable value for backward compatibility.
            if (string.IsNullOrWhiteSpace(ApplicationUpdate.Data.LinkType))
                ApplicationUpdate.Data.LinkType = "Kalkyl";

            if (ApplicationUpdate.Id == 0 && ApplicationUpdate.DepartmentId <= 0)
            {
                var firstDepartmentId = Departments.FirstOrDefault()?.Id;
                if (firstDepartmentId.HasValue)
                    ApplicationUpdate.DepartmentId = firstDepartmentId.Value;
            }

            EnsureTemplateShape();
            InitCollapseState();
        }

        private void OnTemplateTypeChanged(string value)
        {
            if (string.Equals(ApplicationUpdate.Data.TemplateType, value, StringComparison.OrdinalIgnoreCase))
                return;

            var departmentId = ApplicationUpdate.DepartmentId;
            ApplicationDTO template = value switch
            {
                SelfInspectionTemplateTypes.RiskAnalysis => SelfInspectionStandardTemplates.CreateRiskAnalysis(departmentId),
                SelfInspectionTemplateTypes.Handover => SelfInspectionStandardTemplates.CreateHandover(departmentId),
                _ => SelfInspectionStandardTemplates.CreateChecklist(departmentId)
            };

            var keepName = ApplicationUpdate.Name;
            var keepVisible = ApplicationUpdate.IsVisible;
            var keepDepartment = ApplicationUpdate.DepartmentId;
            var keepLink = ApplicationUpdate.Data.LinkType;
            var keepAllDepartments = ApplicationUpdate.Data.AllDepartments;
            var keepRequired = ApplicationUpdate.Data.RequiredBeforeOffer;
            var keepSortOrder = ApplicationUpdate.Data.SortOrder;

            ApplicationUpdate.Data = template.Data;
            ApplicationUpdate.Data.LinkType = keepLink;
            ApplicationUpdate.Data.AllDepartments = keepAllDepartments;
            ApplicationUpdate.Data.RequiredBeforeOffer = keepRequired;
            ApplicationUpdate.Data.SortOrder = keepSortOrder;
            ApplicationUpdate.DepartmentId = keepDepartment;
            ApplicationUpdate.IsVisible = keepVisible;
            if (!string.IsNullOrWhiteSpace(keepName))
                ApplicationUpdate.Name = keepName;

            EnsureTemplateShape();
        }

        private IEnumerable<RowDTO> RowsForSection(SelfInspectionSectionData section)
            => ApplicationUpdate.Data.Rows
                .Where(x => x.SectionId == section.Id || (x.SectionId == Guid.Empty && SameText(GetLegacySectionTitle(x), section.Title)))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name);

        // ---- Standard/default answer columns -------------------------------------------------

        private static List<AttributeDTO> StandardDefaultColumns() =>
        [
            new() { ID = Guid.NewGuid(), Label = "Notering", FieldTypeLabel = "Text", AttributeType = AttributeType.Text, Order = 0 },
            new() { ID = Guid.NewGuid(), Label = "Ansvarig", FieldTypeLabel = "Person / ansvarig", AttributeType = AttributeType.Text, Order = 1 },
            new() { ID = Guid.NewGuid(), Label = "Datum", FieldTypeLabel = "Datum", AttributeType = AttributeType.Date, Order = 2 },
            new() { ID = Guid.NewGuid(), Label = "Klart", FieldTypeLabel = "Checkbox", AttributeType = AttributeType.Bool, Order = 3 }
        ];

        private List<AttributeDTO> DefaultColumns
        {
            get
            {
                if (!_isEnsuringTemplateShape)
                    EnsureTemplateShape();
                return ApplicationUpdate.Data.DefaultColumns;
            }
        }

        private List<AttributeDTO> SectionColumns(SelfInspectionSectionData section)
        {
            if (!_isEnsuringTemplateShape)
                EnsureTemplateShape();
            return section.Columns.OrderBy(x => x.Order).ToList();
        }

        private void AddSection()
        {
            var next = ApplicationUpdate.Data.Sections.Count + 1;
            var section = new SelfInspectionSectionData
            {
                Id = Guid.NewGuid(),
                Title = $"Ny sektion {next}",
                SortOrder = next,
                IsVisible = true,
                // New sections start with the template's standard columns; admin can change them.
                Columns = DefaultColumns.Select(CloneColumnDefinition).ToList()
            };

            ApplicationUpdate.Data.Sections.Add(section);
            AddCheckpoint(section);
        }

        private void RemoveSection(SelfInspectionSectionData section)
        {
            ApplicationUpdate.Data.Rows.RemoveAll(x => x.SectionId == section.Id || SameText(x.SectionTitle, section.Title));
            ApplicationUpdate.Data.Sections.Remove(section);
            if (ApplicationUpdate.Data.Sections.Count == 0)
                AddSection();
        }

        private void AddCheckpoint(SelfInspectionSectionData section)
        {
            ApplicationUpdate.Data.Rows.Add(new RowDTO
            {
                ID = Guid.NewGuid(),
                Name = "Ny kontrollpunkt",
                SectionId = section.Id,
                SectionTitle = section.Title,
                Description = section.Title,
                SortOrder = RowsForSection(section).Count() + 1,
                IsVisible = true,
                Attributes = section.Columns.Select(CloneColumnForRow).ToList()
            });
        }

        private void RemoveCheckpoint(RowDTO row)
        {
            ApplicationUpdate.Data.Rows.Remove(row);
        }

        // ---- Column editing (per section, or the template defaults when section is null) ------

        private void AddColumn(SelfInspectionSectionData? section)
        {
            var target = section?.Columns ?? ApplicationUpdate.Data.DefaultColumns;
            target.Add(new AttributeDTO
            {
                ID = Guid.NewGuid(),
                Label = "Ny kolumn",
                AttributeType = AttributeType.Text,
                FieldTypeLabel = "Text",
                Order = target.Count
            });

            if (section is not null)
                SyncSectionRows(section);
        }

        private void RemoveColumn(SelfInspectionSectionData? section, AttributeDTO column)
        {
            var target = section?.Columns ?? ApplicationUpdate.Data.DefaultColumns;
            target.Remove(column);
            ReorderColumns(target);

            if (section is not null)
                SyncSectionRows(section);
        }

        private void MoveColumn(SelfInspectionSectionData? section, AttributeDTO column, int direction)
        {
            var target = section?.Columns ?? ApplicationUpdate.Data.DefaultColumns;
            var ordered = target.OrderBy(x => x.Order).ToList();
            if (!Swap(ordered, column, direction))
                return;

            for (var i = 0; i < ordered.Count; i++)
                ordered[i].Order = i;

            if (section is not null)
                SyncSectionRows(section);
        }

        private bool CanMoveColumn(SelfInspectionSectionData? section, AttributeDTO column, int direction)
        {
            var target = (section?.Columns ?? ApplicationUpdate.Data.DefaultColumns).OrderBy(x => x.Order).ToList();
            var index = target.FindIndex(x => x.ID == column.ID);
            var t = index + direction;
            return index >= 0 && t >= 0 && t < target.Count;
        }

        private void SetColumnFieldType(SelfInspectionSectionData? section, AttributeDTO column, string? fieldType)
        {
            fieldType = string.IsNullOrWhiteSpace(fieldType) ? "Text" : fieldType;
            column.FieldTypeLabel = fieldType;
            column.AttributeType = ToAttributeType(fieldType);
            column.IsComputed = fieldType == "Beräknat fält";

            if (fieldType == "Dropdown" && column.Options.Count == 0)
                column.Options = ["Alternativ A", "Alternativ B"];

            if (section is not null)
                SyncSectionRows(section);
        }

        private void AddDropdownOption(SelfInspectionSectionData? section, AttributeDTO column)
        {
            column.Options.Add($"Alternativ {(char)('A' + Math.Min(column.Options.Count, 25))}");
            if (section is not null)
                SyncSectionRows(section);
        }

        private void RemoveDropdownOption(SelfInspectionSectionData? section, AttributeDTO column, int index)
        {
            if (index >= 0 && index < column.Options.Count)
                column.Options.RemoveAt(index);
            if (section is not null)
                SyncSectionRows(section);
        }

        private void SetDropdownOption(AttributeDTO column, int index, string? value)
        {
            if (index >= 0 && index < column.Options.Count)
                column.Options[index] = value ?? string.Empty;
        }

        private static bool IsDropdown(AttributeDTO column)
            => string.Equals(column.FieldTypeLabel, "Dropdown", StringComparison.OrdinalIgnoreCase)
               || column.AttributeType == AttributeType.Select;

        private static bool IsCheckbox(AttributeDTO column)
            => column.AttributeType == AttributeType.Bool;

        // Row attributes mirror the section's column definitions (by order).
        private void SyncSectionRows(SelfInspectionSectionData section)
        {
            var columns = section.Columns.OrderBy(x => x.Order).ToList();
            foreach (var row in RowsForSection(section).ToList())
                row.Attributes = MergeColumns(row.Attributes, columns);
        }

        private static string DisplayFieldType(AttributeDTO column)
            => !string.IsNullOrWhiteSpace(column.FieldTypeLabel)
                ? column.FieldTypeLabel
                : ToFieldTypeLabel(column.AttributeType);

        // ---- Visibility conditions (simple, first version) ------------------------------------

        internal sealed record ConditionColumnChoice(Guid ColumnId, string Label, bool IsCheckboxColumn, List<string> Options);

        // Candidate columns for conditions: every Dropdown/Checkbox column across all sections.
        private List<ConditionColumnChoice> ConditionColumnChoices()
        {
            var result = new List<ConditionColumnChoice>();
            foreach (var section in ApplicationUpdate.Data.Sections.OrderBy(x => x.SortOrder))
            {
                foreach (var col in section.Columns.OrderBy(x => x.Order))
                {
                    if (IsDropdown(col))
                        result.Add(new ConditionColumnChoice(col.ID, $"{section.Title} — {col.Label}", false, col.Options.ToList()));
                    else if (IsCheckbox(col))
                        result.Add(new ConditionColumnChoice(col.ID, $"{section.Title} — {col.Label}", true, []));
                }
            }

            return result;
        }

        private void SetConditionMode(Action<SelfInspectionVisibilityCondition?> setter, string? mode)
        {
            if (mode == "always")
            {
                setter(null);
                return;
            }

            var first = ConditionColumnChoices().FirstOrDefault();
            setter(new SelfInspectionVisibilityCondition
            {
                ColumnId = first?.ColumnId ?? Guid.Empty,
                ColumnLabel = first?.Label ?? string.Empty,
                Operator = first is { IsCheckboxColumn: true } ? "checked" : "equals",
                Value = first?.Options.FirstOrDefault() ?? string.Empty
            });
        }

        private void SetConditionColumn(SelfInspectionVisibilityCondition condition, string? columnId)
        {
            if (!Guid.TryParse(columnId, out var id))
                return;

            var choice = ConditionColumnChoices().FirstOrDefault(x => x.ColumnId == id);
            if (choice is null)
                return;

            condition.ColumnId = choice.ColumnId;
            condition.ColumnLabel = choice.Label;
            condition.Operator = choice.IsCheckboxColumn ? "checked" : "equals";
            condition.Value = choice.IsCheckboxColumn ? string.Empty : choice.Options.FirstOrDefault() ?? string.Empty;
        }

        private static string ConditionSummary(SelfInspectionVisibilityCondition? condition)
            => condition is null
                ? "Alltid synlig"
                : condition.Operator switch
                {
                    "checked" => $"Visas om {condition.ColumnLabel} är markerad",
                    "notchecked" => $"Visas om {condition.ColumnLabel} inte är markerad",
                    _ => $"Visas om {condition.ColumnLabel} = {condition.Value}"
                };

        // ---- Save ------------------------------------------------------------------------------

        private async Task HandleSubmitAsync()
        {
            if (IsLoading)
                return;

            _saveError = null;

            if (ApplicationUpdate.Data.IsSystemTemplate)
            {
                _saveError = "Standardmallar kan inte ändras. Kopiera mallen för att skapa en egen version.";
                MHD.Notifications(ApplicationUpdate.Id == 0 ? ToastType.Add : ToastType.Update, false);
                return;
            }

            if (!ApplicationUpdate.Data.AllDepartments && ApplicationUpdate.DepartmentId <= 0)
            {
                _saveError = "Välj en avdelning (eller \"Alla avdelningar\") innan du sparar.";
                return;
            }

            var validationErrors = ValidateTemplate();
            if (validationErrors.Count > 0)
            {
                _saveError = string.Join(" ", validationErrors.Distinct());
                return;
            }

            IsLoading = true;
            var isSuccess = false;

            try
            {
                ApplicationUpdate.Name = (ApplicationUpdate.Name ?? string.Empty).Trim();
                ApplicationUpdate.Data ??= new ApplicationDataDTO();
                ApplicationUpdate.Data.Rows ??= [];
                ApplicationUpdate.Data.Description = ApplicationUpdate.Data.Purpose ?? ApplicationUpdate.Data.Description ?? string.Empty;

                NormalizeBeforeSave();

                if (ApplicationUpdate.Id == 0)
                    isSuccess = await Dispatcher.Send(new CreateApplicationCommand(ApplicationUpdate)) > 0;
                else
                    isSuccess = await Dispatcher.Send(new UpdateApplicationCommand(ApplicationUpdate));
            }
            catch (Exception)
            {
                // Never let a backend/transport failure surface as a raw error page — show it in the form.
                isSuccess = false;
            }
            finally
            {
                IsLoading = false;
            }

            MHD.Notifications(ApplicationUpdate.Id == 0 ? ToastType.Add : ToastType.Update, isSuccess);
            if (isSuccess)
                await Callback.InvokeAsync(true);
            else
                _saveError = SaveFailedMessage;
        }

        private void EnsureTemplateShape()
        {
            if (_isEnsuringTemplateShape)
                return;

            _isEnsuringTemplateShape = true;
            try
            {
                ApplicationUpdate.Data ??= new ApplicationDataDTO();
                ApplicationUpdate.Data.Rows ??= [];
                ApplicationUpdate.Data.Sections ??= [];
                ApplicationUpdate.Data.DefaultColumns ??= [];

                if (ApplicationUpdate.Data.Sections.Count == 0)
                    BuildSectionsFromRows();

                if (ApplicationUpdate.Data.Sections.Count == 0)
                {
                    ApplicationUpdate.Data.Sections.Add(new SelfInspectionSectionData
                    {
                        Id = Guid.NewGuid(),
                        Title = "Allmänt",
                        SortOrder = 1,
                        IsVisible = true
                    });
                }

                foreach (var section in ApplicationUpdate.Data.Sections)
                {
                    if (section.Id == Guid.Empty)
                        section.Id = Guid.NewGuid();
                    if (string.IsNullOrWhiteSpace(section.Title))
                        section.Title = "Sektion";
                    section.Columns ??= [];
                }

                foreach (var row in ApplicationUpdate.Data.Rows)
                {
                    if (row.ID == Guid.Empty)
                        row.ID = Guid.NewGuid();
                    var section = ResolveSection(row);
                    row.SectionId = section.Id;
                    row.SectionTitle = section.Title;
                    row.Description = string.IsNullOrWhiteSpace(row.Description) ? section.Title : row.Description;
                    row.Attributes ??= [];
                    foreach (var attr in row.Attributes)
                        NormalizeColumn(attr);
                }

                // Legacy templates: sections without column definitions inherit them from their rows
                // (previously all rows mirrored one global column set).
                foreach (var section in ApplicationUpdate.Data.Sections)
                {
                    if (section.Columns.Count > 0)
                        continue;

                    var seed = RowsForSection(section).FirstOrDefault()?.Attributes
                        ?? ApplicationUpdate.Data.Rows.OrderBy(x => x.SortOrder).FirstOrDefault()?.Attributes;

                    section.Columns = seed is { Count: > 0 }
                        ? seed.OrderBy(x => x.Order).Select(CloneColumnDefinition).ToList()
                        : (ApplicationUpdate.Data.DefaultColumns.Count > 0
                            ? ApplicationUpdate.Data.DefaultColumns.Select(CloneColumnDefinition).ToList()
                            : StandardDefaultColumns());
                }

                if (ApplicationUpdate.Data.DefaultColumns.Count == 0)
                {
                    var firstSection = ApplicationUpdate.Data.Sections.OrderBy(x => x.SortOrder).First();
                    ApplicationUpdate.Data.DefaultColumns = firstSection.Columns.Count > 0
                        ? firstSection.Columns.Select(CloneColumnDefinition).ToList()
                        : StandardDefaultColumns();
                }

                foreach (var section in ApplicationUpdate.Data.Sections)
                {
                    foreach (var col in section.Columns)
                        NormalizeColumn(col);
                    ReorderColumns(section.Columns);
                }

                foreach (var col in ApplicationUpdate.Data.DefaultColumns)
                    NormalizeColumn(col);
                ReorderColumns(ApplicationUpdate.Data.DefaultColumns);

                if (ApplicationUpdate.Data.Rows.Count == 0)
                    AddCheckpoint(ApplicationUpdate.Data.Sections.OrderBy(x => x.SortOrder).First());

                foreach (var section in ApplicationUpdate.Data.Sections)
                    SyncSectionRows(section);
            }
            finally
            {
                _isEnsuringTemplateShape = false;
            }
        }

        private void BuildSectionsFromRows()
        {
            var legacyTitles = ApplicationUpdate.Data.Rows
                .Select(GetLegacySectionTitle)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (var i = 0; i < legacyTitles.Count; i++)
            {
                ApplicationUpdate.Data.Sections.Add(new SelfInspectionSectionData
                {
                    Id = Guid.NewGuid(),
                    Title = legacyTitles[i],
                    SortOrder = i + 1,
                    IsVisible = true
                });
            }
        }

        private void NormalizeBeforeSave()
        {
            foreach (var section in ApplicationUpdate.Data.Sections)
            {
                section.Title = (section.Title ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(section.Title))
                    section.Title = "Sektion";

                var columns = section.Columns.OrderBy(x => x.Order).ToList();
                for (var i = 0; i < columns.Count; i++)
                {
                    NormalizeColumn(columns[i]);
                    columns[i].Order = i;
                }
            }

            foreach (var row in ApplicationUpdate.Data.Rows)
            {
                var section = ResolveSection(row);
                row.Name = string.IsNullOrWhiteSpace(row.Name) ? "Kontrollpunkt" : row.Name.Trim();
                row.SectionId = section.Id;
                row.SectionTitle = section.Title;
                row.Description = section.Title;
                row.Attributes = MergeColumns(row.Attributes, section.Columns.OrderBy(x => x.Order).ToList());
            }
        }

        private List<AttributeDTO> MergeColumns(List<AttributeDTO> existing, List<AttributeDTO> templateColumns)
        {
            var orderedExisting = existing.OrderBy(x => x.Order).ToList();
            var merged = new List<AttributeDTO>();

            for (var i = 0; i < templateColumns.Count; i++)
            {
                var template = templateColumns[i];
                // Prefer matching by the column link so values follow their column when reordered.
                var target = orderedExisting.FirstOrDefault(x => x.TemplateColumnId == template.ID && template.ID != Guid.Empty)
                    ?? (i < orderedExisting.Count ? orderedExisting[i] : new AttributeDTO { ID = Guid.NewGuid() });
                target.Label = template.Label;
                target.FieldKey = template.FieldKey;
                target.FieldTypeLabel = template.FieldTypeLabel;
                target.AttributeType = template.AttributeType;
                target.Required = template.Required;
                target.IsComputed = template.IsComputed;
                target.Validation = template.Validation;
                target.Style = template.Style;
                target.Options = template.Options.ToList();
                target.TemplateColumnId = template.ID;
                target.Order = i;
                merged.Add(target);
            }

            return merged;
        }

        private SelfInspectionSectionData ResolveSection(RowDTO row)
        {
            if (row.SectionId != Guid.Empty)
            {
                var byId = ApplicationUpdate.Data.Sections.FirstOrDefault(x => x.Id == row.SectionId);
                if (byId is not null)
                    return byId;
            }

            var title = GetLegacySectionTitle(row);
            var section = ApplicationUpdate.Data.Sections.FirstOrDefault(x => SameText(x.Title, title));
            if (section is not null)
                return section;

            section = ApplicationUpdate.Data.Sections.OrderBy(x => x.SortOrder).First();
            return section;
        }

        // Clone used for section/default column DEFINITIONS (keeps a fresh identity).
        private static AttributeDTO CloneColumnDefinition(AttributeDTO source)
        {
            var clone = source.Clone();
            clone.ID = Guid.NewGuid();
            clone.TemplateColumnId = Guid.Empty;
            NormalizeColumn(clone);
            return clone;
        }

        // Clone used for ROW attributes mirroring a section column (keeps the template link).
        private static AttributeDTO CloneColumnForRow(AttributeDTO source)
        {
            var clone = source.Clone();
            clone.TemplateColumnId = source.ID;
            clone.ID = Guid.NewGuid();
            NormalizeColumn(clone);
            return clone;
        }

        private static void NormalizeColumn(AttributeDTO attr)
        {
            if (attr.ID == Guid.Empty)
                attr.ID = Guid.NewGuid();
            attr.Options ??= [];
            if (string.IsNullOrWhiteSpace(attr.Label))
                attr.Label = TryReadStylePart(attr.Style, "label") ?? attr.AttributeType.ToString();
            if (string.IsNullOrWhiteSpace(attr.FieldKey))
                attr.FieldKey = TryReadStylePart(attr.Style, "key") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(attr.FieldTypeLabel))
                attr.FieldTypeLabel = ToFieldTypeLabel(attr.AttributeType);
            attr.AttributeType = ToAttributeType(attr.FieldTypeLabel);
            attr.IsComputed = attr.FieldTypeLabel == "Beräknat fält" || attr.FieldKey is RiskAnalysisFieldKeys.RiskValue or RiskAnalysisFieldKeys.RiskLevel;
        }

        private static void ReorderColumns(List<AttributeDTO> attributes)
        {
            var ordered = attributes.OrderBy(x => x.Order).ToList();
            for (var i = 0; i < ordered.Count; i++)
                ordered[i].Order = i;
        }

        private static AttributeType ToAttributeType(string fieldType) => fieldType switch
        {
            "Lång text" => AttributeType.TextArea,
            "Tal" => AttributeType.Int,
            "Datum" => AttributeType.Date,
            "Datum och tid" => AttributeType.DateTime,
            "Ja/Nej" => AttributeType.Bool,
            "Checkbox" => AttributeType.Bool,
            "Dropdown" => AttributeType.Select,
            "Person / ansvarig" => AttributeType.Text,
            "Beräknat fält" => AttributeType.Text,
            _ => AttributeType.Text
        };

        private static string ToFieldTypeLabel(AttributeType type) => type switch
        {
            AttributeType.TextArea => "Lång text",
            AttributeType.Int or AttributeType.Double => "Tal",
            AttributeType.Date => "Datum",
            AttributeType.DateTime => "Datum och tid",
            AttributeType.Bool => "Checkbox",
            AttributeType.Select => "Dropdown",
            _ => "Text"
        };

        private static string GetLegacySectionTitle(RowDTO row)
            => !string.IsNullOrWhiteSpace(row.SectionTitle)
                ? row.SectionTitle
                : (!string.IsNullOrWhiteSpace(row.Description) ? row.Description : "Allmänt");

        private static bool SameText(string? left, string? right)
            => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

        private static string? TryReadStylePart(string? style, string key)
        {
            if (string.IsNullOrWhiteSpace(style))
                return null;

            var prefix = $"{key}:";
            var part = style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            return part is null ? null : part[prefix.Length..].Trim();
        }

        // ---- Ordering via buttons (admin never edits sort numbers by hand) ----

        private bool CanMoveSection(SelfInspectionSectionData section, int direction)
        {
            var ordered = SectionsForUi;
            var index = ordered.ToList().FindIndex(x => x.Id == section.Id);
            var target = index + direction;
            return index >= 0 && target >= 0 && target < ordered.Count;
        }

        private void MoveSection(SelfInspectionSectionData section, int direction)
        {
            var ordered = ApplicationUpdate.Data.Sections
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .ToList();

            if (!Swap(ordered, section, direction))
                return;

            for (var i = 0; i < ordered.Count; i++)
                ordered[i].SortOrder = i + 1;
        }

        private bool CanMoveRow(RowDTO row, int direction)
        {
            var section = ResolveSection(row);
            var ordered = RowsForSection(section).ToList();
            var index = ordered.FindIndex(x => x.ID == row.ID);
            var target = index + direction;
            return index >= 0 && target >= 0 && target < ordered.Count;
        }

        private void MoveRow(RowDTO row, int direction)
        {
            var section = ResolveSection(row);
            var ordered = RowsForSection(section).ToList();

            if (!Swap(ordered, row, direction))
                return;

            for (var i = 0; i < ordered.Count; i++)
                ordered[i].SortOrder = i + 1;
        }

        private static bool Swap<T>(List<T> list, T item, int direction)
        {
            var index = list.IndexOf(item);
            var target = index + direction;
            if (index < 0 || target < 0 || target >= list.Count)
                return false;

            (list[index], list[target]) = (list[target], list[index]);
            return true;
        }

        // Swedish, human-readable validation so a broken/empty template is stopped in the form instead of
        // being auto-"fixed" silently or bounced back as an HTTP 400.
        private List<string> ValidateTemplate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ApplicationUpdate.Name))
                errors.Add("Namn är obligatoriskt.");

            if (ApplicationUpdate.Data.Sections.Count == 0)
                errors.Add("Minst en sektion krävs.");

            if (ApplicationUpdate.Data.Sections.Any(s => string.IsNullOrWhiteSpace(s.Title)))
                errors.Add("Sektionen saknar namn.");

            if (ApplicationUpdate.Data.Rows.Any(r => string.IsNullOrWhiteSpace(r.Name)))
                errors.Add("Kontrollpunkten saknar text.");

            if (ApplicationUpdate.Data.Sections.Any(s => s.Columns.Any(c => string.IsNullOrWhiteSpace(c.Label))))
                errors.Add("Svarskolumnen saknar namn.");

            if (ApplicationUpdate.Data.Sections.Any(s => s.Columns.Any(c => IsDropdown(c) && c.Options.All(string.IsNullOrWhiteSpace))))
                errors.Add("Dropdown-kolumnen saknar alternativ.");

            return errors;
        }

        private void Preview()
        {
            // Preview a snapshot (Data setter clones) so the interactive preview never mutates the
            // working copy the admin is still editing.
            var snapshot = new ApplicationDTO
            {
                Id = ApplicationUpdate.Id,
                DepartmentId = ApplicationUpdate.DepartmentId,
                Name = ApplicationUpdate.Name,
                IsVisible = ApplicationUpdate.IsVisible,
                UserId = ApplicationUpdate.UserId,
                LastUpdate = ApplicationUpdate.LastUpdate,
                Data = ApplicationUpdate.Data
            };

            MHD.Modal.ShowComponent<ApplicationPreviewUI>(
                $"Förhandsgranskning – {(string.IsNullOrWhiteSpace(snapshot.Name) ? "Namnlös mall" : snapshot.Name)}",
                new Dictionary<string, object> { [nameof(ApplicationPreviewUI.Application)] = snapshot },
                BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);
        }

        private Task CancelAsync() => Callback.InvokeAsync(false);
    }
}
