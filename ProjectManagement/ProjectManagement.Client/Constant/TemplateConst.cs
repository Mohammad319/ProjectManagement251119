using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectManagement.Client.Constant
{
    public class TemplateConst
    {
        public static int Col0 { get; set; } = 35;
        public static List<string> BorderStyles { get; } = ["hidden", "none", "dotted", "dashed", "solid", "double", "groove", "ridge", "inset", "outset", "mix"];
        public static string DragOverStyle { get; set; } = "background:rgb(100 150 120 / 70%);border-style:solid none none none;border-color:blue;";

        public static Collection<string> SSCTitles { get; } =
        [ "name","sort", "netCal", "netCalOH", "sum", "earnings", "ev","price","priceOG",""
        ,"keyValue", "kv", "unit", "OH", "factor"]; //, "priceWithProjectOH", "keyValue", "kv", "unit"


        public static List<string> NetTitle { get; set; } = ["code","active","account","name","status",
            "resourceTypeSystem","resourceType","resourceSort","quantity","unit","cost","changeFactor1","changeFactor2",
            "waste","cap","baseCost","opportunity","netCostQ","totalNetCost","priceQTax","priceQ",
            "priceTotaly","priceTotallyTax","factor","minPrice","ceilingPrice","priceSub","priceTotalSub","diff","responsible","Co2","totalCo2","note"];

    }
}
