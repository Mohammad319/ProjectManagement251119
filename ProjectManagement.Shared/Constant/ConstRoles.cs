using ProjectManagement.Shared.DTO.Calculation.Template;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Calculation.Template
{
    public enum NetColumnId
    {
        [Display(Name = "code")]
        Code = 0,

        [Display(Name = "active")]
        Active = 1,

        [Display(Name = "account")]
        Account = 2,

        [Display(Name = "name")]
        Name = 3,

        [Display(Name = "status")]
        Status = 4,

        [Display(Name = "resourceTypeSystem")]
        ResourceTypeSystem = 5,

        [Display(Name = "resourceType")]
        ResourceType = 6,

        [Display(Name = "resourceSort")]
        ResourceSort = 7,

        [Display(Name = "quantity")]
        Quantity = 8,

        [Display(Name = "unit")]
        Unit = 9,

        [Display(Name = "cost")]
        Cost = 10,

        [Display(Name = "changeFactor1")]
        ChangeFactor1 = 11,

        [Display(Name = "changeFactor2")]
        ChangeFactor2 = 12,

        [Display(Name = "waste")]
        Waste = 13,

        [Display(Name = "cap")]
        Cap = 14,

        [Display(Name = "baseCost")]
        BaseCost = 15,

        [Display(Name = "opportunity")]
        Opportunity = 16,

        [Display(Name = "netCostQ")]
        NetCostQ = 17,

        [Display(Name = "totalNetCost")]
        TotalNetCost = 18,

        [Display(Name = "priceQTax")]
        PriceQTax = 19,

        [Display(Name = "priceQ")]
        PriceQ = 20,

        [Display(Name = "priceTotaly")]
        PriceTotaly = 21,

        [Display(Name = "priceTotallyTax")]
        PriceTotallyTax = 22,

        [Display(Name = "factor")]
        Factor = 23,

        [Display(Name = "minPrice")]
        MinPrice = 24,

        [Display(Name = "ceilingPrice")]
        CeilingPrice = 25,

        [Display(Name = "priceSub")]
        PriceSub = 26,

        [Display(Name = "priceTotalSub")]
        PriceTotalSub = 27,

        [Display(Name = "diff")]
        Diff = 28,

        [Display(Name = "responsible")]
        Responsible = 29,

        [Display(Name = "Co2")]
        Co2 = 30,

        [Display(Name = "totalCo2")]
        TotalCo2 = 31,

        [Display(Name = "Actually quantity")]
        ActuallyQuantity = 32,

        [Display(Name = "Worked Q")]
        WorkedQ = 33,

        [Display(Name = "Worked Q (%)")]
        WorkedQPercent = 34,

        [Display(Name = "Price Actually quantity")]
        PriceActuallyQuantity = 35,

        [Display(Name = "Price Worked Q")]
        PriceWorkedQ = 36,

        [Display(Name = "Price sub Tax")]
        PriceSubTax = 37,

        [Display(Name = "Price Actually quantity Tax")]
        PriceActuallyQuantityTax = 38,

        [Display(Name = "Price Worked Q Tax")]
        PriceWorkedQTax = 39,

        [Display(Name = "note")]
        Note = 40
    }

    public enum SummarySheetColumnId
        {
            [Display(Name = "name")]
            Name = 0,

            [Display(Name = "sort")]
            Sort = 1,

            [Display(Name = "netCal")]
            NetCal = 2,

            [Display(Name = "netCalOH")]
            NetCalOH = 3,

            [Display(Name = "sum")]
            Sum = 4,

            [Display(Name = "earnings")]
            Earnings = 5,

            [Display(Name = "ev")]
            Ev = 6,

            [Display(Name = "price")]
            Price = 7,

            [Display(Name = "priceOG")]
            PriceOG = 8,

            // كان الاسم فارغ سابقاً
            [Display(Name = "")]
            Empty = 9,

            [Display(Name = "keyValue")]
            KeyValue = 10,

            [Display(Name = "kv")]
            Kv = 11,

            [Display(Name = "unit")]
            Unit = 12,

            [Display(Name = "OH")]
            OH = 13,

            [Display(Name = "factor")]
            Factor = 14
        }

    public class TEmpBase {
        public static readonly Dictionary<NetColumnId, string> CalcNetLoc = new()
{
    { NetColumnId.Code, "code" },
    { NetColumnId.Active, "active" },
    { NetColumnId.Account, "account" },
    { NetColumnId.Name, "name" },
    { NetColumnId.Status, "status" },
    { NetColumnId.ResourceTypeSystem, "resourceTypeSystem" },
    { NetColumnId.ResourceType, "resourceType" },
    { NetColumnId.ResourceSort, "resourceSort" },
    { NetColumnId.Quantity, "quantity" },
    { NetColumnId.Unit, "unit" },
    { NetColumnId.Cost, "cost" },
    { NetColumnId.ChangeFactor1, "changeFactor1" },
    { NetColumnId.ChangeFactor2, "changeFactor2" },
    { NetColumnId.Waste, "waste" },
    { NetColumnId.Cap, "cap" },
    { NetColumnId.BaseCost, "baseCost" },
    { NetColumnId.Opportunity, "opportunity" },
    { NetColumnId.NetCostQ, "netCostQ" },
    { NetColumnId.TotalNetCost, "totalNetCost" },
    { NetColumnId.PriceQTax, "priceQTax" },
    { NetColumnId.PriceQ, "priceQ" },
    { NetColumnId.PriceTotaly, "priceTotaly" },
    { NetColumnId.PriceTotallyTax, "priceTotallyTax" },
    { NetColumnId.Factor, "factor" },
    { NetColumnId.MinPrice, "minPrice" },
    { NetColumnId.CeilingPrice, "ceilingPrice" },
    { NetColumnId.PriceSub, "priceSub" },
    { NetColumnId.PriceTotalSub, "priceTotalSub" },
    { NetColumnId.Diff, "diff" },
    { NetColumnId.Responsible, "responsible" },
    { NetColumnId.Co2, "Co2" },
    { NetColumnId.TotalCo2, "totalCo2" },
    { NetColumnId.ActuallyQuantity, "Actually quantity" },
    { NetColumnId.WorkedQ, "Worked Q" },
    { NetColumnId.WorkedQPercent, "Worked Q (%)" },
    { NetColumnId.PriceActuallyQuantity, "Price Actually quantity" },
    { NetColumnId.PriceWorkedQ, "Price Worked Q" },
    { NetColumnId.PriceSubTax, "Price sub Tax" },
    { NetColumnId.PriceActuallyQuantityTax, "Price Actually quantity Tax" },
    { NetColumnId.PriceWorkedQTax, "Price Worked Q Tax" },
    { NetColumnId.Note, "note" },
};
        public static readonly Dictionary<SummarySheetColumnId, string> SummaryLoc = new()
{
    { SummarySheetColumnId.Name, "name" },
    { SummarySheetColumnId.Sort, "sort" },
    { SummarySheetColumnId.NetCal, "netCal" },
    { SummarySheetColumnId.NetCalOH, "netCalOH" },
    { SummarySheetColumnId.Sum, "sum" },
    { SummarySheetColumnId.Earnings, "earnings" },
    { SummarySheetColumnId.Ev, "ev" },
    { SummarySheetColumnId.Price, "price" },
    { SummarySheetColumnId.PriceOG, "priceOG" },
    { SummarySheetColumnId.Empty, "" },
    { SummarySheetColumnId.KeyValue, "keyValue" },
    { SummarySheetColumnId.Kv, "kv" },
    { SummarySheetColumnId.Unit, "unit" },
    { SummarySheetColumnId.OH, "OH" },
    { SummarySheetColumnId.Factor, "factor" },
};
    }
}

namespace ProjectManagement.Shared.DTO.Calculation.Template
{
    public static class EnumDisplayExtensions
    {
        public static string DisplayKey(this Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttribute<DisplayAttribute>();
            return attr?.Name ?? value.ToString();
        }
    }
    public static class NetColumnLoc
    {
        public static readonly IReadOnlyDictionary<NetColumnId, string> Key =
            Enum.GetValues<NetColumnId>().ToDictionary(x => x, x => x.DisplayKey());

        public static readonly IReadOnlyDictionary<SummarySheetColumnId, string> SummaryKey =
            Enum.GetValues<SummarySheetColumnId>().ToDictionary(x => x, x => x.DisplayKey());
    }
}


namespace ProjectManagement.Shared.Constants
{
    public sealed class NetColumnState2
    {
        public NetColumnId Id { get; set; }   // يُحفظ رقمياً تلقائياً
        public int Width { get; set; }
        public bool Frozen { get; set; }


    }
    public sealed class NetColumnState
    {
        public NetColumnId Id { get; set; }
        public int Width { get; set; }
        public bool Frozen { get; set; }
        [JsonIgnore]public int StartPX { get; set; }
    }
    public sealed class SummarySheetColumnState
    {
        public SummarySheetColumnId Id { get; set; }
        public int Width { get; set; }
        public bool Frozen { get; set; }
    }
    public static class TemplateDefaults
    {
        public static List<NetColumnState> NetCalc()
        {
            return
            [
                new() { Id = NetColumnId.Code, Width = 50, Frozen = false },
        new() { Id = NetColumnId.Active, Width = 50, Frozen = false },
        new() { Id = NetColumnId.Account, Width = 70, Frozen = false },
        new() { Id = NetColumnId.Name,Width = 120, Frozen = true },
        new() { Id = NetColumnId.Status, Width = 100, Frozen = false },
        new() { Id = NetColumnId.ResourceTypeSystem, Width = 180, Frozen = false },
        new() { Id = NetColumnId.ResourceType, Width = 120, Frozen = false },
        new() { Id = NetColumnId.ResourceSort, Width = 110, Frozen = false },
        new() { Id = NetColumnId.Quantity, Width = 70, Frozen = false },
        new() { Id = NetColumnId.Unit, Width = 45, Frozen = false },
        new() { Id = NetColumnId.Cost, Width = 60, Frozen = false },
        new() { Id = NetColumnId.ChangeFactor1, Width = 125, Frozen = false },
        new() { Id = NetColumnId.ChangeFactor2, Width = 125, Frozen = false },
        new() { Id = NetColumnId.Waste, Width = 55, Frozen = false },
        new() { Id = NetColumnId.Cap, Width = 45, Frozen = false },
        new() { Id = NetColumnId.BaseCost, Width = 85, Frozen = false },
        new() { Id = NetColumnId.Opportunity, Width = 100, Frozen = false },
        new() { Id = NetColumnId.NetCostQ, Width = 80, Frozen = false },
        new() { Id = NetColumnId.TotalNetCost, Width = 100, Frozen = false },
        new() { Id = NetColumnId.PriceQTax,  Width = 90, Frozen = false },
        new() { Id = NetColumnId.PriceQ, Width = 60, Frozen = false },
        new() { Id = NetColumnId.PriceTotaly, Width = 90, Frozen = false },
        new() { Id = NetColumnId.PriceTotallyTax, Width = 120, Frozen = false },
        new() { Id = NetColumnId.Factor, Width = 130, Frozen = false },
        new() { Id = NetColumnId.MinPrice, Width = 70, Frozen = false },
        new() { Id = NetColumnId.CeilingPrice, Width = 100, Frozen = false },
        new() { Id = NetColumnId.PriceSub, Width = 80, Frozen = false },
        new() { Id = NetColumnId.PriceTotalSub, Width = 110, Frozen = false },
        new() { Id = NetColumnId.Diff, Width = 85, Frozen = false },
        new() { Id = NetColumnId.Responsible, Width = 100, Frozen = false },
        new() { Id = NetColumnId.Co2, Width = 60, Frozen = false },
        new() { Id = NetColumnId.TotalCo2, Width = 60, Frozen = false },
        new() { Id = NetColumnId.ActuallyQuantity, Width = 90, Frozen = false },
        new() { Id = NetColumnId.WorkedQ, Width = 60, Frozen = false },
        new() { Id = NetColumnId.WorkedQPercent, Width = 80, Frozen = false },
        new() { Id = NetColumnId.PriceActuallyQuantity, Width = 60, Frozen = false },
        new() { Id = NetColumnId.PriceWorkedQ, Width = 60, Frozen = false },
        new() { Id = NetColumnId.PriceSubTax, Width = 60, Frozen = false },
        new() { Id = NetColumnId.PriceActuallyQuantityTax, Width = 60, Frozen = false },
        new() { Id = NetColumnId.PriceWorkedQTax, Width = 60, Frozen = false },
        new() { Id = NetColumnId.Note, Width = 80, Frozen = false },
    ];
        }

        public static List<SummarySheetColumnState> SummarySheet()
        {
            return
            [
                new() { Id = SummarySheetColumnId.Name, Width = 100, Frozen = true },
        new() { Id = SummarySheetColumnId.Sort,  Width = 80, Frozen = false },
        new() { Id = SummarySheetColumnId.NetCal, Width = 60, Frozen = false },
        new() { Id = SummarySheetColumnId.NetCalOH, Width = 60, Frozen = false },
        new() { Id = SummarySheetColumnId.Sum, Width = 60, Frozen = false },
        new() { Id = SummarySheetColumnId.Earnings, Width = 100, Frozen = false },
        new() { Id = SummarySheetColumnId.Ev, Width = 60, Frozen = false },
        new() { Id = SummarySheetColumnId.Price, Width = 60, Frozen = false },
        new() { Id = SummarySheetColumnId.PriceOG, Width = 90, Frozen = false },
        new() { Id = SummarySheetColumnId.Empty, Width = 80, Frozen = false },
        new() { Id = SummarySheetColumnId.KeyValue, Width = 60, Frozen = false },
        new() { Id = SummarySheetColumnId.Kv, Width = 60, Frozen = false },
        new() { Id = SummarySheetColumnId.Unit, Width = 120, Frozen = false },
        new() { Id = SummarySheetColumnId.OH, Width = 80, Frozen = false },
        new() { Id = SummarySheetColumnId.Factor, Width = 80, Frozen = false },
    ];
        }

    }

}

namespace ProjectManagement.Shared.Constant
{
    public class PMRolesConst
    {

        public class APP
        {
            public const string Admin = "AA";
            public const string SuperManger = "SA";
            public const string Manger = "MA";
            public const string User = "UA";

            public const string AdminSuperManger = Admin + "," + SuperManger;
            public const string AdminManger = AdminSuperManger + "," + Manger;
            public const string Users = AdminManger + "," + User;
        }

        public class Tenant
        {
            public const string Admin = "AT";
            public const string SuperManger = "AD";
            public const string Manger = "MT";
            public const string User = "UT";

            public const string AdminSuperManger = Admin + "," + SuperManger;
            public const string AdminManger = AdminSuperManger + "," + Manger;
            public const string Users = AdminManger + "," + User;
            public const string Super_Manger = Manger + "," + SuperManger;
            public const string UsersNotAdmin = User + "," + Super_Manger;
        }

        public const string MangerTenantMangerApp = APP.AdminManger + "," + Tenant.AdminManger;
        public const string All = Tenant.Users + "," + APP.Users;
    }

    public class PMClaimsConst
    {
        public const string Tenant = "tenant";
        public const string UserId = "UserId";
        public const string DepartmentId = "DepartmentId";
        public const string FullName = "full_name";
    }
}
