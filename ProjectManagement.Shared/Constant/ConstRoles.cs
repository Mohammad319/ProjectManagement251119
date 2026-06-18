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

        [Display(Name = "actuallyQuantity")]
        ActuallyQuantity = 32,

        [Display(Name = "workedQ")]
        WorkedQ = 33,

        [Display(Name = "workedQPercent")]
        WorkedQPercent = 34,

        [Display(Name = "priceActuallyQuantity")]
        PriceActuallyQuantity = 35,

        [Display(Name = "priceWorkedQ")]
        PriceWorkedQ = 36,

        [Display(Name = "priceSubTax")]
        PriceSubTax = 37,

        [Display(Name = "priceActuallyQuantityTax")]
        PriceActuallyQuantityTax = 38,

        [Display(Name = "priceWorkedQTax")]
        PriceWorkedQTax = 39,

        [Display(Name = "note")]
        Note = 40,

        [Display(Name = "priceTotalSubTax")]
        PriceTotalSubTax = 41,

        [Display(Name = "priceProduction")]
        PriceProduction = 42
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
    { NetColumnId.PriceProduction, "priceProduction" },
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
    { NetColumnId.PriceTotalSubTax, "priceTotalSubTax" },
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
        new() { Id = NetColumnId.Account, Width = 70, Frozen = true },
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
        new() { Id = NetColumnId.PriceProduction, Width = 110, Frozen = false },
        new() { Id = NetColumnId.PriceTotaly, Width = 90, Frozen = false },
        new() { Id = NetColumnId.PriceTotallyTax, Width = 120, Frozen = false },
        new() { Id = NetColumnId.Factor, Width = 130, Frozen = false },
        new() { Id = NetColumnId.MinPrice, Width = 70, Frozen = false },
        new() { Id = NetColumnId.CeilingPrice, Width = 100, Frozen = false },
        new() { Id = NetColumnId.PriceSub, Width = 80, Frozen = false },
        new() { Id = NetColumnId.PriceTotalSub, Width = 110, Frozen = false },
        new() { Id = NetColumnId.PriceTotalSubTax, Width = 130, Frozen = false },
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

        public static List<NetColumnState> EnsureNetCalcColumns(IEnumerable<NetColumnState>? columns)
        {
            var normalized = columns?.Select(CloneNetColumn).ToList();
            return normalized is { Count: > 0 } ? normalized : NetCalc();
        }

        private static NetColumnState CloneNetColumn(NetColumnState value)
            => new()
            {
                Id = value.Id,
                Width = value.Width,
                Frozen = value.Frozen,
                StartPX = value.StartPX
            };

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
    /// <summary>
    /// Canonical role identifiers used for ASP.NET Identity role-based authorization.
    /// <para>
    /// There are two independent scopes:
    /// <list type="bullet">
    ///   <item><b>APP</b> — platform/super-admin scope, used only by the Administrator portal.</item>
    ///   <item><b>Tenant</b> — roles of users inside an organisation (the main application).</item>
    /// </list>
    /// </para>
    /// <para>
    /// There are exactly three role tiers per scope, highest → lowest: <b>Manager</b> (full control),
    /// <b>User</b> (editor), <b>Guest</b> (read-only). The string <i>values</i> below are the ASP.NET Identity
    /// role names persisted in the auth DB (seeded at startup; changing a value just re-seeds on a fresh DB).
    /// </para>
    /// <para>
    /// NOTE on the C# constant <i>names</i>: they are the legacy <c>Admin</c>/<c>Manger</c>/<c>User</c> identifiers
    /// (kept to avoid a wide, risky rename), and they map to the tiers by POSITION, not by their text:
    /// <c>Admin</c> = top tier = "Manager", <c>Manger</c> = middle tier = "User", <c>User</c> = bottom tier = "Guest".
    /// Always reach roles through these constants; never hard-code the string values.
    /// To add a 4th role later, add a constant + value here and to <c>IdentityUserSyncHelper.AppRoles/TenantRoles</c>.
    /// </para>
    /// </summary>
    public class PMRolesConst
    {
        /// <summary>Platform-level roles (Administrator portal only). Tiers: Manager / User / Guest.</summary>
        public class APP
        {
            /// <summary>Top tier — value "AppManager", UI label "Manager". Full platform control.</summary>
            public const string Admin = "AppManager";
            /// <summary>Middle tier — value "AppUser", UI label "User". Platform editor.</summary>
            public const string Manger = "AppUser";
            /// <summary>Bottom tier — value "AppGuest", UI label "Guest". Read-only.</summary>
            public const string User = "AppGuest";

            /// <summary>Editors: Manager + User ("AppManager,AppUser").</summary>
            public const string AdminManger = Admin + "," + Manger;
            /// <summary>Everyone: Manager + User + Guest ("AppManager,AppUser,AppGuest").</summary>
            public const string Users = AdminManger + "," + User;
        }

        /// <summary>
        /// Roles of users within an organisation (tenant). Three tiers, highest to lowest:
        /// <list type="number">
        ///   <item><see cref="Admin"/> — value "TenantManager", UI label "Manager". Full control of the
        ///   organisation: the ONLY tier that manages departments and users. Tenant-wide, has no department.</item>
        ///   <item><see cref="Manger"/> — value "TenantUser", UI label "User". Editor: create/edit/delete
        ///   projects, calculations, tasks, resources, tenders, etc. Belongs to a department. Cannot manage users.</item>
        ///   <item><see cref="User"/> — value "TenantGuest", UI label "Guest". Read-only: can view
        ///   reports/lists; all create/edit/delete actions are hidden (not part of <see cref="AdminManger"/>).</item>
        /// </list>
        /// </summary>
        public class Tenant
        {
            /// <summary>Top tier — value "TenantManager", UI label "Manager". Organisation admin; manages departments and users.</summary>
            public const string Admin = "TenantManager";
            /// <summary>Middle tier — value "TenantUser", UI label "User". Editor of projects/calculations; scoped to a department.</summary>
            public const string Manger = "TenantUser";
            /// <summary>Bottom tier — value "TenantGuest", UI label "Guest". Read-only viewer.</summary>
            public const string User = "TenantGuest";

            /// <summary>Editors: Manager + User ("TenantManager,TenantUser"). Used to gate every write/edit action.</summary>
            public const string AdminManger = Admin + "," + Manger;
            /// <summary>Everyone: Manager + User + Guest ("TenantManager,TenantUser,TenantGuest"). Used to gate read access.</summary>
            public const string Users = AdminManger + "," + User;
            /// <summary>Non-top tiers: User + Guest ("TenantGuest,TenantUser").</summary>
            public const string UsersNotAdmin = User + "," + Manger;
        }

        /// <summary>Managers of either scope: APP editors + Tenant editors.</summary>
        public const string MangerTenantMangerApp = APP.AdminManger + "," + Tenant.AdminManger;
        /// <summary>Every authenticated role across both scopes.</summary>
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
