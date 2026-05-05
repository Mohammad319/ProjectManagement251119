using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public static class TaskConvert
    {
        public static TaskPostDTO GetToPost(TaskListMVVM task)
        {
            ArgumentNullException.ThrowIfNull(task);
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
                IsActive = task.Metadata.IsActive,
                Code = task.Code,
                Type = task.Type,
                IsOH = task.IsOH,
                Metadata = task.Metadata,
                Colspan = task.Ui.CollSpan,
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
        private TaskMetadata? _metadata = new();
        private decimal? _quantity;
        private string _unit = string.Empty;
        [JsonIgnore] public TaskUiState Ui { get; } = new();
        [JsonIgnore] public TaskComputedState Computed { get; } = new();

        public TaskMetadata Metadata
        {
            get
            {
                _metadata ??= new TaskMetadata();
                return _metadata;
            }
            set
            {
                _metadata = CalculationItemMetadataMapper.CloneTaskMetadata(value);
                _quantity ??= _metadata.Quantity;
                if (string.IsNullOrWhiteSpace(_unit))
                    _unit = _metadata.Unit ?? string.Empty;
                SyncMetadataQuantityUnit();
            }
        }

        public string Note => Metadata.Note;
        public List<string> UpperNote => Metadata.UpperNote;
        public string QuantityParam => Metadata.QuantityParam;
        public IReadOnlyList<TaskConversionParameter> ConversionParameters => Metadata.ConversionParameters;
        public decimal? BaseQuantity => Metadata.BaseQuantity;
        public decimal? Quantity
        {
            get => _quantity ?? _metadata?.Quantity;
            set
            {
                _quantity = value;
                SyncMetadataQuantityUnit();
            }
        }

        public string Unit
        {
            get => string.IsNullOrWhiteSpace(_unit) ? _metadata?.Unit ?? string.Empty : _unit;
            set
            {
                _unit = value ?? string.Empty;
                SyncMetadataQuantityUnit();
            }
        }
        public decimal ChangeFactor1 => Metadata.ChangeFactor1;
        public decimal ChangeFactor2 => Metadata.ChangeFactor2;
        public decimal ActuallyQuantity => Metadata.ActuallyQuantity;
        public decimal WorkedQ => Metadata.WorkedQ;

        // ⚠️ تجنب قسمة على صفر
        public decimal WorkedQPercent => (Metadata.ActuallyQuantity == 0) ? 0 : (Metadata.WorkedQ / Metadata.ActuallyQuantity) * 100;

        public decimal? Cap => Metadata.Cap;

        [JsonIgnore] private bool? _activeOverride;
        public bool Active => _activeOverride ?? Metadata.IsActive;
        public void SetActiveOverride(bool? value) => _activeOverride = value;

        public string Code => Metadata.Code;
        public TaskType Type => Metadata.Type;

        public bool IsOH => Metadata.IsOH;

        public decimal? MinPrice => Metadata.MinPrice;
        public decimal? CeilingPrice => Metadata.CeilingPrice;

        [JsonIgnore] public decimal NetCostQ => this.GetComputedNetCostQ();
        [JsonIgnore] public decimal NetCostTotaly => this.GetComputedNetCostTotaly();
        [JsonIgnore] public decimal ApriceTotally => this.GetComputedApriceTotally();

        [JsonIgnore]
        public decimal PriceQ =>
            (Metadata.Quantity.HasValue && Metadata.Quantity.Value > 0)
                ? (this.GetComputedApriceTotally() / Metadata.Quantity.Value)
                : 0;

        [JsonIgnore] public double? TotalCO2 => this.GetComputedTotalCO2();
        [JsonIgnore] public decimal? BaseCost => this.GetComputedBaseCost();

        // هذه بقيت “خفيفة” (ما فيها LINQ)
        [JsonIgnore] public decimal PriceSub => Metadata.PriceSubDB ?? Math.Round(PriceQ);
        [JsonIgnore] public decimal PriceSubTotal => Metadata.Quantity.HasValue ? PriceSub * Metadata.Quantity.Value : 0;
        [JsonIgnore] public decimal Diff => PriceSubTotal - this.GetComputedApriceTotally();

        private static decimal TaxFactor(decimal taxPercent) => 1m + (taxPercent / 100m);

        public decimal PriceQTax(decimal taxPercent) => PriceQ * TaxFactor(taxPercent);
        public decimal ApriceTotallyTax(decimal taxPercent) => this.GetComputedApriceTotally() * TaxFactor(taxPercent);

        [JsonIgnore] public decimal PriceProduction => Metadata.PriceProductionDB.GetValueOrDefault();

        public decimal PriceActuallyQuantity => ActuallyQuantity * PriceProduction;
        public decimal PriceWorkedQ => WorkedQ * PriceProduction;
        public decimal PriceActuallyQuantityTax(decimal taxPercent) => PriceActuallyQuantity * TaxFactor(taxPercent);

        // taxPercent مثال: 25 يعني 25%
        public decimal PriceSubTax(decimal taxPercent) => PriceSub * TaxFactor(taxPercent);
        public decimal PriceTotalSubTax(decimal taxPercent) => PriceSubTotal * TaxFactor(taxPercent);//PriceSubTotal
        public decimal PriceWorkedQTax(decimal taxPercent) => PriceWorkedQ * TaxFactor(taxPercent);

        public bool HasVoice => Metadata.HasVoice;
        public string Responsible => Metadata.Responsible;

        public override bool Equals(object? obj)
        {
            if (obj is not TaskListMVVM other)
                return false;

            return Id == other.Id &&
                   Ui.CollSpan == other.Ui.CollSpan &&
                   (Metadata?.IsActive == other.Metadata?.IsActive) &&
                   (Metadata?.Quantity == other.Metadata?.Quantity);
        }

        public override int GetHashCode() =>
            HashCode.Combine(Id, Ui.CollSpan, Metadata?.IsActive, Metadata?.Quantity);

        public TaskListMVVM()
        {
            Metadata = new();
            Resources = [];
            Tasks = [];
        }

        public int? TaskId { get; set; }
        public string Opportunity { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;

        public List<TaskListMVVM> Tasks { get; set; }
        public List<ResourceListMVVM> Resources { get; set; }

        // إعادة ضبط الحالة المحسوبة الحالية + caches الموارد التابعة.
        public void InvalidateCache()
        {
            Computed.Reset();

            if (Resources is not null)
            {
                for (int i = 0; i < Resources.Count; i++)
                    Resources[i].InvalidateCache();
            }
        }

        public int? StatusId { get; set; }
        public int? OpportunityId { get; set; }

        private void SyncMetadataQuantityUnit()
        {
            _metadata ??= new TaskMetadata();
            _metadata.Quantity = _quantity;
            _metadata.Unit = _unit ?? string.Empty;
        }
    }
}
