using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.Model.Project.Calculation
{
    public class AdditionalFactorGroupModel
    {
        public string Name { get; set; } = "Additional Factor Group";
        [JsonIgnore] public double Total => AdditionalFactorList.Sum(x => x.Total);
        [JsonIgnore] public double RiskTotal => AdditionalFactorList.Where(x => x.Risk).Sum(x => x.Total);
        [JsonIgnore] public double BaseCost => AdditionalFactorList.Sum(x => x.BaseCost);
        [JsonIgnore] public double RiskBaseCost => AdditionalFactorList.Where(x => x.Risk).Sum(x => x.BaseCost);
        [JsonIgnore] public bool Collspan { get; set; }
        public List<AdditionalFactor> AdditionalFactorList { get; set; } = [];
    }

    public class AdditionalFactor
    {
        public string Resource { get; set; }

        public bool ProfitRatio { get; set; }
        public string Name { get; set; } = "Factors Name";
        public bool Risk { get; set; }
        public double Cost { get; set; } = 40;
        public string Account { get; set; }
        public int Quantity { get; set; } = 1;
        public string Unit { get; set; }
        public double Time { get; set; } = 1;
        public string TimeUnit { get; set; }
        public double AMP1 { get; set; } = 1;
        public double BaseCost { get; set; } = 1;
        public string Comment { get; set; }
        [JsonIgnore] public double Total => BaseCost + (Quantity * Cost * Time * AMP1);
    }
}
