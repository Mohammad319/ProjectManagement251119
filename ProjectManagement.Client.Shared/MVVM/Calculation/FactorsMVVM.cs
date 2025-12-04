using ProjectManagement.Shared.Base.Calculation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public class Factors : OHFactors
    {
        public string Sort { get; set; }
        public string ResName { get; set; }

        public double NetCostTotaly { get; set; }
        public double NetCostTotalyOH { get; set; }
        public double Factor { get; set; }

        public double Price => EarningsValue + Sum;
        public double PriceOG => NetCostTotaly * Factor;
        public double Sum => NetCostTotaly + NetCostTotalyOH;
        public double EarningsValue => Sum * (Earnings / 100);

        public List<string> KVName { get; set; } = ["NetCostTotaly", "NetCostTotalyOH", "Sum", "Price", "PriceOG"];

        public void AddResValue(bool oh, double resNetCost)
        {
            if (oh) NetCostTotalyOH += resNetCost;
            else NetCostTotaly += resNetCost;
        }

        public static Factors AddNewFactor(bool oh, ResourceListMVVM res) => new()
        {
            NetCostTotaly = !oh ? res.NetCostTotaly : 0,
            NetCostTotalyOH = oh ? res.NetCostTotaly : 0,
            SortId = res.ResourceSortId,
            ResourceType = res.ResType,
            Sort = res.Sort,
            ResName = res.ResName,
            ResId = res.ResourceTypeId,
        };

        public double KV()
        {
            return Key switch
            {
                0 => 0,
                _ when DivisionKey == 0 => NetCostTotaly / Key,
                1 => NetCostTotalyOH / Key,
                2 => Sum / Key,
                3 => Price / Key,
                4 => PriceOG / Key,
                _ => 0
            };
        }

        public double OHF(List<Factors> factors)
        {
            var filtered = factors.Where(x => x.Selected == "all" && x.NetCostTotalyOH > 0);
            double total = filtered.Sum(x => x.NetCostTotalyOH * (1 + (x.Earnings / 100)));
            double share = (NetCostTotaly * total) / factors.Sum(x => x.NetCostTotaly);
            return share;
        }

        public void FactorF(List<Factors> factors)
        {
            var match = factors.Where(x => x.Selected == $"{ResId},{SortId}");
            double relatedOH = match.Sum(x => x.NetCostTotalyOH * (1 + (x.Earnings / 100)));
            Factor = ((NetCostTotaly * (1 + (Earnings / 100))) + relatedOH + OHF(factors)) / NetCostTotaly;
        }

        public string FactorStr(List<Factors> factors)
        {
            string explanation = "(";
            double sum = 0;

            foreach (var item in factors.Where(x => x.Selected == $"{ResId},{SortId}"))
            {
                double part = item.NetCostTotalyOH * (1 + (item.Earnings / 100));
                sum += part;
                explanation += $"[{F(item.NetCostTotalyOH)} * {1 + (item.Earnings / 100)}] + ";
            }

            if (explanation.Length > 1)
                explanation = explanation[..^3] + ") + ";

            string formula = $"(({F(NetCostTotaly)} * {1 + (Earnings / 100)}) + {explanation}Oh{F(OHF(factors))}) / {F(NetCostTotaly)} = ";
            double part1 = F(NetCostTotaly * (1 + (Earnings / 100)));
            double part2 = F(sum);
            double part3 = F(OHF(factors));
            double result = F((part1 + part2 + part3) / F(NetCostTotaly));

            formula += $"{part1} + {part2} + {part3} / {F(NetCostTotaly)} = {part1 + part2 + part3} / {F(NetCostTotaly)} = {result}";
            return formula;
        }

        private double F(double x) => Math.Round(x, 4);
    }
}
