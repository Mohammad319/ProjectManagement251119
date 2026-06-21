using BlazorMHD.UI.Core.DesignSystem;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.Repositories.Offer;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Hub;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Services.Calculation.CalculationItems
{
    public class ResourceService(
        IResourceRepository Repo,
        IStorageRepository Storage,
        FolderState _folderState,
        MhdServices Mhd,
        ContextMenuService ContextMenuService,
        IContextMenuBuilderService context,
        IOfferRepository Offer,
        DialogService dialogService,
        CalculationInteractionState interactionState,
        CalculationService calculationService) : IDisposable
    {
        public event Action<int>? OfferStateChanged;

        // Effective Visare permission ⇒ block grid mutations with a clear message.
        private bool DenyIfReadOnly()
        {
            if (_folderState.Calculation is { CanEdit: false })
            {
                Mhd.MessageOk("Behörighet", "Du har visningsbehörighet och kan inte ändra den här kalkylen.", MhdState.Warning);
                return true;
            }
            return false;
        }

        public static bool AffectsCalculation(ResourceListMVVM oldR, ResourceListMVVM newR)
        {
            // عدّل حسب منطقك الحقيقي
            if (oldR.Quantity != newR.Quantity) return true;
            //if (oldR.AccountId != newR.AccountId) return true;
            if (oldR.ResourceTypeId != newR.ResourceTypeId) return true;
            //if (oldR.ResourceSortId != newR.ResourceSortId) return true;

            if (oldR.Data?.Cost != newR.Data?.Cost) return true;
            if (oldR.Data?.BaseCost != newR.Data?.BaseCost) return true;
            if (oldR.Data?.CapWaste != newR.Data?.CapWaste) return true;
            if (oldR.Data?.CapFromTask != newR.Data?.CapFromTask) return true;
            if (oldR.Data?.CO2 != newR.Data?.CO2) return true;
            if (oldR.Data?.ChangeFactor1 != newR.Data?.ChangeFactor1) return true;
            if (oldR.Data?.ChangeFactor2 != newR.Data?.ChangeFactor2) return true;
            if (HasEffectiveChangeFactor1Changed(oldR.Data, newR.Data)) return true;
            if (HasEffectiveBaseCostChanged(oldR.Data, newR.Data)) return true;
            if (HasEffectiveCostChanged(oldR, newR)) return true;

            return false;
        }

        private static bool HasEffectiveChangeFactor1Changed(ResourceMetadata? oldData, ResourceMetadata? newData)
        {
            bool oldHasParameters = oldData?.Parameters is { Count: > 0 };
            bool newHasParameters = newData?.Parameters is { Count: > 0 };

            if (!oldHasParameters && !newHasParameters)
                return false;

            return GetEffectiveChangeFactor1(oldData) != GetEffectiveChangeFactor1(newData);
        }

        private static bool HasEffectiveBaseCostChanged(ResourceMetadata? oldData, ResourceMetadata? newData)
        {
            bool oldHasAddOns = oldData?.AddOns is { Count: > 0 };
            bool newHasAddOns = newData?.AddOns is { Count: > 0 };

            if (!oldHasAddOns && !newHasAddOns)
                return false;

            return GetEffectiveBaseCost(oldData) != GetEffectiveBaseCost(newData);
        }

        private static bool HasEffectiveCostChanged(ResourceListMVVM oldR, ResourceListMVVM newR)
        {
            bool oldHasTimes = oldR.Data?.Times is { Count: > 0 };
            bool newHasTimes = newR.Data?.Times is { Count: > 0 };
            bool oldHasAddOns = oldR.Data?.AddOns is { Count: > 0 };
            bool newHasAddOns = newR.Data?.AddOns is { Count: > 0 };

            if (!oldHasTimes && !newHasTimes && !oldHasAddOns && !newHasAddOns)
                return false;

            return GetEffectiveCost(oldR) != GetEffectiveCost(newR);
        }

        private static decimal GetEffectiveChangeFactor1(ResourceMetadata? data)
        {
            if (data is null)
                return 0m;

            var parameters = data.Parameters;
            if (parameters is null || parameters.Count == 0)
                return data.ChangeFactor1;

            decimal product = 1m;
            for (int i = 0; i < parameters.Count; i++)
                product *= parameters[i].Value;

            return Math.Round(product, 4, MidpointRounding.AwayFromZero);
        }

        private static decimal GetEffectiveCost(ResourceListMVVM resource)
        {
            if (resource.Data is null)
                return 0m;

            var quantity = resource.Quantity ?? 0m;
            if (quantity <= 0m)
                return GetEffectiveBaseUnitCost(resource.Data, quantity);

            decimal totalVariable = quantity * GetEffectiveBaseUnitCost(resource.Data, quantity);
            var addOns = resource.Data.AddOns;

            if (addOns is not null)
            {
                for (int i = 0; i < addOns.Count; i++)
                    totalVariable += addOns[i].Quantity(quantity) * addOns[i].Cost;
            }

            return totalVariable / quantity;
        }

        private static decimal GetEffectiveBaseUnitCost(ResourceMetadata? data, decimal quantity)
        {
            if (data is null)
                return 0m;

            data.SyncTimesWithQuantity(quantity > 0m ? quantity : (decimal?)null);
            var times = data.Times;
            if (times is null || times.Count == 0)
                return data.Cost;

            if (quantity <= 0m)
                return 0m;

            decimal total = 0m;
            for (int i = 0; i < times.Count; i++)
                total += times[i].Quantity * times[i].Cost;

            return Math.Round(total / quantity, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal GetEffectiveBaseCost(ResourceMetadata? data)
        {
            if (data is null)
                return 0m;

            decimal total = data.BaseCost ?? 0m;
            var addOns = data.AddOns;

            if (addOns is not null)
            {
                for (int i = 0; i < addOns.Count; i++)
                    total += addOns[i].BaseCost;
            }

            return total;
        }

        public async Task HandleOfferAsync(ResourceListMVVM res)
        {
            if (DenyIfReadOnly())
                return;

            if (res.HasOfferSelected())
                await Offer.SetOfferToResourceAsync(res.Id, 0); // Minus
            else if (!res.HasOffer)
            {
                _ = await Offer.AddAsync(new PostOfferDTO()
                {
                    BaseCost = res.GetComputedBaseCost() ?? 0,
                    Cost = res.GetComputedCost(),
                    ResourceId = res.Id,
                });
            }
        }

        public async Task Context(ResourceListMVVM res, int taskId)
        {
            var list = context.BuildResourceContextMenu(res, taskId, () => Remove(res), async () => await Duplicate(res));
            await ContextMenuService.ShowMenuAsync(list);
        }

        public async Task Duplicate(ResourceListMVVM dusection)
        {
            if (DenyIfReadOnly())
                return;

            var calc = _folderState.Calculation;
            if (calc == null) return;

            PostStorygeDTO post = new()
            {
                Items = [new ResourceTaskItemDTO(dusection.Id, dusection.Quantity)],
                Type = CalculationItemType.resource,
                copyType = CopyType.Copy,
                WithCildren = true,
                ParentID = dusection.TaskId,
                NewCalcID = calc.Id,
                OldCalcID = calc.Id,
                IsOH = calc.OHFactors
            };

            bool result = await Storage.CreateItem(post);
            Mhd.Notifications(ToastType.Delete, result);
        }

        private ResourceListMVVM? Get(int id)
        {
            var calc = _folderState.Calculation;
            if (calc == null) return null;

            if (calc.TryGetResource(id, out var res))
                return res;

            // fallback
            return calc.Tasks.SelectMany(x => x.Resources).FirstOrDefault(x => x.Id == id);
        }

        public void FromOfferHub(OperationType ot, object obj)
        {
            var calc = _folderState.Calculation;
            if (calc == null) return;

            if (ot == OperationType.Remove)
            {
                if (!int.TryParse(obj?.ToString(), out var offerId))
                    return;

                // ✅ O(1) بدل SelectMany
                if (calc.TryGetOffer(offerId, out var offer) && offer != null)
                {
                    // نحتاج resource owner لإزالة العرض من قائمته
                    // بما أن كودك الأصلي ينادي res.RemoveOffer(offerId)، نبحث عن resource عبر OfferId بسرعة:
                    // (لو عندك Offer يحمل ResourceId استخدمه هنا مباشرة. إن لم يكن، نعمل fallback مرة واحدة)
                    var res = calc.Tasks.SelectMany(t => t.Resources).FirstOrDefault(r => r.Offers.Any(o => o.Id == offerId));
                    if (res != null)
                    {
                        res.RemoveOffer(offerId);
                        calc.OfferById.Remove(offerId);
                        OfferStateChanged?.Invoke(res.Id);
                    }
                }
                else
                {
                    // fallback safe
                    var res = calc.Tasks.SelectMany(t => t.Resources).FirstOrDefault(r => r.Offers.Any(o => o.Id == offerId));
                    if (res != null)
                    {
                        res.RemoveOffer(offerId);
                        calc.OfferById.Remove(offerId);
                        OfferStateChanged?.Invoke(res.Id);
                    }
                }
            }
            else if (ot == OperationType.Update)
            {
                HubDataDto? list = obj.FromJsonWeb<HubDataDto>();
                if (list == null) return;

                if (list.Data != null)
                {
                    List<ListOfferMVVM> listOO = (list.GetData<List<ListOfferDTO>>() ?? [])
                        .Select(x => x.ToListOfferMVVM())
                        .ToList();

                    for (int i = 0; i < listOO.Count; i++)
                    {
                        var newOffer = listOO[i];

                        // ✅ O(1)
                        if (calc.TryGetOffer(newOffer.Id, out var oldOffer) && oldOffer != null)
                            newOffer.CopyPropertiesTo(oldOffer);
                    }
                }
                else
                {
                    var res = Get(list.ParentId);
                    if (res == null) return;

                    if (string.IsNullOrEmpty(list.Parent))
                    {
                        res.OfferId = null;
                        res.SyncOfferSelection();
                        OfferStateChanged?.Invoke(res.Id);
                    }
                    else if (int.TryParse(list.Parent, out int offerID))
                    {
                        res.OfferId = offerID;
                        var off = res.Offers.FirstOrDefault(x => x.Id == res.OfferId);
                        if (off != null && res.Data != null)
                        {
                            res.Data.BaseCost = off.BaseCost;
                            res.Data.Cost = off.Cost;
                        }

                        res.SyncOfferSelection();
                        OfferStateChanged?.Invoke(res.Id);
                    }
                }
            }
            else if (ot == OperationType.Add)
            {
                var list = obj.FromJsonWeb<HubDataDto>();
                if (list == null) return;

                var offerDto = list.GetData<ListOfferDTO>();
                if (offerDto == null) return;

                var offer = offerDto.ToListOfferMVVM();

                var res = Get(list.ParentId);
                if (res == null) return;

                res.Offers.Add(offer);
                res.SyncOfferSelection();

                // تحديث Offer index فوراً
                calc.OfferById[offer.Id] = offer;
                OfferStateChanged?.Invoke(res.Id);
            }
        }

        public bool FromHub(OperationType ot, object obj)
        {
            var calc = _folderState.Calculation;
            if (calc == null) return false;

            if (ot == OperationType.RemoveRange)
            {
                var rlist = obj.FromJsonWeb<IEnumerable<int>>();
                if (rlist != null)
                    calc.RemoveResources(rlist);
            }
            else if (ot == OperationType.Update)
            {
                var resourceDto = obj.FromJsonWeb<ResourceListDTO>();
                if (resourceDto == null) return false;

                var newG = resourceDto.ToResourceListMVVM();

                var oldRes = Get(newG.Id);
                if (oldRes == null) return false;

                var affects = AffectsCalculation(oldRes, newG);

                newG.Ui.OfferClick = oldRes.Ui.OfferClick;
                newG.CopyPropertiesTo(oldRes);
                oldRes.SyncOfferSelection();

                calc.LastHubChangeAffectsCalc |= affects;
                return true;
            }
            else if (ot == OperationType.Add)
            {
                var list = obj.FromJsonWeb<HubDataDto>();
                if (list == null) return false;

                var resDto = list.GetData<ResourceListDTO>();
                if (resDto != null && !calc.ResourceById.ContainsKey(resDto.Id))
                    calc.Add(resDto.ToResourceListMVVM());
            }
            else if (ot == OperationType.AddRange)
            {
                var list = obj.FromJsonWeb<HubDataDto>();
                if (list == null) return false;

                var resourceDtos = list.GetData<List<ResourceListDTO>>();
                if (resourceDtos == null) return false;

                var resources = resourceDtos
                    .Where(resource => !calc.ResourceById.ContainsKey(resource.Id))
                    .Select(x => x.ToResourceListMVVM())
                    .ToList();

                if (resources.Count == 0)
                    return false;

                // ✅ إصلاح bug + أسرع
                calc.AddRangeResources(resources);
            }
            else if (ot == OperationType.MoveRange)
            {
                var list = obj.FromJsonWeb<Tuple<int, List<int>>>();
                if (list == null) return false;

                int targetTaskId = list.Item1;
                var ids = list.Item2;
                if (ids == null) return false;

                if (!calc.TryGetTask(targetTaskId, out var targetTask) || targetTask == null)
                    return false;

                // اجلب الموارد من index بدل SelectMany
                var moved = new List<ResourceListMVVM>(ids.Count);
                for (int i = 0; i < ids.Count; i++)
                {
                    if (calc.TryGetResource(ids[i], out var res) && res != null)
                        moved.Add(res);
                }

                calc.RemoveResources(ids);

                targetTask.Resources ??= [];
                for (int i = 0; i < moved.Count; i++)
                {
                    moved[i].TaskId = targetTaskId;
                    targetTask.Resources.Add(moved[i]);

                    // تحديث index
                    calc.ResourceById[moved[i].Id] = moved[i];
                }

                calc.FlatListDirty = true;
            }

            return true;
        }

        public void Remove(ResourceListMVVM resource)
        {
            if (DenyIfReadOnly())
                return;

            if (!interactionState.IsSelected(CalculationItemType.resource, resource.Id))
                Mhd.DeleteMessage(resource.Name, EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([resource.Id])));
            else
                Mhd.DeleteMessage("", EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([.. interactionState.SelectedItems.Select(x => x.Id)])));
        }

        public async Task ConfirmedRemoveAsync(List<int> items)
        {
            var calc = _folderState.Calculation;
            if (calc == null) return;

            bool result = await Repo.DeleteAsync(calc.Id, items);
            Mhd.Notifications(ToastType.Delete, result);
            if (!result)
                return;

            interactionState.ResetSelection();
            await calculationService.RefreshAfterStructuralMutationAsync();
            await dialogService.CloseAsync();
        }

        public void Dispose() { }
    }
}
