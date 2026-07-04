using BlazorMHD.UI.Core.DesignSystem;
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
        private RowDTO? RowForm;
        private bool IsLoading;
        private static readonly IReadOnlyList<string> TemplateTypes =
        [
            SelfInspectionTemplateTypes.Checklist,
            SelfInspectionTemplateTypes.Handover,
            SelfInspectionTemplateTypes.RiskAnalysis
        ];

        protected override async Task OnParametersSetAsync()
        {
            Departments = await Dispatcher.Send(new GetDepartmentsAsListQuery()) ?? [];

            ApplicationUpdate.Data ??= new ApplicationDataDTO();
            ApplicationUpdate.Data.Rows ??= [];
            if (string.IsNullOrWhiteSpace(ApplicationUpdate.Data.TemplateType))
                ApplicationUpdate.Data.TemplateType = SelfInspectionTemplateTypes.Checklist;
            if (string.IsNullOrWhiteSpace(ApplicationUpdate.Data.Purpose))
                ApplicationUpdate.Data.Purpose = ApplicationUpdate.Data.Description ?? string.Empty;

            if (ApplicationUpdate.Id == 0 && ApplicationUpdate.DepartmentId <= 0)
            {
                var firstDepartmentId = Departments.FirstOrDefault()?.Id;
                if (firstDepartmentId.HasValue)
                    ApplicationUpdate.DepartmentId = firstDepartmentId.Value;
            }
        }

        private void OpenNewRow() => RowForm = new RowDTO();

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

            ApplicationUpdate.Data = template.Data;
            ApplicationUpdate.DepartmentId = keepDepartment;
            ApplicationUpdate.IsVisible = keepVisible;
            if (!string.IsNullOrWhiteSpace(keepName))
                ApplicationUpdate.Name = keepName;
        }

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
            await Callback.InvokeAsync(isSuccess);
        }

        private void CallBackRowForm(RowDTO row)
        {
            if (row is not null)
            {
                if (row.ID == Guid.Empty)
                {
                    row.ID = Guid.NewGuid();
                    ApplicationUpdate.Data.Rows.Add(row);
                }
                else if (RowForm is not null)
                {
                    RowForm.Name = row.Name;
                    RowForm.Description = row.Description;
                    RowForm.StyleRow = row.StyleRow;
                    RowForm.Style = row.Style;
                    RowForm.Attributes = row.Attributes;
                    RowForm.IsVisible = row.IsVisible;
                }
            }

            RowForm = null;
        }

        private void Remove(RowDTO row)
        {
            MHD.MessageYesNo(ResourceApp.delete,
                AppLoc[LocalizerConst.deleteConfirmMsg, row.Name],
                MhdState.Warning,
                EventCallback.Factory.Create(this, () => RemoveAsync(row)));
        }

        private bool RemoveAsync(RowDTO row)
        {
            ApplicationUpdate.Data.Rows.Remove(row);
            StateHasChanged();
            return true;
        }

        private Task CancelAsync() => Callback.InvokeAsync(false);
    }
}
