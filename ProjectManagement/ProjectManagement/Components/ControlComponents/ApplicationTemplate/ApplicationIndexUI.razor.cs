using Application.Feature.Application.Commands;
using Application.Feature.Application.Queries;
using Domain.Entities.Application;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Shared.Base.Application;

namespace ProjectManagement.Components.ControlComponents.ApplicationTemplate
{
    public partial class ApplicationIndexUI
    {
        bool IsVisible = true;
        List<ApplicationEntity>? Applications;
        ApplicationEntity? ApplicationForm;
        void NewApp()
        {
            ApplicationForm = new()
            {
                Data = new()
                {
                    Row = [
                    new (){Name = "Row_1", IsVisible = true, ID = Guid.NewGuid(),
                        Style = "color:#43952d;background-color:#ebf5eb;width:200px;font-size:18px;text-align:center;font-weight:bold;",
                        StyleRow = "color:#00000;background-color:#ecebf4;height:50px;padding-left:3px;padding-right:5px;align-items:center;",
                        Attributes = [
                new () { Order = 0,AttributeType = AttributeType.Text, ID = Guid.NewGuid()
             ,Style="color:#864184;background-color:#fff5ff;width:120px;margin-left:5px;margin-right:5px;font-weight:bold;"
                 ,Required = true},
                new () { Order = 1,AttributeType = AttributeType.Bool, ID = Guid.NewGuid()
             ,Style="color:#864184;background-color:#321fed;width:25px;margin-left:5px;margin-right:5px;font-weight:bold;"},
                new () { Order = 2,AttributeType = AttributeType.Int, ID = Guid.NewGuid()
             ,Style="color:#864184;background-color:#fff5ff;width:100px;height: 25px;margin-left:5px;margin-right:5px;font-weight:bold;"},
         ]},
            new (){Name = "Row_2", IsVisible = true, ID = Guid.NewGuid(),
                 Style = "color:#43952d;background-color:#ebf5eb;width:200px;font-size:18px;text-align:center;font-weight:bold;",
            StyleRow = "color:#00000;background-color:#ecebf4;height:50px;padding-left:3px;padding-right:5px;align-items:center;",
            Attributes = [
                new() { Order = 0,AttributeType = AttributeType.Text, ID = Guid.NewGuid()
             ,Style="color:#864184;background-color:#fff5ff;width:200px;margin-left:5px;margin-right:5px;font-weight:bold;"
                 ,Required = true},
                new() { Order = 1,AttributeType = AttributeType.Date, ID = Guid.NewGuid()
             ,Style="color:#864184;background-color:#fff5ff;width:180px;margin-left:5px;margin-right:5px;font-weight:bold;"},
                new() { Order = 2,AttributeType = AttributeType.Int, ID = Guid.NewGuid()
             ,Style="color:#864184;background-color:#fff5ff;width:100px;margin-left:5px;margin-right:5px;font-weight:bold;"},
         ]},
            new (){Name = "Row_3", IsVisible = true, ID = Guid.NewGuid(),
                    Style = "color:#43952d;background-color:#ebf5eb;width:200px;font-size:18px;text-align:center;font-weight:bold;",
            StyleRow = "color:#00000;background-color:#ecebf4;height:50px;padding-left:3px;padding-right:5px;align-items:center;",
            Attributes = [
                 new () { Order = 0,AttributeType = AttributeType.Text, ID = Guid.NewGuid()
             ,Style="color:#317c27;background-color:#f0fff4;width:170px;margin-left:5px;margin-right:5px;font-size:12px;text-align:center;",Required = true},
                new () { Order = 1,AttributeType = AttributeType.Char, ID = Guid.NewGuid()
             ,Style="color:#9a8932;background-color:#fffde5;width:50px;margin-left:5px;margin-right:5px;font-size:12px;text-align:center;"},
                new () { Order = 2,AttributeType = AttributeType.Double, ID = Guid.NewGuid()
             ,Style="color:#864184;background-color:#fff5ff;width:100px;margin-left:5px;margin-right:5px;font-weight:bold;"},
         ]
                }
            ]
                }
            };
        }
        void Remove(ApplicationEntity app)
        {
            MHD.DeleteMessage(app.Name, EventCallback.Factory.Create(this, () => RemoveAsync(app)));
        }
        async Task RemoveAsync(ApplicationEntity app)
        {
            bool result = await MicroBus.Send(new DeleteApplicationCommand(app.Id));
            if (result)
            {
                Applications?.Remove(app);
                StateHasChanged();
            }
            MHD.Notifications(ToastType.Delete, result);
        }
        async Task BtnUpdateAsync(bool IsSuccess)
        {
            if (IsSuccess) await GetApplicationsAsync();

            StateHasChanged();
            ApplicationForm = null;
        }

        async Task GetApplicationsAsync()
        {
            Applications = await MicroBus.Send(new GetApplicationQuery(true));
            //ExHandlers.RunCheckTokenAsync(() => Repo.Application.GetApplicationsAsync(true));
        }
        protected async override Task OnInitializedAsync()
        {
            await GetApplicationsAsync();
        }
    }
}
