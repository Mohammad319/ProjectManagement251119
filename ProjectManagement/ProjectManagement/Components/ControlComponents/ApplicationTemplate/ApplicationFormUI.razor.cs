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
            if (ApplicationUpdate.Data.IsSystemTemplate)
            {
                MHD.Notifications(ApplicationUpdate.Id == 0 ? ToastType.Add : ToastType.Update, false);
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
            finally
            {
                IsLoading = false;
            }

            MHD.Notifications(ApplicationUpdate.Id == 0 ? ToastType.Add : ToastType.Update, isSuccess);
            if (isSuccess)
                await Callback.InvokeAsync(true);
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

        private Task CancelAsync() => Callback.InvokeAsync(false);
    }
}
