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
        public decimal CostMarketPrices { get; set; }
        public decimal CostSubmittedPrices { get; set; }

        public string Comment { get; set; }

        [JsonIgnore]
        public decimal TotalMarketPrices => CostMarketPrices * (decimal)Quantity;
        [JsonIgnore]
        public decimal TotalSubmittedPrices => CostSubmittedPrices * (decimal)Quantity;
        [JsonIgnore]
        public decimal Difference => TotalSubmittedPrices - TotalMarketPrices;


        
    }
}
