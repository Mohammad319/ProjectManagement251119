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
        private bool _collapseInitialized;

        private bool IsSectionExpanded(SelfInspectionSectionData section) => !_collapsedSections.Contains(section.Id);

        private void ToggleSection(SelfInspectionSectionData section)
        {
            if (!_collapsedSections.Remove(section.Id))
                _collapsedSections.Add(section.Id);
        }

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

        private static readonly IReadOnlyList<string> LinkTypes =
        [
            "Projekt",
            "Kalkyl",
            "Anbud",
            "Överlämning"
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

        private List<AttributeDTO> ResponseColumns
        {
            get
            {
                if (!_isEnsuringTemplateShape)
                    EnsureTemplateShape();

                return GetResponseColumnsForCurrentState();
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

        private void AddSection()
        {
            var next = ApplicationUpdate.Data.Sections.Count + 1;
            var section = new SelfInspectionSectionData
            {
                Id = Guid.NewGuid(),
                Title = $"Ny sektion {next}",
                SortOrder = next,
                IsVisible = true
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
            var columns = GetResponseColumnsForCurrentState().Select(CloneColumnForRow).ToList();
            if (columns.Count == 0)
                columns = DefaultColumnsForCurrentType().Select(CloneColumnForRow).ToList();

            ApplicationUpdate.Data.Rows.Add(new RowDTO
            {
                ID = Guid.NewGuid(),
                Name = "Ny kontrollpunkt",
                SectionId = section.Id,
                SectionTitle = section.Title,
                Description = section.Title,
                SortOrder = RowsForSection(section).Count() + 1,
                IsVisible = true,
                Attributes = columns
            });
        }

        private void RemoveCheckpoint(RowDTO row)
        {
            ApplicationUpdate.Data.Rows.Remove(row);
        }

        private void AddResponseColumn()
        {
            var index = ResponseColumns.Count;
            var column = new AttributeDTO
            {
                ID = Guid.NewGuid(),
                Label = "Ny kolumn",
                AttributeType = AttributeType.Text,
                FieldTypeLabel = "Text",
                Order = index
            };

            foreach (var row in ApplicationUpdate.Data.Rows)
                row.Attributes.Add(CloneColumnForRow(column));
        }

        private void RemoveResponseColumn(AttributeDTO column)
        {
            var index = ResponseColumns.FindIndex(x => x.ID == column.ID);
            if (index < 0)
                index = ResponseColumns.FindIndex(x => SameColumn(x, column));

            if (index < 0)
                return;

            foreach (var row in ApplicationUpdate.Data.Rows)
            {
                var ordered = row.Attributes.OrderBy(x => x.Order).ToList();
                if (index < ordered.Count)
                    row.Attributes.Remove(ordered[index]);
                ReorderColumns(row.Attributes);
            }
        }

        private void SetColumnFieldType(AttributeDTO column, string? fieldType)
        {
            fieldType = string.IsNullOrWhiteSpace(fieldType) ? "Text" : fieldType;
            column.FieldTypeLabel = fieldType;
            column.AttributeType = ToAttributeType(fieldType);
            column.IsComputed = fieldType == "Beräknat fält";
        }

        private static string DisplayFieldType(AttributeDTO column)
            => !string.IsNullOrWhiteSpace(column.FieldTypeLabel)
                ? column.FieldTypeLabel
                : ToFieldTypeLabel(column.AttributeType);

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

            // A template must belong to a department (unless it applies to all departments), otherwise the
            // backend rejects it silently — validate here so the user sees why nothing was saved.
            if (!ApplicationUpdate.Data.AllDepartments && ApplicationUpdate.DepartmentId <= 0)
            {
                _saveError = "Välj en avdelning eller markera \"Alla avdelningar\" innan du sparar.";
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

            if (ApplicationUpdate.Data.Rows.Count == 0)
                AddCheckpoint(ApplicationUpdate.Data.Sections.OrderBy(x => x.SortOrder).First());

            EnsureRowsHaveColumns();
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

        private void EnsureRowsHaveColumns()
        {
            var templateColumns = GetResponseColumnsForCurrentState();
            if (templateColumns.Count == 0)
                templateColumns = DefaultColumnsForCurrentType().Select(CloneColumnForRow).ToList();

            foreach (var row in ApplicationUpdate.Data.Rows)
            {
                if (row.Attributes.Count == 0)
                {
                    row.Attributes = templateColumns.Select(CloneColumnForRow).ToList();
                    continue;
                }

                foreach (var attr in row.Attributes)
                    NormalizeColumn(attr);
            }
        }

        private List<AttributeDTO> GetResponseColumnsForCurrentState()
            => ApplicationUpdate.Data.Rows
                .OrderBy(x => x.SortOrder)
                .FirstOrDefault()?.Attributes
                .OrderBy(x => x.Order)
                .ToList() ?? [];

        private void NormalizeBeforeSave()
        {
            foreach (var section in ApplicationUpdate.Data.Sections)
            {
                section.Title = (section.Title ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(section.Title))
                    section.Title = "Sektion";
            }

            var templateColumns = ResponseColumns
                .Select((x, index) =>
                {
                    NormalizeColumn(x);
                    x.Order = index;
                    return x;
                })
                .ToList();

            foreach (var row in ApplicationUpdate.Data.Rows)
            {
                var section = ResolveSection(row);
                row.Name = string.IsNullOrWhiteSpace(row.Name) ? "Kontrollpunkt" : row.Name.Trim();
                row.SectionId = section.Id;
                row.SectionTitle = section.Title;
                row.Description = section.Title;
                row.Attributes = MergeColumns(row.Attributes, templateColumns);
            }
        }

        private List<AttributeDTO> MergeColumns(List<AttributeDTO> existing, List<AttributeDTO> templateColumns)
        {
            var orderedExisting = existing.OrderBy(x => x.Order).ToList();
            var merged = new List<AttributeDTO>();

            for (var i = 0; i < templateColumns.Count; i++)
            {
                var template = templateColumns[i];
                var target = i < orderedExisting.Count ? orderedExisting[i] : new AttributeDTO { ID = Guid.NewGuid() };
                target.Label = template.Label;
                target.FieldKey = template.FieldKey;
                target.FieldTypeLabel = template.FieldTypeLabel;
                target.AttributeType = template.AttributeType;
                target.Required = template.Required;
                target.IsComputed = template.IsComputed;
                target.Validation = template.Validation;
                target.Style = template.Style;
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

        private IReadOnlyList<AttributeDTO> DefaultColumnsForCurrentType()
        {
            var template = ApplicationUpdate.Data.TemplateType switch
            {
                SelfInspectionTemplateTypes.RiskAnalysis => SelfInspectionStandardTemplates.CreateRiskAnalysis(ApplicationUpdate.DepartmentId),
                SelfInspectionTemplateTypes.Handover => SelfInspectionStandardTemplates.CreateHandover(ApplicationUpdate.DepartmentId),
                _ => SelfInspectionStandardTemplates.CreateChecklist(ApplicationUpdate.DepartmentId)
            };

            return template.Data.Rows.FirstOrDefault()?.Attributes ?? [];
        }

        private static AttributeDTO CloneColumnForRow(AttributeDTO source)
        {
            var clone = source.Clone();
            clone.ID = Guid.NewGuid();
            NormalizeColumn(clone);
            return clone;
        }

        private static void NormalizeColumn(AttributeDTO attr)
        {
            if (attr.ID == Guid.Empty)
                attr.ID = Guid.NewGuid();
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

        private static bool SameColumn(AttributeDTO left, AttributeDTO right)
            => left.ID == right.ID
               || (!string.IsNullOrWhiteSpace(left.FieldKey) && SameText(left.FieldKey, right.FieldKey))
               || SameText(left.Label, right.Label);

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

        private bool CanMoveColumn(AttributeDTO column, int direction)
        {
            var ordered = ResponseColumns;
            var index = ordered.FindIndex(x => x.ID == column.ID);
            var target = index + direction;
            return index >= 0 && target >= 0 && target < ordered.Count;
        }

        private void MoveColumn(AttributeDTO column, int direction)
        {
            var index = ResponseColumns.FindIndex(x => x.ID == column.ID);
            var target = index + direction;
            if (index < 0 || target < 0 || target >= ResponseColumns.Count)
                return;

            // Columns are mirrored across every row (kept in sync by Order), so the same positional swap
            // must be applied to each row's attribute list.
            foreach (var row in ApplicationUpdate.Data.Rows)
            {
                var attrs = row.Attributes.OrderBy(x => x.Order).ToList();
                if (index < attrs.Count && target < attrs.Count)
                    (attrs[index], attrs[target]) = (attrs[target], attrs[index]);
                for (var i = 0; i < attrs.Count; i++)
                    attrs[i].Order = i;
            }
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

            if (string.IsNullOrWhiteSpace(ApplicationUpdate.Data.TemplateType))
                errors.Add("Typ är obligatoriskt.");

            if (string.IsNullOrWhiteSpace(ApplicationUpdate.Data.LinkType))
                errors.Add("Koppling är obligatorisk.");

            if (ApplicationUpdate.Data.Sections.Count == 0)
                errors.Add("Minst en sektion krävs.");

            if (ApplicationUpdate.Data.Sections.Any(s => string.IsNullOrWhiteSpace(s.Title)))
                errors.Add("Sektionen saknar namn.");

            if (ApplicationUpdate.Data.Rows.Any(r => string.IsNullOrWhiteSpace(r.Name)))
                errors.Add("Kontrollpunkten saknar text.");

            if (ResponseColumns.Any(c => string.IsNullOrWhiteSpace(c.Label)))
                errors.Add("Svarskolumnen saknar namn.");

            if (ResponseColumns.Any(c => string.IsNullOrWhiteSpace(DisplayFieldType(c))))
                errors.Add("Svarskolumnen saknar fälttyp.");

            return errors;
        }

        private void Preview()
        {
            // Preview a snapshot (Data setter clones) so the read-only preview never mutates the working
            // copy the admin is still editing.
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
