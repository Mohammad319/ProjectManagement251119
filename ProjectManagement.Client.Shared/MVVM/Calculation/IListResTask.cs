using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Shared.Base.Calculation;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public interface IListResTask
    {
        public string Name { get; set; }
        public double Order { get; set; }

        public string Note { get; }
        public List<string> UpperNote { get; }
        public string QuantityParam { get; }
        public double? Quantity { get; }
        public string Unit { get; }
        public double ChangeFactor1 { get; }
        public double ChangeFactor2 { get; }
        public double? Cap { get; }
        public bool Active { get; }
        public string Code { get; }
        public TaskType Type { get; }

        public bool IsOH { get; }
        public bool PriceSubInPrecent { get; }
        public double? PriceSubDB { get; }
        public double? PriceSubTaxDB { get; }
        public double? MinPrice { get; }
        public double? CeilingPrice { get; }
        public bool HasVoice { get; }
        public string Responsible { get; }



        public int? TaskId { get; set; }
        public string Opportunity { get; set; }
        public int Id { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public List<IListResTask> Tasks { get; set; }
        public List<IListResTask> Resources { get; set; }

        public double NetCostQ { get; }
        double NetCostTotaly { get; }
        public double PriceQ { get; }

        public double PriceQTax(double Tax);
        public double ApriceTotally { get; }
        public double ApriceTotallyTax(double Tax);
        public double TotalCO2 { get; }
        public double? BaseCost { get; }
        public double PriceSub { get; }
        public double PriceSubTotal { get; }
        public double Diff { get; }

        [JsonIgnore] public bool FilterVisible { get; set; }
        [JsonIgnore] public bool CollSpan { get; set; }
        [JsonIgnore] public bool HasUpdated { get; set; }
        [JsonIgnore] public bool IsDragOver { get; set; }

        public int? StatusId { get; set; }
        public int? OpportunityId { get; set; }

        //Resource
        public int? OfferId { get; set; }
        public int? AccountId { get; set; }
        public string Account { get; set; }
        public string AccountCode { get; set; }
        public int? ResourceSortId { get; set; }
        public int? ResourceTypeId { get; set; }
        public string ResName { get; set; }
        public string Sort { get; set; }
        public double Factor { get; set; }
        public List<ListOfferMVVM> Offers { get; set; }
    }
}
