using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectManagement.Client.Constant
{
    public class TemplateConst
    {
        public static int Col0 { get; set; } = 35;
        public static List<string> BorderStyles { get; } = ["hidden", "none", "dotted", "dashed", "solid", "double", "groove", "ridge", "inset", "outset", "mix"];

        // Swedish display names for the CSS border styles above (admin should never see raw CSS keywords).
        public static IReadOnlyDictionary<string, string> BorderStyleLabelsSv { get; } = new Dictionary<string, string>
        {
            ["hidden"] = "Dold",
            ["none"] = "Ingen",
            ["dotted"] = "Punktad",
            ["dashed"] = "Streckad",
            ["solid"] = "Heldragen",
            ["double"] = "Dubbel",
            ["groove"] = "Skåra",
            ["ridge"] = "Upphöjd",
            ["inset"] = "Infälld",
            ["outset"] = "Utfälld",
            ["mix"] = "Blandad"
        };

        public static string BorderStyleLabel(string style)
            => BorderStyleLabelsSv.TryGetValue(style, out var label) ? label : style;
        public static string DragOverStyle { get; set; } = "background:rgb(100 150 120 / 70%);border-style:solid none none none;border-color:blue;";

        public static Collection<string> SSCTitles { get; } =
        [ "name","sort", "netCal", "netCalOH", "sum", "earnings", "ev","price","priceOG",""
        ,"keyValue", "kv", "unit", "OH", "factor"];


        public static List<string> NetTitle { get; set; } = ["code","active","account","name","status",
            "resourceTypeSystem","resourceType","resourceSort","quantity","unit","cost","changeFactor1","changeFactor2",
            "waste","cap","baseCost","opportunity","netCostQ","totalNetCost","priceQTax","priceQ","priceProduction",
            "priceTotaly","priceTotallyTax","factor","minPrice","ceilingPrice","priceSub","priceTotalSub","diff","responsible","Co2","totalCo2","note"];

    }
}
