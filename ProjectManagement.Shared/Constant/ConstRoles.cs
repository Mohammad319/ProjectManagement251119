using System.Collections.Generic;

namespace ProjectManagement.Shared.Constants
{
    public enum NetColumn
    {
        Code = 0,
        Active = 1,
        Account = 2,
        Name = 3,
        Status = 4,
        ResourceTypeSystem = 5,
        ResourceType = 6,
        ResourceSort = 7,
        Quantity = 8,
        Unit = 9,
        Cost = 10,
        ChangeFactor1 = 11,
        ChangeFactor2 = 12,
        Waste = 13,
        Cap = 14,
        BaseCost = 15,
        Opportunity = 16,
        NetCostQ = 17,
        TotalNetCost = 18,
        PriceQTax = 19,
        PriceQ = 20,
        PriceTotaly = 21,
        PriceTotallyTax = 22,
        Factor = 23,
        MinPrice = 24,
        CeilingPrice = 25,
        PriceSub = 26,
        PriceTotalSub = 27,
        Diff = 28,
        Responsible = 29,
        Co2 = 30,
        TotalCo2 = 31,

        ActuallyQuantity = 32,
        WorkedQ = 33,
        WorkedQPercent = 34,

        PriceActuallyQuantity = 35,
        PriceWorkedQ = 36,
        PriceSubTax = 37,
        PriceTotalSubTax = 38,
        PriceActuallyQuantityTax = 39,
        PriceWorkedQTax = 40,

        Note = 41
    }

    public sealed class NetColumnState
    {
        public int Id { get; set; }
        public int Width { get; set; }
        public int Order { get; set; }
        public bool Frozen { get; set; }
        public string Name { get; set; }
        public bool Visible { get; set; } = true;
        public int StartPX { get; set; }

        public NetColumn? Column { get; set; }  // المفتاح الجديد الآمن
        public bool IsMigrated { get; set; }    // لمعرفة هل انتقلنا للـ Column أم لا
    }

    public static class TemplateDefaults
    {
        public static List<NetColumnState> NetCalc()
        {
            return
            [
                new() { Id = 0,  Name = "code",          Order = 0,  Width = 50,  Frozen = false,  Visible = true  },
            new() { Id = 1,  Name = "active",            Order = 1,  Width = 50,  Frozen = false, Visible = true  },
            new() { Id = 2,  Name = "account",           Order = 2,  Width = 70,  Frozen = false,  Visible = true  },
            new() { Id = 3,  Name = "name",              Order = 3,  Width = 120, Frozen = true, Visible = true  },
            new() { Id = 4,  Name = "status",            Order = 4,  Width = 100, Frozen = false,  Visible = true },
            new() { Id = 5,  Name = "resourceTypeSystem",Order = 5,  Width = 180, Frozen = false, Visible = true  },
            new() { Id = 6,  Name = "resourceType",      Order = 6,  Width = 120, Frozen = false, Visible = true  },
            new() { Id = 7,  Name = "resourceSort",      Order = 7,  Width = 110, Frozen = false, Visible = true  },
            new() { Id = 8,  Name = "quantity",          Order = 8,  Width = 70,  Frozen = false, Visible = true  },
            new() { Id = 9,  Name = "unit",              Order = 9,  Width = 45,  Frozen = false, Visible = true  },
            new() { Id = 10, Name = "cost",              Order = 10, Width = 60,  Frozen = false, Visible = true  },
            new() { Id = 11, Name = "changeFactor1",     Order = 11, Width = 125, Frozen = false, Visible = true  },
            new() { Id = 12, Name = "changeFactor2",     Order = 12, Width = 125, Frozen = false, Visible = true  },
            new() { Id = 13, Name = "waste",             Order = 13, Width = 55,  Frozen = false, Visible = true  },
            new() { Id = 14, Name = "cap",               Order = 14, Width = 45,  Frozen = false, Visible = true  },
            new() { Id = 15, Name = "baseCost",          Order = 15, Width = 85,  Frozen = false, Visible = true  },
            new() { Id = 16, Name = "opportunity",       Order = 16, Width = 100, Frozen = false, Visible = true  },
            new() { Id = 17, Name = "netCostQ",          Order = 17, Width = 80,  Frozen = false, Visible = true  },
            new() { Id = 18, Name = "totalNetCost",      Order = 18, Width = 100, Frozen = false, Visible = true  },
            new() { Id = 19, Name = "priceQTax",         Order = 19, Width = 90,  Frozen = false, Visible = true  },
            new() { Id = 20, Name = "priceQ",            Order = 20, Width = 60,  Frozen = false, Visible = true  },
            new() { Id = 21, Name = "priceTotaly",       Order = 21, Width = 90,  Frozen = false, Visible = true  },
            new() { Id = 22, Name = "priceTotallyTax",   Order = 22, Width = 120, Frozen = false, Visible = true  },
            new() { Id = 23, Name = "factor",            Order = 23, Width = 130, Frozen = false, Visible = true  },
            new() { Id = 24, Name = "minPrice",          Order = 24, Width = 70,  Frozen = false, Visible = true  },
            new() { Id = 25, Name = "ceilingPrice",      Order = 25, Width = 100, Frozen = false, Visible = true  },
            new() { Id = 26, Name = "priceSub",          Order = 26, Width = 80,  Frozen = false, Visible = true  },
            new() { Id = 27, Name = "priceTotalSub",     Order = 27, Width = 110, Frozen = false, Visible = true  },
            new() { Id = 28, Name = "diff",              Order = 28, Width = 85,  Frozen = false, Visible = true  },
            new() { Id = 29, Name = "responsible",       Order = 29, Width = 100, Frozen = false, Visible = true  },
            new() { Id = 30, Name = "Co2",               Order = 30, Width = 60,  Frozen = false, Visible = true  },
            new() { Id = 31, Name = "totalCo2",          Order = 31, Width = 60,  Frozen = false, Visible = true  },

            new() { Id = 32, Name = "Actually quantity", Order = 32, Width = 90,  Frozen = false, Visible = true  },
            new() { Id = 33, Name = "Worked Q",          Order = 33, Width = 60,  Frozen = false, Visible = true  },
            new() { Id = 34, Name = "Worked Q (%)",      Order = 34, Width = 80,  Frozen = false, Visible = true  },

            new() { Id = 35, Name = "Price Actually quantity ",Order = 35, Width = 60,  Frozen = false, Visible = true  },
            new() { Id = 36, Name = "Price Worked Q",          Order = 36, Width = 60,  Frozen = false, Visible = true  },
            new() { Id = 37, Name = "Price sub Tax",          Order = 37, Width = 60,  Frozen = false, Visible = true  },
            new() { Id = 38, Name = "Price Actually quantity Tax",Order = 38, Width = 60,  Frozen = false, Visible = true  },
            new() { Id = 39, Name = "Price Worked Q Tax",Order = 39, Width = 60,  Frozen = false, Visible = true  },

            new() { Id = 40, Name = "note",              Order = 40, Width = 80,  Frozen = false, Visible = true  },
        ];
        }

        public static List<NetColumnState> SummarySheet()
        {
            return
            [
                new() { Id = 0,  Name = "name",        Order = 0,  Width = 100, Frozen = true,  Visible = true },
            new() { Id = 1,  Name = "sort",        Order = 1,  Width = 80,  Frozen = false, Visible = true },
            new() { Id = 2,  Name = "netCal",      Order = 2,  Width = 60,  Frozen = false, Visible = true },
            new() { Id = 3,  Name = "netCalOH",    Order = 3,  Width = 60,  Frozen = false, Visible = true },
            new() { Id = 4,  Name = "sum",         Order = 4,  Width = 60,  Frozen = false, Visible = true },
            new() { Id = 5,  Name = "earnings",    Order = 5,  Width = 100, Frozen = false, Visible = true },
            new() { Id = 6,  Name = "ev",          Order = 6,  Width = 60,  Frozen = false, Visible = true },
            new() { Id = 7,  Name = "price",       Order = 7,  Width = 60,  Frozen = false, Visible = true },
            new() { Id = 8,  Name = "priceOG",     Order = 8,  Width = 90,  Frozen = false, Visible = true },

            // لاحظ أن العنوان فارغ في SSCTitles
            new() { Id = 9,  Name = "",            Order = 9,  Width = 80,  Frozen = false, Visible = false },

            new() { Id = 10, Name = "keyValue",    Order = 10, Width = 60,  Frozen = false, Visible = true },
            new() { Id = 11, Name = "kv",          Order = 11, Width = 60,  Frozen = false, Visible = true },
            new() { Id = 12, Name = "unit",        Order = 12, Width = 120, Frozen = false, Visible = true },
            new() { Id = 13, Name = "OH",          Order = 13, Width = 80,  Frozen = false, Visible = true },
            new() { Id = 14, Name = "factor",      Order = 14, Width = 80,  Frozen = false, Visible = true },
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
