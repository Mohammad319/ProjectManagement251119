using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.Repositories.Offer;
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
        DialogService dialogService) : IDisposable
    {
        public static bool AffectsCalculation(ResourceListMVVM oldR, ResourceListMVVM newR)
        {
            // عدّل حسب منطقك الحقيقي
            if (oldR.Quantity != newR.Quantity) return true;
            //if (oldR.AccountId != newR.AccountId) return true;
            if (oldR.ResourceTypeId != newR.ResourceTypeId) return true;
            //if (oldR.ResourceSortId != newR.ResourceSortId) return true;

            if (oldR.Data?.Quantity != newR.Data?.Quantity) return true;
            if (oldR.Data?.Cost != newR.Data?.Cost) return true;
            if (oldR.Data?.BaseCost != newR.Data?.BaseCost) return true;
            if (oldR.Data?.CapWaste != newR.Data?.CapWaste) return true;
            if (oldR.Data?.CO2 != newR.Data?.CO2) return true;
            if (oldR.Data?.ChangeFactor1 != newR.Data?.ChangeFactor1) return true;
            if (oldR.Data?.ChangeFactor2 != newR.Data?.ChangeFactor2) return true;

            return false;
        }

        public async Task HandleOfferAsync(ResourceListMVVM res)
        {
            if (res.HasOfferSelected())
                await Offer.SetOfferToResourceAsync(res.Id, 0); // Minus
            else if (!res.HasOffer)
            {
                _ = await Offer.AddAsync(new PostOfferDTO()
                {
                    BaseCost = res.BaseCost ?? 0,
                    Cost = res.Cost,
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
                    res?.RemoveOffer(offerId);
                }
                else
                {
                    // fallback safe
                    var res = calc.Tasks.SelectMany(t => t.Resources).FirstOrDefault(r => r.Offers.Any(o => o.Id == offerId));
                    res?.RemoveOffer(offerId);
                }
            }
            else if (ot == OperationType.Update)
            {
                HubDataDto? list = obj.FromJsonWeb<HubDataDto>();
                if (list == null) return;

                if (list.Data != null)
                {
                    List<ListOfferMVVM> listOO = list.GetData<List<ListOfferMVVM>>() ?? [];

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
                        res.OfferId = null;
                    else if (int.TryParse(list.Parent, out int offerID))
                    {
                        res.OfferId = offerID;
                        var off = res.Offers.FirstOrDefault(x => x.Id == res.OfferId);
                        if (off != null && res.Data != null)
                        {
                            res.Data.BaseCost = off.BaseCost;
                            res.Data.Cost = off.Cost;
                        }
                    }
                }
            }
            else if (ot == OperationType.Add)
            {
                var list = obj.FromJsonWeb<HubDataDto>();
                if (list == null) return;

                var offer = list.GetData<ListOfferMVVM>();
                if (offer == null) return;

                var res = Get(list.ParentId);
                if (res == null) return;

                res.Offers.Add(offer);

                // تحديث Offer index فوراً
                calc.OfferById[offer.Id] = offer;
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
                var newG = obj.FromJsonWeb<ResourceListMVVM>();
                if (newG == null) return false;

                var oldRes = Get(newG.Id);
                if (oldRes == null) return false;

                var affects = AffectsCalculation(oldRes, newG);

                newG.OfferClick = oldRes.OfferClick;
                newG.CopyPropertiesTo(oldRes);

                calc.LastHubChangeAffectsCalc |= affects;
                return true;
            }
            else if (ot == OperationType.Add)
            {
                var list = obj.FromJsonWeb<HubDataDto>();
                if (list == null) return false;

                var res = list.GetData<ResourceListMVVM>();
                if (res != null)
                    calc.Add(res);
            }
            else if (ot == OperationType.AddRange)
            {
                var list = obj.FromJsonWeb<HubDataDto>();
                if (list == null) return false;

                var resources = list.GetData<List<ResourceListMVVM>>();
                if (resources == null) return false;

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
            if (!SelectedData.ExistItem(CalculationItemType.resource, resource.Id))
                Mhd.DeleteMessage(resource.Name, EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([resource.Id])));
            else
                Mhd.DeleteMessage("", EventCallback.Factory.Create(this, () => ConfirmedRemoveAsync([.. SelectedData.SelectedItems.Select(x => x.Id)])));
        }

        public async Task ConfirmedRemoveAsync(List<int> items)
        {
            var calc = _folderState.Calculation;
            if (calc == null) return;

            bool result = await Repo.DeleteAsync(calc.Id, items);
            SelectedData.Reset();
            Mhd.Notifications(ToastType.Delete, result);
            if (result) dialogService.Close();
        }

        public void Dispose() { }
    }
}
