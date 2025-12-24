using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.Calculation;
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.DTO.Offer;

namespace ProjectManagement.Client.Services.Calculation
{
    public class CalculationService(
        ITemplateRepository templateRepo,
        ICalculationRepository calcRepo,
        FolderState folderState, MhdServices mhdServices)
    {
        public bool ShowComments { get; set; } = true;
        public ListOfferDTO? Offer { get; set; }

        public event EventHandler PageChanged = delegate { };

        private async Task<CalculationMVVM?> NewCalculation(int id)
        {
            var calculation = await calcRepo.GetPageAsync(id, folderState.OtherDepartment);

            if (calculation is null)
                return null;

            SelectedData.Reset();
            calculation.Id = id;
            calculation.BuildTaskHierarchy();

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
                        var updated = obj.FromJsonWeb<OpportunityModel>();
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
                        var added = obj.FromJsonWeb<OpportunityModel>();
                        if (added != null)
                            opportunities.Add(added);
                        break;
                    }
            }
        }

        public void FromHub(OperationType ot, object obj)
        {
            if (ot != OperationType.Update || folderState.Calculation is null) return;

            var calcN = obj.FromJsonWeb<CalculationMVVM>();
            if (calcN is null) return;

            // احتفظ بالـ Tasks الحالية
            calcN.Tasks = folderState.Calculation.Tasks;
            calcN.CopyPropertiesTo(folderState.Calculation);
        }

        public void GetFilter(FilterVM? filter)
        {
            if (folderState.Calculation is null) return;

            filter ??= new FilterVM();

            folderState.Calculation.FilterVM = filter;
            CalculationStaticFun.Filter(filter, folderState.Calculation.Tasks);
        }
    }
}
