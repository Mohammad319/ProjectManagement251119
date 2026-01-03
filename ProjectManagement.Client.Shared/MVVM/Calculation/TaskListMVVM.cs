using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public static class TaskConvert
    {
        public static TaskPostDTO GetToPost(TaskListMVVM task)
        {
            if (task == null) return new TaskPostDTO();
            return new TaskPostDTO()
            {
                Id = task.Id,
                ParentTaskId = task.TaskId,
                StatusId = task.StatusId,
                OpportunityId = task.OpportunityId,
                Note = task.Note,
                Quantity = task.Quantity,
                Unit = task.Unit,
                ChangeFactor1 = task.ChangeFactor1,
                ChangeFactor2 = task.ChangeFactor2,
                Cap = task.Cap,
                IsActive = task.Active,
                Code = task.Code,
                Type = task.Type,
                IsOH = task.IsOH,
                Metadata = task.Metadata,
                Colspan = task.CollSpan,
                //Resources = task.Resources.Select(r => ResourceConvert.GetToPost(r)).ToList(),
                Tasks = [.. task.Tasks.Select(t => GetToPost(t))],
                Name = task.Name,
                Order = task.Order,
                WorkedQ = task.Metadata.WorkedQ,
                ActuallyQuantity = task.Metadata.ActuallyQuantity,

                //OnlyCodeText = task.Metadata.on

            };
        }
    }
    public class TaskListMVVM : TaskBase
    {
        public TaskMetadata Metadata { get; set; } = new();

        public string Note => Metadata.Note;
        public List<string> UpperNote => Metadata.UpperNote;
        public string QuantityParam => Metadata.QuantityParam;
        public double? Quantity => Metadata.Quantity;
        public string Unit => Metadata.Unit;
        public double ChangeFactor1 => Metadata.ChangeFactor1;
        public double ChangeFactor2 => Metadata.ChangeFactor2;
        public double ActuallyQuantity => Metadata.ActuallyQuantity;
        public double WorkedQ => Metadata.WorkedQ;
        public double WorkedQPercent => Metadata.WorkedQ / Metadata.ActuallyQuantity;

        public double? Cap => Metadata.Cap;
        public bool Active => Metadata.IsActive;
        public string Code => Metadata.Code;
        public TaskType Type => Metadata.Type;

        public bool IsOH => Metadata.IsOH;
        public bool PriceSubInPrecent => Metadata.PriceSubInPrecent;
        public double? PriceSubDB => Metadata.PriceSubDB;
        public double? PriceSubTaxDB => Metadata.PriceSubTaxDB;
        public double? MinPrice => Metadata.MinPrice;
        public double? CeilingPrice => Metadata.CeilingPrice;
        public double PriceActuallyQuantity => ActuallyQuantity * PriceSub;
        public double PriceWorkedQ => WorkedQ * PriceSub;
        public double PriceSubTax(double tax) => PriceSub * (1 + tax);
        public double PriceTotalSubTax(double tax) => PriceSubTax(tax) * (1 + tax);
        public double PriceActuallyQuantityTax(double tax) => PriceActuallyQuantity * (1 + tax);
        public double PriceWorkedQTax(double tax) => PriceWorkedQ * (1 + tax);


        public bool HasVoice => Metadata.HasVoice;
        public string Responsible => Metadata.Responsible;

        public int Version { get; set; } = 0;

        public override bool Equals(object obj)
        {
            if (obj is not TaskListMVVM other)
                return false;

            return Id == other.Id &&
                   CollSpan == other.CollSpan &&
                   (Metadata?.IsActive == other.Metadata?.IsActive) &&
                   (Metadata?.Quantity == other.Metadata?.Quantity);
        }

        public override int GetHashCode()
        {
            // دمج خصائص مهمة في حساب الهاش كود
            return HashCode.Combine(Id, CollSpan, Metadata?.IsActive, Metadata?.Quantity);
        }
        public TaskListMVVM()
        {
            Metadata ??= new();
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
        [JsonIgnore] public double PriceQ => _priceQ ??= Metadata.Quantity.HasValue && Metadata.Quantity > 0 ? ApriceTotally / Metadata.Quantity.Value : 0;
        [JsonIgnore] public double ApriceTotally => _apriceTotally ??= Tasks.Where(x => x.Active).Sum(x => x.ApriceTotally) + Resources.Where(x => x.Active).Sum(x => x.ApriceTotally);
        [JsonIgnore] public double? TotalCO2 => _totalCO2 ??= Tasks.Sum(x => x.TotalCO2) + (double)(Resources?.Where(x => x.Active)?.Sum(x => x.TotalCO2));
        [JsonIgnore] public double? BaseCost => _baseCost ??= Tasks?.Where(x => x.Active)?.Sum(x => x.BaseCost) + Resources?.Where(x => x.Active)?.Sum(x => x.BaseCost);
        [JsonIgnore] public double PriceSub => _priceSub ??= Metadata.PriceSubDB ?? Math.Round(PriceQ);
        [JsonIgnore] public double PriceSubTotal => _priceSubTotal ??= Metadata.Quantity.HasValue ? PriceSub * Metadata.Quantity.Value : 0;
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