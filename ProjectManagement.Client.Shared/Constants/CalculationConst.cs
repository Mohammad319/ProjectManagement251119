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
          [ new() { TN = nameof(TaskMetadata.Code), RN = string.Empty, TT = ColT.Txt, RT = ColT.Null },                        //index=0
            new() { TN = nameof(TaskMetadata.IsActive), RN = nameof(ResourceListMVVM.Active), TT = ColT.Check, RT = ColT.Check },
            new() { TN = string.Empty, RN = nameof(ResourceListMVVM.AccountCode), TT = ColT.Null, RT = ColT.Txt },
            new() { TN = nameof(TaskListMVVM.Name), RN = nameof(ResourceListMVVM.Name), TT = ColT.Txt, RT = ColT.Txt },
            new() { TN = nameof(TaskListMVVM.Status), RN = nameof(ResourceListMVVM.Status), TT = ColT.Status, RT = ColT.Status },
            new() { TN = string.Empty, RN = nameof(ResourceListMVVM.ResType), TT = ColT.Null, RT = ColT.Txt },                //index=4
            new() { TN = string.Empty, RN = nameof(ResourceListMVVM.ResName), TT = ColT.Null, RT = ColT.Txt },
            new() { TN = string.Empty, RN = nameof(ResourceListMVVM.Sort), TT = ColT.Null, RT = ColT.Txt },
            new() { TN = nameof(TaskMetadata.Quantity), RN = nameof(ResourceListMVVM.Quantity), TT = ColT.Number, RT = ColT.Number },
            new() { TN = nameof(TaskMetadata.Unit), RN = nameof(ResourceMetadata.Unit), TT = ColT.Txt, RT = ColT.Txt },
            new() { TN = string.Empty, RN = nameof(ResourceMetadata.Cost),  TT =ColT.Null, RT = ColT.NumberIn },     //index=9
            new() { TN = nameof(TaskMetadata.ChangeFactor1), RN = nameof(ResourceMetadata.ChangeFactor1), TT = ColT.NumberIn, RT = ColT.NumberIn },
            new() { TN = nameof(TaskMetadata.ChangeFactor2), RN = nameof(ResourceMetadata.ChangeFactor2), TT = ColT.NumberIn, RT = ColT.NumberIn },
            new() { TN = nameof(TaskMetadata.Cap), RN = nameof(ResourceMetadata.CapWaste), TT = ColT.NumberIn, RT = ColT.Cap },
            new() { TN = string.Empty, RN =nameof(ResourceMetadata.CapWaste), TT = ColT.Null, RT = ColT.Waste },
            new() { TN = nameof(TaskListMVVM.BaseCost), RN = nameof(ResourceMetadata.BaseCost), TT = ColT.Number, RT = ColT.NumberIn },                  //index=14
            new() { TN = nameof(TaskListMVVM.Opportunity), RN = nameof(ResourceListMVVM.Opportunity), TT = ColT.Txt, RT = ColT.Txt },
            new() { TN = nameof(TaskListMVVM.NetCostQ), RN = nameof(ResourceListMVVM.NetCostQ), TT = ColT.Number, RT = ColT.Number },
            new() { TN = nameof(TaskListMVVM.NetCostTotaly), RN = nameof(ResourceListMVVM.NetCostTotaly), TT = ColT.Number, RT = ColT.Number },
            new() { TN = nameof(TaskListMVVM.PriceQTax), RN = string.Empty, TT = ColT.Func, RT = ColT.Null },
            new() { TN = nameof(TaskListMVVM.PriceQ), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },                                   //index=19
            new() { TN = nameof(TaskListMVVM.ApriceTotally), RN = nameof(ResourceListMVVM.ApriceTotally), TT = ColT.Number, RT = ColT.Number },
            new() { TN = nameof(TaskListMVVM.ApriceTotallyTax), RN = string.Empty, TT = ColT.Func, RT = ColT.Null },
            new() { TN = string.Empty, RN = nameof(ResourceListMVVM.Factor), TT = ColT.Null, RT = ColT.Number },
            new() { TN = nameof(TaskMetadata.MinPrice), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },
            new() { TN = nameof(TaskMetadata.CeilingPrice), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },                          //24
            new() { TN = nameof(TaskListMVVM.PriceSub), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },
            new() { TN = nameof(TaskListMVVM.PriceSubTotal), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },
            new() { TN = nameof(TaskListMVVM.Diff), RN = string.Empty, TT = ColT.Number, RT = ColT.Null },
            new() { TN = nameof(TaskMetadata.Responsible), RN = string.Empty, TT = ColT.TxtIn, RT = ColT.Null },

            new() { TN = string.Empty, RN = nameof(ResourceMetadata.CO2), TT = ColT.Null, RT = ColT.NumberIn },                         //29
            new() { TN = nameof(TaskListMVVM.TotalCO2), RN = nameof(ResourceListMVVM.TotalCO2), TT = ColT.Number, RT = ColT.Number },
            new() { TN = nameof(TaskMetadata.Note), RN = nameof(ResourceMetadata.Note), TT = ColT.Txt, RT = ColT.Txt },
        ];
    }
    public enum ColT
    {
        Txt, Number, TxtIn, NumberIn, Check, Null, Func, Cap, Waste, Status
    }
}