using Application.Feature.Application.Commands;
using Application.Feature.Application.Queries;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.DTO.App;

namespace ProjectManagement.Components.ControlComponents.ApplicationTemplate
{
    public partial class ApplicationIndexUI
    {
        private bool IsVisible = true;
        private bool Loading = true;
        private List<ApplicationDTO> Applications = [];
        private ApplicationDTO? ApplicationForm;

        private List<ApplicationDTO> VisibleApplications
            => Applications.Where(x => x.IsVisible == IsVisible).ToList();

        private void ToggleVisibility() => IsVisible = !IsVisible;

        private void OpenApplication(ApplicationDTO application)
            => ApplicationForm = application;

        private void NewApp()
        {
            ApplicationForm = new ApplicationDTO
            {
                Name = string.Empty,
                IsVisible = true,
                Data = new ApplicationDataDTO
                {
                    Rows =
                    [
                        CreateRow(
                            "Row_1",
                            "color:#43952d;background-color:#ebf5eb;width:200px;font-size:18px;text-align:center;font-weight:bold;",
                            "color:#00000;background-color:#ecebf4;height:50px;padding-left:3px;padding-right:5px;align-items:center;",
                            new AttributeDTO
                            {
                                Order = 0,
                                AttributeType = AttributeType.Text,
                                ID = Guid.NewGuid(),
                                Style = "color:#864184;background-color:#fff5ff;width:120px;margin-left:5px;margin-right:5px;font-weight:bold;",
                                Required = true
                            },
                            new AttributeDTO
                            {
                                Order = 1,
                                AttributeType = AttributeType.Bool,
                                ID = Guid.NewGuid(),
                                Style = "color:#864184;background-color:#321fed;width:25px;margin-left:5px;margin-right:5px;font-weight:bold;"
                            },
                            new AttributeDTO
                            {
                                Order = 2,
                                AttributeType = AttributeType.Int,
                                ID = Guid.NewGuid(),
                                Style = "color:#864184;background-color:#fff5ff;width:100px;height:25px;margin-left:5px;margin-right:5px;font-weight:bold;"
                            }),
                        CreateRow(
                            "Row_2",
                            "color:#43952d;background-color:#ebf5eb;width:200px;font-size:18px;text-align:center;font-weight:bold;",
                            "color:#00000;background-color:#ecebf4;height:50px;padding-left:3px;padding-right:5px;align-items:center;",
                            new AttributeDTO
                            {
                                Order = 0,
                                AttributeType = AttributeType.Text,
                                ID = Guid.NewGuid(),
                                Style = "color:#864184;background-color:#fff5ff;width:200px;margin-left:5px;margin-right:5px;font-weight:bold;",
                                Required = true
                            },
                            new AttributeDTO
                            {
                                Order = 1,
                                AttributeType = AttributeType.Date,
                                ID = Guid.NewGuid(),
                                Style = "color:#864184;background-color:#fff5ff;width:180px;margin-left:5px;margin-right:5px;font-weight:bold;"
                            },
                            new AttributeDTO
                            {
                                Order = 2,
                                AttributeType = AttributeType.Int,
                                ID = Guid.NewGuid(),
                                Style = "color:#864184;background-color:#fff5ff;width:100px;margin-left:5px;margin-right:5px;font-weight:bold;"
                            }),
                        CreateRow(
                            "Row_3",
                            "color:#43952d;background-color:#ebf5eb;width:200px;font-size:18px;text-align:center;font-weight:bold;",
                            "color:#00000;background-color:#ecebf4;height:50px;padding-left:3px;padding-right:5px;align-items:center;",
                            new AttributeDTO
                            {
                                Order = 0,
                                AttributeType = AttributeType.Text,
                                ID = Guid.NewGuid(),
                                Style = "color:#317c27;background-color:#f0fff4;width:170px;margin-left:5px;margin-right:5px;font-size:12px;text-align:center;",
                                Required = true
                            },
                            new AttributeDTO
                            {
                                Order = 1,
                                AttributeType = AttributeType.Char,
                                ID = Guid.NewGuid(),
                                Style = "color:#9a8932;background-color:#fffde5;width:50px;margin-left:5px;margin-right:5px;font-size:12px;text-align:center;"
                            },
                            new AttributeDTO
                            {
                                Order = 2,
                                AttributeType = AttributeType.Double,
                                ID = Guid.NewGuid(),
                                Style = "color:#864184;background-color:#fff5ff;width:100px;margin-left:5px;margin-right:5px;font-weight:bold;"
                            })
                    ]
                }
            };
        }

        private static RowDTO CreateRow(string name, string style, string styleRow, params AttributeDTO[] attributes)
            => new()
            {
                Name = name,
                IsVisible = true,
                ID = Guid.NewGuid(),
                Style = style,
                StyleRow = styleRow,
                Attributes = [.. attributes]
            };

        private void Remove(ApplicationDTO application)
            => MHD.DeleteMessage(application.Name, EventCallback.Factory.Create(this, () => RemoveAsync(application)));

        private async Task RemoveAsync(ApplicationDTO application)
        {
            var result = await Dispatcher.Send(new DeleteApplicationCommand(application.Id));
            if (result)
            {
                Applications.RemoveAll(x => x.Id == application.Id);
                await InvokeAsync(StateHasChanged);
            }

            MHD.Notifications(ToastType.Delete, result);
        }

        private async Task BtnUpdateAsync(bool isSuccess)
        {
            if (!isSuccess)
                return;

            await GetApplicationsAsync();
            ApplicationForm = null;
            await InvokeAsync(StateHasChanged);
        }

        private async Task GetApplicationsAsync()
        {
            Loading = true;
            Applications = await Dispatcher.Send(new GetApplicationQuery(true)) ?? [];
            Loading = false;
        }

        protected override async Task OnInitializedAsync()
        {
            await GetApplicationsAsync();
        }
    }
}
