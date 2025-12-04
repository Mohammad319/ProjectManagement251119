using Domain.Entities.Folder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO.Calculation
{
    public class AdditionalFactorGroupModel
    {
        public string Name { get; set; }
        public ICollection<AdditionalFactor> AdditionalFactorList { get; set; }
    }
    public class AdditionalFactor
    {
        public string Resource { get; set; }
        public string Name { get; set; } = "Factors Name";
        public double Cost { get; set; } = 40;
        public string Account { get; set; }
        public bool Risk { get; set; }
        public bool ProfitRatio { get; set; }
        public int Quantity { get; set; } = 1;
        public string Unit { get; set; }
        public double Time { get; set; } = 1;
        public string TimeUnit { get; set; }
        public double AMP1 { get; set; } = 1;
        public double BaseCost { get; set; } = 1;
        public string Comment { get; set; }
    }
}
