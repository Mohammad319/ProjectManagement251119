using BlazorMHD.UI.Core.Services;
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Shared.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Pages.Calculation.Table
{
    public partial class SummarySheet
    {
        bool DiscrptionShow;
        CalculationMVVM Calculation => Calc;
        private decimal TaxMultiplier => 1m + ((decimal)Calculation.Tax / 100m);
        private static decimal ParseDecimal(ChangeEventArgs e) =>
            NumericInputHelper.TryParseDecimal(e.Value?.ToString(), out var value) ? value : 0m;
        private static string FactorRowKey(Factors factor) =>
            $"{(int)factor.ResourceType}:{factor.ResId}:{factor.SortId}";

        void SomeHasChanged()
        {
            EarningOnChange = true;
            CalcultationExtensions.AssignFactorsToResourcesFast(Calculation);
            //Calculation.AssignFactorsToResources();

            Calculation.TenderExcelTax = Calculation.Factors.Sum(x => x.PriceOG);
            ProfitDecisionFun();
            Calculation.TenderInclTax = Calculation.TenderExcelTax * TaxMultiplier;
        }
        [Parameter] public bool IsReport { get; set; }
        bool ShowAllSort = true;

        async Task SaveAsync()
        {
            await ExtraFactorsSaveAsync();
            Calculation.TenderExcelTax = Calculation.Factors.Sum(x => x.PriceOG);
            ProfitDecisionFun();
            Calculation.TenderInclTax = Calculation.TenderExcelTax * TaxMultiplier;
        }
        void TenderExcelTaxChanged(ChangeEventArgs e)
        {
            Calculation.TenderExcelTax = ParseDecimal(e);
            Calculation.TenderInclTax = Calculation.TenderExcelTax * TaxMultiplier;
            ProfitDecisionFun();
            Calculation.CalcEarningsForUnlockedRes();
            EarningOnChange = true;
            StateHasChanged();
        }
        void TenderInclTaxChanged(ChangeEventArgs e)
        {
            Calculation.TenderInclTax = ParseDecimal(e);
            Calculation.TenderExcelTax = TaxMultiplier == 0
                ? 0
                : Calculation.TenderInclTax / TaxMultiplier;
            ProfitDecisionFun();
            Calculation.CalcEarningsForUnlockedRes();
            EarningOnChange = true;
            StateHasChanged();
        }
        private double Format(double x) => Template.Format(x);
        private decimal Format(decimal x) => Template.Format(x);
        private static string FormatInput(decimal x) => NumericDisplayHelper.Format(x);
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
            if (Folder?.State?.Calculation != null)
                Folder.State.Calculation.OnChangeInCalculation += ChangeCalcultionItems;
            Calculation.TenderExcelTax = Calculation.Factors.Sum(x => x.PriceOG);
            ProfitDecisionFun();
            Calculation.TenderInclTax = Calculation.TenderExcelTax * TaxMultiplier;
        }
        void ChangeProfit(decimal e)
        {
            EarningOnChange = true;
            Calculation.ProfitDecision = e;
            Calculation.CalcEarningsForUnlockedRes();
            Calculation.TenderExcelTax = Calculation.Factors.Sum(x => x.PriceOG);
            Calculation.TenderInclTax = Calculation.TenderExcelTax * TaxMultiplier;
            StateHasChanged();
        }
        //ProfitDecision = ((TenderExcelTax/ TotalSum) – 1) * 100
        //Calculation.TenderExcelTax = ((Calculation.ProfitDecision / 100) + 1) * Calculation.Sum;
        void ProfitDecisionFun()
        {
            Calculation.ProfitDecision = Calculation.Sum == 0
                ? 0
                : ((Calculation.TenderExcelTax / Calculation.Sum) - 1m) * 100m;
        }
        public void Dispose()
        {
            if (Folder?.State?.Calculation != null)
                Folder.State.Calculation.OnChangeInCalculation -= ChangeCalcultionItems;
        }
        void ChangeCalcultionItems() => _ = this.InvokeAsync(StateHasChanged);

    }
}
