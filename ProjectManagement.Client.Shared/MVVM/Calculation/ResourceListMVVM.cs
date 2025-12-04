using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public class ResFromData : ResourceBase
    {
        public ResourceData Data { get; set; } = new();

        public string Note => Data.Note;
        public List<string> UpperNote => Data.UpperNote;
        public string QuantityParam => Data.QuantityParam;
        public double? Quantity => Data.Quantity;
        public string Unit => Data.Unit;
        public double ChangeFactor1 => Data.ChangeFactor1;
        public double ChangeFactor2 => Data.ChangeFactor2;
        public double CapWaste => Data.CapWaste;
        public double Cost => Data.Cost;
        public double? BaseCost => Data.BaseCost;
        public double? CO2 => Data.CO2;
    }

    public class ResourceListMVVM : ResFromData , ICalcRow
    {
        public int Version { get; set; } = 0;

        public ResourceListMVVM() { }
        public Func<Task> OfferClick { get; set; }

        public int Id { get; set; }
        public int TaskId { get; set; }
        public int? OfferId { get; set; }
        public int? OpportunityId { get; set; }

        public string Opportunity { get; set; }
        public int? AccountId { get; set; }
        public string Account { get; set; }
        public string AccountCode { get; set; }

        public string Status { get; set; }
        public string StatusColor { get; set; }
        public int? StatusId { get; set; }

        public int? ResourceSortId { get; set; }
        public int? ResourceTypeId { get; set; }
        public string ResName { get; set; }
        public string Sort { get; set; }

        public double Factor { get; set; } = 1;

        public List<ListOfferMVVM> Offers { get; set; } = [];
        private double? _netCostQ;
        private double? _netCostTotally;
        private double? _apriceTotally;
        private double? _totalCO2;

        [JsonIgnore]public double NetCostQ => _netCostQ ??= Quantity.HasValue && Quantity > 0? NetCostTotaly / Quantity.Value: 0;
        [JsonIgnore] public double NetCostTotaly => _netCostTotally ??= (BaseCost ?? 0) + (Quantity * Cost ?? 0);
        [JsonIgnore]public double ApriceTotally => _apriceTotally ??= Factor * NetCostTotaly;
        [JsonIgnore]public double? TotalCO2 => _totalCO2 ??= CO2.HasValue ? Quantity * CO2.Value : null;
        public void InvalidateCache()
        {
            _netCostTotally = null;
            _apriceTotally = null;
            _totalCO2 = null;
        }
        [JsonIgnore] public bool HasOffer => Offers?.Count > 0;
        [JsonIgnore] public bool HasCap => ResType is ResourceTypesEnum.MachinesAndEquipments or ResourceTypesEnum.Worker;
        [JsonIgnore] public bool HasWast => ResType is ResourceTypesEnum.Materials;

        public bool HasOfferSelected() =>OfferId.HasValue && Offers?.Any(x => x.Id == OfferId) == true;

        [JsonIgnore] public bool FilterVisible { get; set; } = true;
        [JsonIgnore] public bool HasUpdated { get; set; }
        [JsonIgnore] public bool IsDragOver { get; set; }


        public void RemoveOffer(int id)
        {
            var offer = Offers.FirstOrDefault(x => x.Id == id);
            if (offer != null)
            {
                if (OfferId == id) OfferId = null;
                Offers.Remove(offer);
            }
        }
    }

}
