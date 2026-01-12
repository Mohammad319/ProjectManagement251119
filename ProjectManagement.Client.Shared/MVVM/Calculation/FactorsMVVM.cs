using ProjectManagement.Shared.Base.Calculation;
using System;
using System.Collections.Generic;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public class Factors : OHFactors
    {
        // تجنب nulls (أفضل للـUI وأقل فروع)
        public string Sort { get; set; } = string.Empty;
        public string ResName { get; set; } = string.Empty;

        public double NetCostTotaly { get; set; }
        public double NetCostTotalyOH { get; set; }
        public double Factor { get; set; }

        public double Sum => NetCostTotaly + NetCostTotalyOH;
        public double EarningsValue => Sum * (Earnings / 100);
        public double Price => EarningsValue + Sum;
        public double PriceOG => NetCostTotaly * Factor;

        // بدل List لكل instance (Alloc غير لازم)
        public static readonly string[] KVName = { "NetCostTotaly", "NetCostTotalyOH", "Sum", "Price", "PriceOG" };

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
            Sort = res.Sort ?? string.Empty,
            ResName = res.ResName ?? string.Empty,
            ResId = res.ResourceTypeId,
        };

        public double KV()
        {
            // نفس منطقك
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

        // =========================================================
        // OHF / FactorF بدون LINQ (لكن ما يزال O(n) لكل استدعاء)
        // ملاحظة: المسار السريع الحقيقي موجود في CalcultationExtensions
        // (ApplyFactorF_Optimized) لتجنب O(n^2).
        // =========================================================

        public double OHF(List<Factors> factors)
        {
            if (factors is null || factors.Count == 0) return 0;

            double totalOHAll = 0;
            double sumNetCostAll = 0;

            for (int i = 0; i < factors.Count; i++)
            {
                var x = factors[i];
                sumNetCostAll += x.NetCostTotaly;

                if (x.Selected == "all" && x.NetCostTotalyOH > 0)
                    totalOHAll += x.NetCostTotalyOH * (1 + (x.Earnings / 100));
            }

            if (sumNetCostAll == 0) return 0;

            // حصتك من OH حسب NetCostTotaly
            return (NetCostTotaly * totalOHAll) / sumNetCostAll;
        }

        public void FactorF(List<Factors> factors)
        {
            if (NetCostTotaly <= 0) { Factor = 0; return; }
            if (factors is null || factors.Count == 0) { Factor = 1; return; }

            // relatedOH: Selected == $"{ResId},{SortId}"
            string matchKey = $"{ResId},{SortId}";
            double relatedOH = 0;

            for (int i = 0; i < factors.Count; i++)
            {
                var x = factors[i];
                if (x.Selected == matchKey && x.NetCostTotalyOH > 0)
                    relatedOH += x.NetCostTotalyOH * (1 + (x.Earnings / 100));
            }

            double ohShare = OHF(factors);

            Factor = ((NetCostTotaly * (1 + (Earnings / 100))) + relatedOH + ohShare) / NetCostTotaly;
        }

        public string FactorStr(List<Factors> factors)
        {
            if (NetCostTotaly <= 0) return "NetCostTotaly is 0";

            string matchKey = $"{ResId},{SortId}";

            string explanation = "(";
            double relatedSum = 0;

            if (factors != null)
            {
                for (int i = 0; i < factors.Count; i++)
                {
                    var item = factors[i];
                    if (item.Selected != matchKey) continue;

                    double part = item.NetCostTotalyOH * (1 + (item.Earnings / 100));
                    relatedSum += part;
                    explanation += $"[{F(item.NetCostTotalyOH)} * {1 + (item.Earnings / 100)}] + ";
                }
            }

            if (explanation.Length > 1)
                explanation = explanation[..^3] + ") + ";
            else
                explanation = string.Empty;

            double oh = OHF(factors ?? new List<Factors>(0));

            string formula = $"(({F(NetCostTotaly)} * {1 + (Earnings / 100)}) + {explanation}Oh{F(oh)}) / {F(NetCostTotaly)} = ";

            double part1 = F(NetCostTotaly * (1 + (Earnings / 100)));
            double part2 = F(relatedSum);
            double part3 = F(oh);

            double result = F((part1 + part2 + part3) / F(NetCostTotaly));

            formula += $"{part1} + {part2} + {part3} / {F(NetCostTotaly)} = {part1 + part2 + part3} / {F(NetCostTotaly)} = {result}";
            return formula;
        }

        private double F(double x) => Math.Round(x, 4);
    }
}
