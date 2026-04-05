using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.Calculation;
using ProjectManagement.Client.Shared.Mapping;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;

namespace ProjectManagement.Client.Services.Calculation
{
    public class CalculationService(
        ITemplateRepository templateRepo,
        ICalculationRepository calcRepo,
        FolderState folderState,
        MhdServices mhdServices,
        CalculationInteractionState interactionState)
    {
        private bool showComments = true;

        public event Action? CommentsVisibilityChanged;
        public event Action? GridViewMaterialized;

        public bool ShowComments
        {
            get => showComments;
            set
            {
                if (showComments == value)
                    return;

                showComments = value;
                CommentsVisibilityChanged?.Invoke();
            }
        }

        public ListOfferDTO? Offer { get; set; }

        public event EventHandler PageChanged = delegate { };

        public void RequestGridRefresh(CalculationGridRefreshKind kind = CalculationGridRefreshKind.View)
        {
            var calculation = folderState.Calculation;
            if (calculation == null)
                return;

            switch (kind)
            {
                case CalculationGridRefreshKind.Structure:
                    calculation.NotifyGridRefresh(flatListDirty: true, structureFlatListDirty: true);
                    break;

                case CalculationGridRefreshKind.FlatList:
                    calculation.NotifyGridRefresh(flatListDirty: true);
                    break;

                default:
                    calculation.NotifyGridRefresh(flatListDirty: false);
                    break;
            }
        }

        public void NotifyGridViewMaterialized() => GridViewMaterialized?.Invoke();

        private async Task<CalculationMVVM?> NewCalculation(int id)
        {
            var calculation = await calcRepo.GetPageAsync(id, folderState.OtherDepartment);

            if (calculation is null)
                return null;

            interactionState.ResetSelection();
            calculation.Id = id;
            calculation.BuildTaskHierarchy();
            calculation.RebuildHierarchyAndIndexes();

            if (calculation.TemplateId > 0)
            {
                calculation.Template = await templateRepo.GetByIdAsync(calculation.TemplateId.Value) ?? new();
            }
            else
            {
                calculation.Template = new();
            }
            return calculation;
        }

        // Select Calc from Table (عندما يكون المجلد/المشروع محددين مسبقًا)
        public async Task SetCalc(int id)
        {
            mhdServices.Loading.Begin();
            var calc = await NewCalculation(id);
            folderState.SetCalculation(calc);
            PageChanged(this, EventArgs.Empty);
            mhdServices.Loading.End();
        }

        public async Task SetCalc(int id, ListProjectMVVM project, FolderMVVM folder)
        {
            mhdServices.Loading.Begin();
            var calc = await NewCalculation(id);
            folderState.SetCalculation(calc, project, folder);
            PageChanged(this, EventArgs.Empty);
            mhdServices.Loading.End();
        }

        public void FromOperationHub(OperationType ot, object obj)
        {
            if (folderState.Calculation is null) return;

            var opportunities = folderState.Calculation.Opportunities ??= [];

            switch (ot)
            {
                case OperationType.Update:
                    {
                        var updated = obj.FromJsonWeb<OpportunityListDTO>()?.ToOpportunityModel();
                        if (updated is null) return;

                        var existing = opportunities.FirstOrDefault(x => x.Id == updated.Id);
                        if (existing != null)
                            updated.CopyPropertiesTo(existing);
                        break;
                    }

                case OperationType.Remove:
                    {
                        if (int.TryParse(obj?.ToString(), out var idToRemove))
                        {
                            var toRemove = opportunities.FirstOrDefault(x => x.Id == idToRemove);
                            if (toRemove != null)
                                opportunities.Remove(toRemove);
                        }
                        break;
                    }

                case OperationType.Add:
                    {
                        var added = obj.FromJsonWeb<OpportunityListDTO>()?.ToOpportunityModel();
                        if (added != null)
                            opportunities.Add(added);
                        break;
                    }
            }
        }

        public void FromHub(OperationType ot, object obj)
        {
            if (ot != OperationType.Update || folderState.Calculation is null) return;

            var calcDto = obj.FromJsonWeb<CalculationPageDTO>();
            if (calcDto is null) return;

            // السيرفر يرسل DTO جزئي هنا، لذلك نحدّث فقط الحقول التي يضمن إرسالها
            // بدل إسقاطه على CalculationMVVM كاملة وإعادة قيم أخرى إلى defaults.
            foreach (var item in calcDto.Factors.Select(x => x.ToFactors()))
            {
                var neuF = folderState.Calculation.Factors
                    .FirstOrDefault(x => x.ResourceType == item.ResourceType &&
                    x.ResId == item.ResId && x.SortId == item.SortId);
                if (neuF != null)
                {
                    neuF.Factor = item.Factor;
                    //neuF.NetCostTotaly = item.NetCostTotaly;
                    //neuF.NetCostTotalyOH = item.NetCostTotalyOH;
                    neuF.Earnings = item.Earnings;
                    neuF.IsLocked = item.IsLocked;
                    neuF.Key = item.Key;
                }
                else
                {
                    folderState.Calculation.Factors.Add(item);
                }
            }

            folderState.Calculation.QuanityList = calcDto.QuanityList ?? [];
            folderState.Calculation.Tax = (int)calcDto.Tax;
            folderState.Calculation.Name = calcDto.Name ?? string.Empty;
            folderState.Calculation.Code = calcDto.Code ?? string.Empty;
            folderState.Calculation.Supervisor = calcDto.Supervisor ?? string.Empty;
            folderState.Calculation.Inspector = calcDto.Inspector ?? string.Empty;
            folderState.Calculation.TemplateId = calcDto.TemplateId;
        }

        public void GetFilter(FilterVM? filter)
        {
            if (folderState.Calculation is null) return;

            filter ??= new FilterVM();

            folderState.Calculation.FilterVM = filter;
            CalculationStaticFun.Filter(filter, folderState.Calculation.Tasks);
            RequestGridRefresh(CalculationGridRefreshKind.FlatList);
        }
    }
}
