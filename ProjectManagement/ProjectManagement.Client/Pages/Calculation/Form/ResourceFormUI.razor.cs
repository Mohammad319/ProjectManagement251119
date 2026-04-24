using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.Components;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.ResourceType;

namespace ProjectManagement.Client.Pages.Calculation.Form
{
    partial class ResourceFormUI : IDisposable
    {
        public const string DialogFormId = "resForm";
        private ResourceMetadata ResourceData => ResourceUpdate.Data;
        private int AddOnRowCount => (ResourceUpdate.Data.AddOns?.Count ?? 0) + 1;
        private decimal? ResolvedResourceQuantity => GetResolvedResourceQuantity();
        private decimal PrimaryAddOnQuantity => ResolvedResourceQuantity ?? 0m;
        private decimal PrimaryAddOnBaseCost => ResourceUpdate.Data.BaseCost ?? 0m;
        private decimal PrimaryAddOnTotalCost => RoundMoney((PrimaryAddOnQuantity * ResourceUpdate.Data.Cost) + PrimaryAddOnBaseCost);
        private decimal EffectiveAddOnBaseCost => RoundMoney(PrimaryAddOnBaseCost + (ResourceUpdate.Data.AddOns?.Sum(x => x.BaseCost) ?? 0m));
        private decimal EffectiveAddOnTotalCost => RoundMoney(PrimaryAddOnTotalCost + (ResourceUpdate.Data.AddOns?.Sum(GetAddOnTotalCost) ?? 0m));
        private decimal EffectiveAddOnCost
        {
            get
            {
                if (PrimaryAddOnQuantity <= 0m)
                    return ResourceUpdate.Data.Cost;

                decimal addOnVariableCost = ResourceUpdate.Data.AddOns?.Sum(x => x.Quantity(PrimaryAddOnQuantity) * x.Cost) ?? 0m;
                return RoundMoney(((PrimaryAddOnQuantity * ResourceUpdate.Data.Cost) + addOnVariableCost) / PrimaryAddOnQuantity);
            }
        }

        private void AddUpperNote() => ResourceData.UpperNote.Add(string.Empty);

        private void RemoveUpperNoteAt(int index) => ResourceData.UpperNote.RemoveAt(index);

        private decimal GetAddOnTotalCost(ResourceAddon addOn) => RoundMoney((addOn.Quantity(PrimaryAddOnQuantity) * addOn.Cost) + addOn.BaseCost);
        private bool HasCapResourceType => ResourceUpdate.ResType is ResourceTypesEnum.MachinesAndEquipments or ResourceTypesEnum.Worker;
        private decimal? ParentTaskCap => GetParentTaskCap();
        private bool UseCapFromTask => HasCapResourceType && ResourceUpdate.Data.CapFromTask && ParentTaskCap.HasValue;
        private decimal EffectiveCapWaste => UseCapFromTask ? ParentTaskCap!.Value : ResourceUpdate.Data.CapWaste;

        private decimal? GetParentTaskQuantity()
        {
            var resource = Resource;
            if (Calc is null || resource is null || resource.TaskId <= 0)
                return null;

            return Calc.TryGetTask(resource.TaskId, out var task) ? task?.Quantity : null;
        }

        private decimal? GetParentTaskCap()
        {
            var resource = Resource;
            if (Calc is null || resource is null || resource.TaskId <= 0)
                return null;

            return Calc.TryGetTask(resource.TaskId, out var task) ? task?.Metadata?.Cap : null;
        }

        private decimal? GetResolvedResourceQuantity()
        {
            var quantityParam = ResourceUpdate.Data.QuantityParam;

            if (quantityParam == ConstValues.FixedQ)
                return ResourceUpdate.Data.Quantity;

            if (!string.IsNullOrEmpty(quantityParam))
                return Calc?.QuanityList?.FirstOrDefault(x => x.Name == quantityParam)?.Quantity;

            var parentTaskQuantity = GetParentTaskQuantity();
            if (!parentTaskQuantity.HasValue)
                return null;

            var baseCalc = parentTaskQuantity.Value * ResourceUpdate.Data.ChangeFactor1 * ResourceUpdate.Data.ChangeFactor2;
            var capWaste = EffectiveCapWaste;

            if (ResourceUpdate.ResType == ResourceTypesEnum.Materials && capWaste != 0m)
                return baseCalc * (1 + capWaste / 100m);

            if (ResourceUpdate.ResType is ResourceTypesEnum.MachinesAndEquipments or ResourceTypesEnum.Worker && capWaste != 0m)
                return baseCalc / capWaste;

            return baseCalc;
        }

        private void SyncResolvedResourceQuantity()
        {
            var resolvedQuantity = GetResolvedResourceQuantity();
            ResourceUpdate.Data.Quantity = resolvedQuantity;
            ResourceUpdate.ActuallyQuantity = resolvedQuantity ?? 0m;
        }

        private void SyncResolvedResourceAndTimes()
        {
            SyncResolvedResourceQuantity();
            ResourceUpdate.Data.SyncTimesWithQuantity();
        }

        private void AddAddOn()
        {
            ResourceUpdate.Data.AddOns.Add(new ResourceAddon
            {
                Name = string.Empty,
                Unit = ResourceUpdate.Data.Unit,
                Factor = 1m,
                Type = QuantityResourceAddon.Multiplication,
                Cost = 0m,
                BaseCost = 0m
            });
        }

        private void RemoveAddOn(int index)
        {
            if (index < 0 || index >= ResourceUpdate.Data.AddOns.Count)
                return;

            ResourceUpdate.Data.AddOns.RemoveAt(index);
        }

        private void ChangeQuantityParam(string qp)
        {
            ResourceUpdate.Data.QuantityParam = qp;
            SyncResolvedResourceAndTimes();
        }

        private void ChangeCapFromTask(bool value)
        {
            ResourceUpdate.Data.CapFromTask = value;
            SyncResolvedResourceAndTimes();
        }

        private void NormalizeCapFromTaskState()
        {
            if (!HasCapResourceType)
                ResourceUpdate.Data.CapFromTask = false;
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
        async Task Update(decimal ch1, decimal ch2, decimal? BaseCost, decimal CapWaste, double? CO2, decimal Cost,
            decimal? FixedQ, string Unit, int? AccountId, int? resTypeId, int? resTypeSortId)
        {
            ResourceUpdate.Data.ChangeFactor1 = ch1;
            ResourceUpdate.Data.ChangeFactor2 = ch2;
            ResourceUpdate.Data.BaseCost = BaseCost.HasValue ? BaseCost.Value : null;
            ResourceUpdate.Data.CapWaste = CapWaste;
            ResourceUpdate.Data.CO2 = CO2;
            ResourceUpdate.Data.Cost = Cost;
            ResourceUpdate.Data.Quantity = FixedQ.HasValue ? FixedQ.Value : null;
            ResourceUpdate.Data.Unit = Unit;
            ResourceUpdate.ResourceTypeId = resTypeId;
            ResourceUpdate.ResourceSortId = resTypeSortId;
            NormalizeCapFromTaskState();
            SyncResolvedResourceAndTimes();

            //ResourceUpdate.ResType = ty;
            await SetNewAccountAsync(AccountId);
            StateHasChanged();
        }
        async Task ChangeResType(ListResourceTypeDTO rt)
        {
            ResourceUpdate.ResType = rt.Type;
            await Update(rt.ChangeFactor1, rt.ChangeFactor2, rt.BaseCost,
                rt.CapWaste, rt.CO2, rt.Cost, rt.FixedQ, rt.Unit, rt.AccountId, rt.Id, null);
        }
        async Task ChangeResType(ListResourceSortDTO rt) => await Update(rt.ChangeFactor1, rt.ChangeFactor2, rt.BaseCost,
    rt.CapWaste, rt.CO2, rt.Cost, rt.FixedQ, rt.Unit, rt.AccountId, ResourceUpdate.ResourceTypeId, rt.Id);
        async Task ChangeResType(int? id)
        {

            if (!id.HasValue) return;
            ResTypeSelected = Config.ResourceTypes?.FirstOrDefault(x => x.Id == id);
            if (ResTypeSelected == null) return;
            if (Resource?.Id > 0)
            {
                ResourceUpdate.ResourceTypeId = id;
                ResourceUpdate.ResType = ResTypeSelected.Type;
                ResourceUpdate.ResourceSortId = null;
                NormalizeCapFromTaskState();
                SyncResolvedResourceAndTimes();
            }
            else await ChangeResType(ResTypeSelected);

            await InvokeAsync(StateHasChanged);
        }
        async Task ChangeResSort2(int? id)
        {
            if (Resource?.Id > 0) ResourceUpdate.ResourceSortId = id;
            else if (id.HasValue)
            {
                ListResourceSortDTO? ResourceSort = ResTypeSelected?.ResourcesSort?.FirstOrDefault(x => x.Id == id);
                if (ResourceSort is not null)
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
            var selected = e.Value?.ToString();
            if (string.IsNullOrEmpty(selected) || !int.TryParse(selected, out var accountGroupId)) return;
            await Task.Delay(1);
            AccountGroupSelected = Config?.AccountGroups?.FirstOrDefault(x => x.Id == accountGroupId);
        }
        async Task SetNewAccountAsync(int? accountId)
        {
            Refresh = true;
            await Task.Delay(1);
            ResourceUpdate.AccountId = accountId;
            if (accountId != null)
                AccountGroupSelected = Config.AccountGroups?.FirstOrDefault(x => x.Accounts.Any(s => s.Id == accountId));
            Refresh = false;
        }

        async Task HandleSubmitAsync()
        {
            if (btnSubmitDisabled)
                return;

            btnSubmitDisabled = true;
            bool hasSuccess = false;
            var resource = Resource;
            if (resource is null)
            {
                MHD.Notifications(ToastType.Danger, false);
                Close();
                return;
            }

            try
            {
                if (ResTypeSelected != null)
                    ResourceUpdate.ResType = ResTypeSelected.Type;

                NormalizeCapFromTaskState();
                SyncResolvedResourceAndTimes();

                if (resource.Id > 0)
                {
                    hasSuccess = await Repo.Resource.UpdateAsync(ResourceUpdate, resource.Id);
                }
                else if (resource.Id == 0)
                {
                    int maxOrder = 0;
                    var task = Calc?.Tasks?.FirstOrDefault(x => x.Id == resource.TaskId);
                    if (task?.Resources?.Count > 0)
                        maxOrder = task.Resources.Max(x => x.Order) + 100;
                    foreach (var item in PostList)
                    {
                        item.Order = maxOrder;
                        maxOrder += 100;
                    }
                    hasSuccess = await Repo.Resource.CreateAsync([ResourceUpdate], resource.TaskId);
                }
                MHD.Notifications(resource.Id == 0 ? ToastType.Add : ToastType.Update, hasSuccess);
            }
            catch (Exception ex)
            {
                MHD.Notifications(resource.Id == 0 ? ToastType.Add : ToastType.Update, false);
                Console.WriteLine(ex.Message);
            }
            Close();
        }
        protected async override Task OnInitializedAsync()
        {
            ResourceUpdate = new();
            ResourceUpdate.Data = new ResourceMetadata();
            editContext = new(ResourceUpdate);
            editContext.SetFieldCssClassProvider(RequiredFieldCssClassProvider.Instance);
            editContext.OnValidationRequested += HandleValidationRequested;
            editContext.OnFieldChanged += HandleFieldChanged;
            messageStore = new(editContext);
            if (Calc.Opportunities == null)
                Calc.Opportunities = await Repo.Opportunity.GetAsync(Calc.Id);
            Config = await Repo.Resource.GetConfigForm();
            if (Resource is not null)
            {
                PropertyCopier.CopyPropertiesTo(Resource, ResourceUpdate);
                ResourceUpdate.Data = Resource.Data;
            }

            if (Resource?.Id > 0)
                await SetNewAccountAsync(Resource.AccountId);
            else
            {
                PostList.Add(ResourceUpdate);
                if (Config.ResourceTypes != null && Config.ResourceTypes.Count > 0)
                    await ChangeResType(Config.ResourceTypes.First().Id);
                //await SetNewAccountAsync(ResourceTypes.First().AccountId);
            }

            SyncResolvedResourceAndTimes();

            StateHasChanged();
        }
        bool Refresh = false;
        private EditContext? editContext;
        private ValidationMessageStore? messageStore;
        private FieldIdentifier FixedQuantityField => new(ResourceUpdate.Data, nameof(ResourceMetadata.Quantity));

        private bool IsFixedQuantityRequired()
            => string.Equals(ResourceUpdate.Data.QuantityParam, ConstValues.FixedQ, StringComparison.OrdinalIgnoreCase);

        private void ClearFixedQuantityValidation()
            => messageStore?.Clear(FixedQuantityField);

        private void ValidateFixedQuantity()
        {
            if (IsFixedQuantityRequired() && !ResourceUpdate.Data.Quantity.HasValue)
            {
                messageStore?.Add(
                    FixedQuantityField,
                    string.Format(ProjectManagement.Shared.Resource.ResLocalize.FieldIsRequred, CalcResource.quantity));
            }
        }

        private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args)
        {
            messageStore?.Clear();
            SyncResolvedResourceAndTimes();
            ValidateFixedQuantity();
            if (ResourceUpdate.ResType == ResourceTypesEnum.Materials && (ResourceUpdate.Data.CapWaste < 0) || ResourceUpdate.Data.CapWaste > 999)
            {
                messageStore?.Add(() => ResourceUpdate.Data.CapWaste, ResourceApp.rangeErrors);
            }

            ValidateTimesPercentage();
        }

        private void HandleFieldChanged(object? sender, FieldChangedEventArgs args)
        {
            if (ReferenceEquals(args.FieldIdentifier.Model, ResourceUpdate.Data)
                && IsResourceQuantityDriver(args.FieldIdentifier.FieldName))
            {
                SyncResolvedResourceAndTimes();

                if (args.FieldIdentifier.FieldName is nameof(ResourceMetadata.Quantity) or nameof(ResourceMetadata.QuantityParam))
                {
                    ClearFixedQuantityValidation();
                    editContext?.NotifyValidationStateChanged();
                }

                return;
            }

            if (args.FieldIdentifier.Model is ResourceTime
                && args.FieldIdentifier.FieldName == nameof(ResourceTime.Percentage))
            {
                ResourceUpdate.Data.SyncTimesWithQuantity();
            }
        }

        private static bool IsResourceQuantityDriver(string fieldName)
            => fieldName is nameof(ResourceMetadata.Quantity)
                or nameof(ResourceMetadata.QuantityParam)
                or nameof(ResourceMetadata.ChangeFactor1)
                or nameof(ResourceMetadata.ChangeFactor2)
                or nameof(ResourceMetadata.CapWaste);

        private void ValidateTimesPercentage()
        {
            if (ResourceUpdate.Data.Times is null || ResourceUpdate.Data.Times.Count == 0)
                return;

            var timesPercentageSum = RoundPercentageForValidation(ResourceUpdate.Data.Times.Sum(x => x.Percentage ?? 0m));

            if (timesPercentageSum != 100m)
                messageStore?.Add(
                    new FieldIdentifier(ResourceUpdate.Data, nameof(ResourceUpdate.Data.Times)),
                    "The total Times percentage must equal 100%.");
        }

        private static decimal RoundPercentageForValidation(decimal value)
            => Math.Round(value, 4, MidpointRounding.AwayFromZero);

        private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public void Dispose()
        {
            if (editContext is not null)
            {
                editContext.OnValidationRequested -= HandleValidationRequested;
                editContext.OnFieldChanged -= HandleFieldChanged;
            }
        }
        void Close() => Modal.Close();
    }
}
