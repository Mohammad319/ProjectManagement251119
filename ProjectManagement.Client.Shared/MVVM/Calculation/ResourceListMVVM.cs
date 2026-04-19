using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Enums;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public class ResFromData : ResourceBase
    {
        private ResourceMetadata? _data = new();

        public ResourceMetadata Data
        {
            get
            {
                _data ??= new ResourceMetadata();
                return _data;
            }
            set { _data = CalculationItemMetadataMapper.CloneResourceMetadata(value); }
        }

        public string QuantityParam => Data.QuantityParam;
        public decimal? Quantity => Data.Quantity;
        public string Unit => Data.Unit;
        public decimal ChangeFactor1 => Data.ChangeFactor1;
        public decimal ChangeFactor2 => Data.ChangeFactor2;

        public decimal CapWaste => Data.CapWaste;
        public decimal Cost => Data.Cost;
        public decimal? BaseCost => Data.BaseCost;
        public double? CO2 => Data.CO2;

        [JsonIgnore] public decimal PriceSubTotal => Data.PriceSub.HasValue && Quantity.HasValue ? Data.PriceSub.Value * Quantity.Value : 0;
    }

    public class ResourceListMVVM : ResFromData
    {
        [JsonIgnore] public ResourceUiState Ui { get; } = new();
        [JsonIgnore] public ResourceComputedState Computed { get; } = new();

        public int Id { get; set; }
        public int TaskId { get; set; }
        public int? OfferId { get; set; }
        public int? OpportunityId { get; set; }

        public string? Opportunity { get; set; }
        public int? AccountId { get; set; }
        public string? Account { get; set; }
        public string? AccountCode { get; set; }

        public string? Status { get; set; }
        public string? StatusColor { get; set; }
        public int? StatusId { get; set; }

        public int? ResourceSortId { get; set; }
        public int? ResourceTypeId { get; set; }
        public string? ResName { get; set; }
        public string? Sort { get; set; }
        [JsonIgnore] public decimal DisplayCapWaste => Computed.EffectiveCapWaste ?? Data.CapWaste;

        [JsonIgnore]
        public decimal Factor
        {
            get => this.GetComputedFactor();
            set => this.SetComputedFactor(value);
        }

        public List<ListOfferMVVM> Offers { get; set; } = [];
        [JsonIgnore] public bool HasSelectedOffer { get; private set; }

        [JsonIgnore] public decimal NetCostQ => this.GetComputedNetCostQ();

        public void InvalidateCache()
        {
            Computed.Reset();
        }

        [JsonIgnore] public bool HasOffer => Offers.Count > 0;
        [JsonIgnore] public bool HasCap => ResType is ResourceTypesEnum.MachinesAndEquipments or ResourceTypesEnum.Worker;
        [JsonIgnore] public bool HasWast => ResType is ResourceTypesEnum.Materials;

        public bool HasOfferSelected() => HasSelectedOffer;

        public void SyncOfferSelection()
        {
            if (!OfferId.HasValue || Offers.Count == 0)
            {
                HasSelectedOffer = false;
                return;
            }

            for (int i = 0; i < Offers.Count; i++)
            {
                if (Offers[i].Id == OfferId.Value)
                {
                    HasSelectedOffer = true;
                    return;
                }
            }

            HasSelectedOffer = false;
        }

        public void RemoveOffer(int id)
        {
            var offer = Offers.FirstOrDefault(x => x.Id == id);
            if (offer != null)
            {
                if (OfferId == id) OfferId = null;
                Offers.Remove(offer);
                SyncOfferSelection();
            }
        }
    }
}
