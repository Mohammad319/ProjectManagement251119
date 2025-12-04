using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.ResourceType;

namespace ProjectManagement.Client.Pages.Calculation.Form
{
    partial class ResourceFormUI
    {
        int Selectwidth = 150;
        int InputNumber = 110;

        private void ChangeQuantityParam(string qp)
        {
            if (string.IsNullOrEmpty(qp))
                ResourceUpdate.Data.Quantity = null;
            else if (qp != ConstValues.FixedQ)
                ResourceUpdate.Data.Quantity = Calc?.QuanityList?.FirstOrDefault(x => x.Name == qp)?.Quantity;

            ResourceUpdate.Data.QuantityParam = qp;
        }
        bool btnSubmitDisabled = false;
        [Parameter] public ResourceListMVVM? Resource { get; set; } = default!;
        private List<ResourcePostDTO> PostList = [];
        private ResourcePostDTO ResourceUpdate = new();
        AccountGroupsListDto? AccountGroupSelected = new();
        ListResourceTypeDTO? ResTypeSelected;

        ResourceFormDTO Config = new();
        void NewSP()
        {
            ResourcePostDTO p = new();
            ResourceUpdate.CopyPropertiesTo(p);
            p.Name = "Copy from " + p.Name;
            PostList.Insert(0, p);
        }
        void ChangePos(int id)
        {
            //var r = PostList[id];
            //if (ResourceUpdate.AccountId.HasValue)
            //    AccountGroupSelected = AccountGroupsAPI.FirstOrDefault(x => x.Accounts.Any(a => a.Id == r.AccountId));

            //ResourceUpdate = r;
        }
        async Task Update(double ch1, double ch2, double? BaseCost, double CapWaste, double? CO2, double Cost,
            double? FixedQ, string Unit, int? AccountId, int? resTypeId, int? resTypeSortId)
        {
            ResourceUpdate.Data.ChangeFactor1 = ch1;
            ResourceUpdate.Data.ChangeFactor2 = ch2;
            ResourceUpdate.Data.BaseCost = BaseCost;
            ResourceUpdate.Data.CapWaste = CapWaste;
            ResourceUpdate.Data.CO2 = CO2;
            ResourceUpdate.Data.Cost = Cost;
            ResourceUpdate.Data.Quantity = FixedQ;
            ResourceUpdate.Data.Unit = Unit;
            ResourceUpdate.ResourceTypeId = resTypeId;
            ResourceUpdate.ResourceSortId = resTypeSortId;
            await SetNewAccountAsync(AccountId);
            StateHasChanged();
        }
        async Task ChangeResType(ListResourceTypeDTO rt) => await Update(rt.ChangeFactor1, rt.ChangeFactor2, rt.BaseCost,
                    rt.CapWaste, rt.CO2, rt.Cost, rt.FixedQ, rt.Unit, rt.AccountId, rt.Id, null);
        async Task ChangeResType(ListResourceSortDTO rt) => await Update(rt.ChangeFactor1, rt.ChangeFactor2, rt.BaseCost,
    rt.CapWaste, rt.CO2, rt.Cost, rt.FixedQ, rt.Unit, rt.AccountId, ResourceUpdate.ResourceTypeId, rt.Id);
        async Task ChangeResType(int? id)
        {

            if (!id.HasValue) return;
            ResTypeSelected = Config.ResourceTypes.FirstOrDefault(x => x.Id == id);
            if (ResTypeSelected == null) return;
            if (Resource.Id > 0)
            {
                ResourceUpdate.ResourceTypeId = id;
                ResourceUpdate.ResType = ResTypeSelected.Type;
                ResourceUpdate.ResourceSortId = null;
            }
            else await ChangeResType(ResTypeSelected);
        }
        async Task ChangeResSort2(int? id)
        {
            if (Resource?.Id > 0) ResourceUpdate.ResourceSortId = id;
            else if (id.HasValue)
            {
                ListResourceSortDTO? ResourceSort = ResTypeSelected?.ResourcesSort?.FirstOrDefault(x => x.Id == id);
                await ChangeResType(ResourceSort);
            }
            else
            {
                await SetNewAccountAsync(null);
                ResourceUpdate.ResourceSortId = null;
            }
        }
        async Task ChangeGroupAccount(ChangeEventArgs e)
        {
            AccountGroupSelected = new();
            ResourceUpdate.AccountId = null;
            if (string.IsNullOrEmpty(e.Value.ToString())) return;
            await Task.Delay(1);
            AccountGroupSelected = Config?.AccountGroups?.FirstOrDefault(x => x.Id == int.Parse(e.Value.ToString()));
        }
        async Task SetNewAccountAsync(int? accountId)
        {
            Refresh = true;
            await Task.Delay(1);
            ResourceUpdate.AccountId = accountId;
            if (accountId != null)
                AccountGroupSelected = Config.AccountGroups.FirstOrDefault(x => x.Accounts.Any(s => s.Id == accountId));
            Refresh = false;
        }

        async Task HandleSubmitAsync()
        {
            btnSubmitDisabled = true;
            bool hasSuccess = false;
            try
            {
                if (ResTypeSelected != null)
                    ResourceUpdate.ResType = ResTypeSelected.Type;
                if (Resource.Id > 0)
                {
                    hasSuccess = await ExHandlers.RunCheckTokenAsync(() => Repo.Resource.UpdateAsync(ResourceUpdate, Resource.Id));
                }
                else if (Resource.Id == 0)
                {
                    double maxOrder = 0;
                    if (Calc?.Tasks?.FirstOrDefault(x => x.Id == Resource.TaskId)?.Resources.Count != 0)
                        maxOrder = Calc.Tasks.FirstOrDefault(x => x.Id == Resource.TaskId).Resources.OrderByDescending(x => x.Order).Last().Order + 100;
                    foreach (var item in PostList)
                    {
                        item.Order = maxOrder;
                        maxOrder += 100;
                    }
                    hasSuccess = await ExHandlers.RunCheckTokenAsync(() => Repo.Resource.CreateAsync([ResourceUpdate], Resource.TaskId));
                }
                MHD.Notifications(Resource.Id == 0 ? ToastType.Add : ToastType.Update, hasSuccess);
            }
            catch (Exception ex)
            {
                MHD.Notifications(Resource.Id == 0 ? ToastType.Add : ToastType.Update, false);
                Console.WriteLine(ex.Message);
            }
            Close();
        }
        protected async override Task OnInitializedAsync()
        {
            ResourceUpdate = new();
            ResourceUpdate.Data = new ResourceData();
            editContext = new(ResourceUpdate);
            editContext.OnValidationRequested += HandleValidationRequested;
            messageStore = new(editContext);
            if (Calc.Opportunities == null)
                Calc.Opportunities = await Repo.Opportunity.GetAsync(Calc.Id);
            Config = await ExHandlers.RunCheckTokenAsync(Repo.Resource.GetConfigForm);
            PropertyCopier.CopyPropertiesTo(Resource, ResourceUpdate);
            ResourceUpdate.Data = Resource.Data;
            if (Resource.Id > 0)
                await SetNewAccountAsync(Resource.AccountId);
            else
            {
                PostList.Add(ResourceUpdate);
                if (Config.ResourceTypes != null && Config.ResourceTypes.Count > 0)
                    await ChangeResType(Config.ResourceTypes.First().Id);
                //await SetNewAccountAsync(ResourceTypes.First().AccountId);
            }

            StateHasChanged();
        }
        bool Refresh = false;
        private EditContext editContext;
        private ValidationMessageStore messageStore;
        private void HandleValidationRequested(object sender, ValidationRequestedEventArgs args)
        {
            messageStore?.Clear();
            if (ResourceUpdate.ResType == ResourceTypesEnum.Materials && (ResourceUpdate.Data.CapWaste < 0) || ResourceUpdate.Data.CapWaste > 999)
            {
                messageStore?.Add(() => ResourceUpdate.Data.CapWaste, ResourceApp.rangeErrors);
            }
        }

        public void Dispose()
        {
            if (editContext is not null)
            {
                editContext.OnValidationRequested -= HandleValidationRequested;
            }
        }
        void Close() => Modal.Close();
    }
}
