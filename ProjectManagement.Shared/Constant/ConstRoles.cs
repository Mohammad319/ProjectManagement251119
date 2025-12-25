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
