using BlazorMHD.UI.Core.Services;
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Pages.Calculation.Table
{
    public partial class SummarySheet
    {
        bool DiscrptionShow;
        CalculationMVVM Calculation => Calc;
        void SomeHasChanged()
        {
            EarningOnChange = true;
            CalcultationExtensions.AssignFactorsToResourcesFast(Calculation);
            //Calculation.AssignFactorsToResources();

            Calculation.TenderExcelTax = Calculation.Factors.Sum(x => x.PriceOG);
            ProfitDecisionFun();
            Calculation.TenderInclTax = Calculation.TenderExcelTax * (1m + ((decimal)Calculation.Tax / 100m));
        }
        [Parameter] public bool IsReport { get; set; }
        bool ShowAllSort = true;

        async Task SaveAsync()
        {
            await ExtraFactorsSaveAsync();
            Calculation.TenderExcelTax = Calculation.Factors.Sum(x => x.PriceOG);
            ProfitDecisionFun();
            Calculation.TenderInclTax = Calculation.TenderExcelTax * (1m + ((decimal)Calculation.Tax / 100m));
        }
        void TenderExcelTaxChanged(ChangeEventArgs e)
        {
            Calculation.TenderExcelTax = decimal.Parse(e.Value?.ToString() ?? "0");
            Calculation.TenderInclTax = Calculation.TenderExcelTax * (1m + ((decimal)Calculation.Tax / 100m));
            ProfitDecisionFun();
            Calculation.CalcEarningsForUnlockedRes();
            EarningOnChange = true;
            StateHasChanged();
        }
        void TenderInclTaxChanged(ChangeEventArgs e)
        {
            Calculation.TenderInclTax = decimal.Parse(e.Value?.ToString() ?? "0");
            Calculation.TenderExcelTax = Calculation.TenderInclTax / (1m + ((decimal)Calculation.Tax / 100m));
            ProfitDecisionFun();
            Calculation.CalcEarningsForUnlockedRes();
            EarningOnChange = true;
            StateHasChanged();
        }
        private double Format(double x) => Template.Format(x);
        public string SelectedFactor(string res)
        {
            var res45 = Calculation.Factors.FirstOrDefault(x => x.ResId + "," + x.SortId == res);
            if (res45 != null) return res45.ResName + (res45.SortId > 0 ? " , " + res45.Sort : "");
            return "";
        }
        private void TemplateDialog() =>
    Modal.ShowComponent<Template.TemplateSetDefaultUI>(ResourceLoc.templates, new Dictionary<string, object>()
    {
        [nameof(Pages.Calculation.Template.TemplateSetDefaultUI.Tab)] = 3,
    }, DialogSize.ExtraLarge);
        protected override void OnInitialized()
        {
            Folder.State.Calculation.OnChangeInCalculation += ChangeCalcultionItems;
            Calculation.TenderExcelTax = Calculation.Factors.Sum(x => x.PriceOG);
            ProfitDecisionFun();
            Calculation.TenderInclTax = Calculation.TenderExcelTax * (1m + ((decimal)Calculation.Tax / 100m));
        }
        void ChangeProfit(decimal e)
        {
            EarningOnChange = true;
            Calculation.ProfitDecision = e;
            Calculation.CalcEarningsForUnlockedRes();
            Calculation.TenderExcelTax = Calculation.Factors.Sum(x => x.PriceOG);
            Calculation.TenderInclTax = Calculation.TenderExcelTax * (1m + ((decimal)Calculation.Tax / 100m));
            StateHasChanged();
        }
        //ProfitDecision = ((TenderExcelTax/ TotalSum) – 1) * 100
        //Calculation.TenderExcelTax = ((Calculation.ProfitDecision / 100) + 1) * Calculation.Sum;
        void ProfitDecisionFun()
        {
            Calculation.ProfitDecision = ((Calculation.TenderExcelTax / Calculation.Sum) - 1m) * 100m;
        }
        public void Dispose()
        {
            if (Folder?.State?.Calculation != null)
                Folder.State.Calculation.OnChangeInCalculation -= ChangeCalcultionItems;
        }
        void ChangeCalcultionItems() => _ = this.InvokeAsync(StateHasChanged);

    }
}
