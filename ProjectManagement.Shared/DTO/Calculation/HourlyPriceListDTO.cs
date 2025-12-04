using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class HourlyPriceListGroupDTO
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }

        [JsonIgnore]
        public bool SubItemsVisible { get; set; } = true;
        public List<HourlyPriceListItemDTO> Items { get; set; } = [];
    }
    public class HourlyPriceListItemDTO
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double Quantity { get; set; }
        public double CostMarketPrices { get; set; }
        public double CostSubmittedPrices { get; set; }

        public string Comment { get; set; }

        [JsonIgnore]
        public double TotalMarketPrices => CostMarketPrices * Quantity;
        [JsonIgnore]
        public double TotalSubmittedPrices => CostSubmittedPrices * Quantity;
        [JsonIgnore]
        public double Difference => TotalSubmittedPrices - TotalMarketPrices;


        
    }
}
