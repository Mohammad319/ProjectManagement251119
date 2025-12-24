
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
    public class ResourceService(IResourceRepository Repo, IStorageRepository Storage,
        FolderState _folderState, MhdServices Mhd, ContextMenuService ContextMenuService,
        IContextMenuBuilderService context, IOfferRepository Offer, DialogService dialogService) : IDisposable
    {
        public async Task HandleOfferAsync(ResourceListMVVM res)
        {
            if (res.HasOfferSelected()) await Offer.SetOfferToResourceAsync(res.Id, 0);//Minus
            else if (!res.HasOffer)
            {
                int id = await Offer.AddAsync(new PostOfferDTO()
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
            PostStorygeDTO post = new()
            {
                Items = [new ResourceTaskItemDTO(dusection.Id, dusection.Quantity)],
                Type = CalculationItemType.resource,
                copyType = CopyType.Copy,
                WithCildren = true,
                ParentID = dusection.TaskId,
                NewCalcID = _folderState.Calculation.Id,
                OldCalcID = _folderState.Calculation.Id,
                IsOH = _folderState.Calculation.OHFactors
            };
            bool result = await Storage.CreateItem(post);
            Mhd.Notifications(ToastType.Delete, result);
        }

        public ResourceListMVVM Get(int id) => _folderState.Calculation.Tasks.SelectMany(x => x.Resources).FirstOrDefault(x => x.Id == id);

        public void FromOfferHub(OperationType ot, object obj)
        {
            if (ot == OperationType.Remove)
            {
                var res = _folderState.Calculation.Tasks.SelectMany(x => x.Resources).FirstOrDefault(x => x.Offers.Any(o => x.Id == int.Parse(obj.ToString())));
                res?.RemoveOffer(int.Parse(obj.ToString()));
            }
            else if (ot == OperationType.Update)
            {
                HubDataDto list = obj.FromJsonWeb<HubDataDto>();
                if (list.Data != null)
                {
                    List<ListOfferMVVM> listOO = list.GetData<List<ListOfferMVVM>>();
                    foreach (var newOffer in listOO)
                    {
                        ListOfferMVVM oldoffer = _folderState.Calculation.Tasks.SelectMany(x => x.Resources).SelectMany(x => x.Offers).FirstOrDefault(x => x.Id == newOffer.Id);
                        if (oldoffer != null) newOffer.CopyPropertiesTo(oldoffer);
                    }
                }
                else
                {
                    ResourceListMVVM res = _folderState.Calculation.Tasks.SelectMany(x => x.Resources).FirstOrDefault(x => x.Id == list.ParentId);
                    if (res != null)
                    {
                        if (string.IsNullOrEmpty(list.Parent)) res.OfferId = null;
                        else if (int.TryParse(list.Parent, out int offerID))
                        {
                            res.OfferId = offerID;
                            ListOfferMVVM off = res.Offers.FirstOrDefault(x => x.Id == res.OfferId);
                            if (off != null)
                            {
                                res.Data.BaseCost = off.BaseCost;
                                res.Data.Cost = off.Cost;
                            }
                        }
                    }
                }
            }
            else if (ot == OperationType.Add)
            {
                var list = obj.FromJsonWeb<HubDataDto>();
                var zz = list.GetData<ListOfferMVVM>();
                var res = Get(list.ParentId);
                res.Offers.Add(zz);
            }
        }
        public bool FromHub(OperationType ot, object obj)
        {
            if (ot == OperationType.RemoveRange)
            {
                var rlist = obj.FromJsonWeb<IEnumerable<int>>();
                _folderState.Calculation.RemoveResources(rlist);
            }
            else if (ot == OperationType.Update)
            {
                var newG = obj.FromJsonWeb<ResourceListMVVM>();
                var oldRes = Get(newG.Id);
                newG.OfferClick = oldRes.OfferClick;
                if (oldRes != null) newG.CopyPropertiesTo(oldRes);
            }
            else if (ot == OperationType.Add)
            {
                var list = obj.FromJsonWeb<HubDataDto>();
                var zz = list.GetData<ResourceListMVVM>();
                _folderState.Calculation.Add(zz);
            }
            else if (ot == OperationType.AddRange)
            {
                try
                {
                    var list = obj.FromJsonWeb<HubDataDto>();
                    var zz = list.GetData<List<ResourceListMVVM>>();
                    foreach (var res in zz)
                        _folderState.Calculation.Add(zz);
                }
                catch (Exception ex)
                {

                    Mhd.MessageOk("3", ex.Message + "----" + ex.StackTrace.ToString());
                }
            }
            else if (ot == OperationType.MoveRange)
            {
                var list = obj.FromJsonWeb<Tuple<int, List<int>>>();
                if (list == null) return false;
                var task = _folderState.Calculation.Tasks.FirstOrDefault(t => t.Id == list.Item1);
                if (task == null) return false;
                var resources = _folderState.Calculation.Tasks.SelectMany(x => x.Resources)
                    .Where(t => list.Item2.Any(b => t.Id == b)).ToList();
                if (resources == null) return false;
                _folderState.Calculation.RemoveResources(list.Item2);

                foreach (var resource in resources) resource.TaskId = list.Item1;
                task.Resources.AddRange(resources);
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
            bool result = await Repo.DeleteAsync(_folderState.Calculation.Id, items);
            SelectedData.Reset();
            Mhd.Notifications(ToastType.Delete, result);
            if (result) dialogService.Close();
        }

        public void Dispose() { }
    }
}
