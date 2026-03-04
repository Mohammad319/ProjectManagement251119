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
                Tasks = [.. task.Tasks.Select(t => GetToPost(t))],
                Name = task.Name,
                Order = task.Order,
                WorkedQ = task.Metadata.WorkedQ,
                ActuallyQuantity = task.Metadata.ActuallyQuantity,
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

        // ⚠️ تجنب قسمة على صفر
        public double WorkedQPercent => (Metadata.ActuallyQuantity == 0) ? 0 : (Metadata.WorkedQ / Metadata.ActuallyQuantity);

        public double? Cap => Metadata.Cap;
        public bool Active => Metadata.IsActive;
        public string Code => Metadata.Code;
        public TaskType Type => Metadata.Type;

        public bool IsOH => Metadata.IsOH;
        public bool PriceSubInPrecent => Metadata.PriceSubInPrecent;
        public decimal? PriceSubDB => Metadata.PriceSubDB;
        public decimal? PriceSubTaxDB => Metadata.PriceSubTaxDB;
        public decimal? MinPrice => Metadata.MinPrice;
        public decimal? CeilingPrice => Metadata.CeilingPrice;

        // =========================
        // ✅ محسوبات سريعة (لا LINQ)
        // =========================
        [JsonIgnore] public decimal Calc_NetCostQ { get; set; }
        [JsonIgnore] public decimal Calc_NetCostTotaly { get; set; }
        [JsonIgnore] public decimal Calc_ApriceTotally { get; set; }
        [JsonIgnore] public double? Calc_TotalCO2 { get; set; }
        [JsonIgnore] public decimal? Calc_BaseCost { get; set; }

        [JsonIgnore] public decimal NetCostQ => Calc_NetCostQ;
        [JsonIgnore] public decimal NetCostTotaly => Calc_NetCostTotaly;
        [JsonIgnore] public decimal ApriceTotally => Calc_ApriceTotally;

        [JsonIgnore]
        public decimal PriceQ =>
            (Metadata.Quantity.HasValue && Metadata.Quantity.Value > 0)
                ? (Calc_ApriceTotally / (decimal)Metadata.Quantity.Value)
                : 0;

        [JsonIgnore] public double? TotalCO2 => Calc_TotalCO2;
        [JsonIgnore] public decimal? BaseCost => Calc_BaseCost;

        // هذه بقيت “خفيفة” (ما فيها LINQ)
        [JsonIgnore] public decimal PriceSub => Metadata.PriceSubDB ?? Math.Round(PriceQ);
        [JsonIgnore] public decimal PriceSubTotal => Metadata.Quantity.HasValue ? PriceSub * (decimal)Metadata.Quantity.Value : 0;
        [JsonIgnore] public decimal Diff => PriceSubTotal - ApriceTotally;

        public decimal PriceQTax(double Tax) => PriceQ * (1 + ((decimal)Tax / 100));
        public decimal ApriceTotallyTax(double Tax) => ApriceTotally * (1 + ((decimal)Tax / 100));

        public decimal PriceActuallyQuantity => (decimal)ActuallyQuantity * PriceSub;
        public decimal PriceWorkedQ => (decimal)WorkedQ * PriceSub;
        public decimal PriceSubTax(double tax) => PriceSub * (1 + (decimal)tax);
        public decimal PriceTotalSubTax(double tax) => PriceSubTax(tax) * (1 + (decimal)tax);
        public decimal PriceActuallyQuantityTax(double tax) => PriceActuallyQuantity * (1 + (decimal)tax);
        public decimal PriceWorkedQTax(double tax) => PriceWorkedQ * (1 + (decimal)tax);

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

        public override int GetHashCode() =>
            HashCode.Combine(Id, CollSpan, Metadata?.IsActive, Metadata?.Quantity);

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

        // بدل InvalidateCache القديم: الآن فقط صفّر الـCalc_* + صفّر caches الموارد
        public void InvalidateCache()
        {
            Calc_NetCostQ = 0;
            Calc_NetCostTotaly = 0;
            Calc_ApriceTotally = 0;
            Calc_TotalCO2 = null;
            Calc_BaseCost = null;

            if (Resources is not null)
            {
                for (int i = 0; i < Resources.Count; i++)
                    Resources[i].InvalidateCache();
            }
        }

        [JsonIgnore] public bool FilterVisible { get; set; } = true;
        [JsonIgnore] public bool CollSpan { get; set; } = true;
        [JsonIgnore] public bool HasUpdated { get; set; }
        [JsonIgnore] public bool IsDragOver { get; set; }

        public int? StatusId { get; set; }
        public int? OpportunityId { get; set; }

        //Resource (خاص بنوع Task "Resource" عندك)
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

        public bool HasOfferSelected() =>
            OfferId.HasValue && Offers != null && Offers.FirstOrDefault(x => x.Id == OfferId) != null;

        public ResourceTypesEnum ResType { get; set; }
        [JsonIgnore] public bool HasCap => ResType is (ResourceTypesEnum.MachinesAndEquipments or ResourceTypesEnum.Worker);
        [JsonIgnore] public bool HasWast => ResType is ResourceTypesEnum.Materials;
    }
}
