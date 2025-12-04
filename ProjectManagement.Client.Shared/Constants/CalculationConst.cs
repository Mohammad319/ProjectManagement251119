using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using System.Collections.Generic;

namespace ProjectManagement.Client.Constant
{
    public record NetCalcProperty
    {
        public string TN { get; init; } = string.Empty; // Task DisplayName
        public string RN { get; init; } = string.Empty; // Resource DisplayName
        public ColT TT { get; init; }                  // Task Column Type
        public ColT RT { get; init; }                  // Resource Column Type
    }
    public static class CalcConst
    {
        public static readonly List<NetCalcProperty> NetCall =
          [ new() { TN = nameof(TaskData.Code), RN = string.Empty, TT = ColT.Txt, RT = ColT.Null },                        //index=0
            new() { TN = nameof(TaskData.Active), RN = nameof(ResourceListMVVM.Active), TT = ColT.Check, RT = ColT.Check },
            new() { TN = string.Empty, RN = nameof(ResourceListMVVM.AccountCode), TT = ColT.Null, RT = ColT.Txt },
            new() { TN = nameof(TaskListMVVM.Name), RN = nameof(ResourceListMVVM.Name), TT = ColT.Txt, RT = ColT.Txt },
            new() { TN = nameof(TaskListMVVM.Status), RN = nameof(ResourceListMVVM.Status), TT = ColT.Status, RT = ColT.Status },
            new() { TN = string.Empty, RN = nameof(ResourceListMVVM.ResType), TT = ColT.Null, RT = ColT.Txt },                //index=4
            new() { TN = string.Empty, RN = nameof(ResourceListMVVM.ResName), TT = ColT.Null, RT = ColT.Txt },
            new() { TN = string.Empty, RN = nameof(ResourceListMVVM.Sort), TT = ColT.Null, RT = ColT.Txt },
            new() { TN = nameof(TaskData.Quantity), RN = nameof(ResourceListMVVM.Quantity), TT = ColT.Number, RT = ColT.Number },
            new() { TN = nameof(TaskData.Unit), RN = nameof(ResourceData.Unit), TT = ColT.Txt, RT = ColT.Txt },
            new() { TN = string.Empty, RN = nameof(ResourceData.Cost),  TT =ColT.Null, RT = ColT.NumberIn },     //index=9
            new() { TN = nameof(TaskData.ChangeFactor1), RN = nameof(ResourceData.ChangeFactor1), TT = ColT.NumberIn, RT = ColT.NumberIn },
            new() { TN = nameof(TaskData.ChangeFactor2), RN = nameof(ResourceData.ChangeFactor2), TT = ColT.NumberIn, RT = ColT.NumberIn },
            new() { TN = nameof(TaskData.Cap), RN = nameof(ResourceData.CapWaste), TT = ColT.NumberIn, RT = ColT.Cap },
            new() { TN = string.Empty, RN =nameof(ResourceData.CapWaste), TT = ColT.Null, RT = ColT.Waste },
            new() { TN = nameof(TaskListMVVM.BaseCost), RN = nameof(ResourceData.BaseCost), TT = ColT.Number, RT = ColT.NumberIn },                  //index=14
            new() { TN = nameof(TaskListMVVM.Opportunity), RN = nameof(ResourceListMVVM.Opportunity), TT = ColT.Txt, RT = ColT.Txt },
            new() { TN = nameof(TaskListMVVM.NetCostQ), RN = nameof(ResourceListMVVM.NetCostQ), TT = ColT.Number, RT = ColT.Number },
            new() { TN = nameof(TaskListMVVM.NetCostTotaly), RN = nameof(ResourceListMVVM.NetCostTotaly), TT = ColT.Number, RT = ColT.Number },
            new() { TN = nameof(TaskListMVVM.PriceQTax), RN = string.Empty, TT = ColT.Func, RT = ColT.Null },
            new() { TN = nameof(TaskListMVVM.PriceQ), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },                                   //index=19
            new() { TN = nameof(TaskListMVVM.ApriceTotally), RN = nameof(ResourceListMVVM.ApriceTotally), TT = ColT.Number, RT = ColT.Number },
            new() { TN = nameof(TaskListMVVM.ApriceTotallyTax), RN = string.Empty, TT = ColT.Func, RT = ColT.Null },
            new() { TN = string.Empty, RN = nameof(ResourceListMVVM.Factor), TT = ColT.Null, RT = ColT.Number },
            new() { TN = nameof(TaskData.MinPrice), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },
            new() { TN = nameof(TaskData.CeilingPrice), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },                          //24
            new() { TN = nameof(TaskListMVVM.PriceSub), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },
            new() { TN = nameof(TaskListMVVM.PriceSubTotal), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },
            new() { TN = nameof(TaskListMVVM.Diff), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },
            new() { TN = nameof(TaskData.Responsible), RN = string.Empty, TT = ColT.TxtIn, RT = ColT.Null },

            new() { TN = string.Empty, RN = nameof(ResourceData.CO2), TT = ColT.Null, RT = ColT.NumberIn },                         //29
            new() { TN = nameof(TaskListMVVM.TotalCO2), RN = nameof(ResourceListMVVM.TotalCO2), TT = ColT.Number, RT = ColT.Number },
            new() { TN = nameof(TaskData.Note), RN = nameof(ResourceData.Note), TT = ColT.Txt, RT = ColT.Txt },
        ];
    }
    public enum ColT
    {
        Txt, Number, TxtIn, NumberIn, Check, Null, Func, Cap, Waste, Status
    }
}