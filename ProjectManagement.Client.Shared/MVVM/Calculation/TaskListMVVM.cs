using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public class TaskFromData: TaskBase
    {
        public TaskData Data { get; set; } = new();

        public string Note => Data.Note;
        public List<string> UpperNote => Data.UpperNote;
        public string QuantityParam => Data.QuantityParam;
        public double? Quantity => Data.Quantity;
        public string Unit => Data.Unit;
        public double ChangeFactor1 => Data.ChangeFactor1;
        public double ChangeFactor2 => Data.ChangeFactor2;
        public double? Cap => Data.Cap;
        public bool Active => Data.Active;
        public string Code => Data.Code;
        public TaskType Type => Data.Type;

        public bool IsOH => Data.IsOH;
        public bool PriceSubInPrecent => Data.PriceSubInPrecent;
        public double? PriceSubDB => Data.PriceSubDB;
        public double? PriceSubTaxDB => Data.PriceSubTaxDB;
        public double? MinPrice => Data.MinPrice;
        public double? CeilingPrice => Data.CeilingPrice;
        public bool HasVoice => Data.HasVoice;
        public string Responsible => Data.Responsible;
    }
    public class TaskListMVVM : TaskFromData, ICalcRow
    {
        public int Version { get; set; } = 0;

        public override bool Equals(object obj)
        {
            if (obj is not TaskListMVVM other)
                return false;

            return Id == other.Id &&
                   CollSpan == other.CollSpan &&
                   (Data?.Active == other.Data?.Active) &&
                   (Data?.Quantity == other.Data?.Quantity);
        }

        public override int GetHashCode()
        {
            // دمج خصائص مهمة في حساب الهاش كود
            return HashCode.Combine(Id, CollSpan, Data?.Active, Data?.Quantity);
        }
        public TaskListMVVM()
        {
            Data ??= new();
            Resources = [];
            Tasks = [];
        }
        public int? TaskId { get; set; }
        public string Opportunity { get; set; }
        public int Id { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public List<TaskListMVVM> Tasks { get; set; }
        public List<ResourceListMVVM> Resources { get; set; }
        public double PriceQTax(double Tax) => PriceQ * (1 + (Tax / 100));
        public double ApriceTotallyTax(double Tax) => ApriceTotally * (1 + (Tax / 100));

        private double? _netCostQ;
        private double? _netCostTotaly;
        private double? _priceQ;
        private double? _apriceTotally;
        private double? _totalCO2;
        private double? _baseCost;
        private double? _priceSub;
        private double? _priceSubTotal;
        private double? _diff;
        public void InvalidateCache()
        {
            _netCostQ = null;
            _netCostTotaly = null;
            _apriceTotally = null;
            _totalCO2 = null;
            _baseCost = null;
            _priceSub = null;
            _priceSubTotal = null;
            _diff = null;
            foreach (var res in Resources)
            {
                res.InvalidateCache();
            }
        }

        [JsonIgnore] public double NetCostQ => _netCostQ ??= Tasks.Where(x => x.Active).Sum(x => x.NetCostQ) + Resources.Where(x => x.Active).Sum(x => x.NetCostQ);
        [JsonIgnore] public double NetCostTotaly => _netCostTotaly ??= Tasks.Where(x => x.Active).Sum(x => x.NetCostTotaly) + Resources.Where(x => x.Active).Sum(x => x.NetCostTotaly);
        [JsonIgnore]public double PriceQ => _priceQ ??= Data.Quantity.HasValue && Data.Quantity > 0 ? ApriceTotally / Data.Quantity.Value : 0;
        [JsonIgnore] public double ApriceTotally => _apriceTotally ??= Tasks.Where(x => x.Active).Sum(x => x.ApriceTotally)+ Resources.Where(x => x.Active).Sum(x => x.ApriceTotally);
        [JsonIgnore] public double? TotalCO2 => _totalCO2 ??= Tasks.Sum(x => x.TotalCO2) + (double)(Resources?.Where(x => x.Active)?.Sum(x => x.TotalCO2));
        [JsonIgnore] public double? BaseCost => _baseCost ??= Tasks?.Where(x => x.Active)?.Sum(x => x.BaseCost) + Resources?.Where(x => x.Active)?.Sum(x => x.BaseCost);
        [JsonIgnore] public double PriceSub => _priceSub ??= Data.PriceSubDB ?? Math.Round(PriceQ);
        [JsonIgnore] public double PriceSubTotal => _priceSubTotal ??= Data.Quantity.HasValue ? PriceSub * Data.Quantity.Value : 0;
        [JsonIgnore] public double Diff => _diff ??= PriceSubTotal - ApriceTotally;

        [JsonIgnore] public bool FilterVisible { get; set; } = true;
        [JsonIgnore] public bool CollSpan { get; set; } = true;
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
        public double Factor { get; set; } = 1;
        public List<ListOfferMVVM> Offers { get; set; } = [];

        public bool HasOffer => Offers != null && Offers.Count != 0;
        public bool HasOfferSelected()
        {
            return OfferId.HasValue && Offers != null && Offers.FirstOrDefault(x => x.Id == OfferId) != null;
        }
        public ResourceTypesEnum ResType { get; set; }
        [JsonIgnore] public bool HasCap => ResType is (ResourceTypesEnum.MachinesAndEquipments or ResourceTypesEnum.Worker);
        [JsonIgnore] public bool HasWast => ResType is ResourceTypesEnum.Materials;
    }
}