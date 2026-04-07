
using ProjectManagement.Shared.Base.Calculation;
using System.Linq;
using System.Threading.Tasks;
namespace ProjectManagement.Client.Pages.Project.Component.Calculation.Table
{
    public class BaseFactors : AppComponentBase
    {
        public bool IsLoading = false;
        protected bool HasNewProfitRatio = false;
        protected override void OnInitialized()
        {
            TemplateConst.Col0 = 40;
        }
        protected bool EarningOnChange = false;

        protected async Task ExtraFactorsSaveAsync()
        {
            var tt = Calc.Factors.Where(x => x.NetCostTotalyOH > 0 || x.NetCostTotaly > 0).Select(x =>
                new OHFactors
                {
                    Earnings = x.Earnings,
                    ResourceType = x.ResourceType,
                    SortId = x.SortId,
                    ResId = x.ResId,
                    IsLocked = x.IsLocked,
                    Selected = x.Selected,
                    Unit = x.Unit,
                    DivisionKey = x.DivisionKey,
                    Key = x.Key,

                }).ToList();
            await Repo.Calculation.UpdateAsync(tt, Calc.Id);
            EarningOnChange = false;
            IsLoading = false;
        }
    }
}
