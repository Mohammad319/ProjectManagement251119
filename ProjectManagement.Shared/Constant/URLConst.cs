namespace ProjectManagement.Shared.Constant
{
    public static class URLConst
    {
        public const string GetList = "list";
        public const string Details = "details";
        public const string CreateFromStorage = "cfstorage";
        public const string Copy = "copy";
        public const string Cut = "cut";
        public const string ReOrder = "reorder";
        public const string CopyCut = "cut";
        public const string GetAll = "getall";
        public const string Filter = "filter";

        public static class Department
        {
            public const string GetUsers = "getusers";
            public const string GetUsersByDepartment = "ausers";
        }
        public static class Account
        {
            public const string GetAll = "get";
            public const string AddRange = "addrange";
        }
        public static class Application
        {
            public const string Base = "ca";
            public const string Attribute = "ca";
            public const string Row = "row";
            public const string AppCalculationValues = "cca";
        }
        public static class Calculation
        {
            public const string ReOrder = "reorder";
            public const string Copy = "copy";
            public const string ProductionCopy = "productioncopy";
            public const string VersionCopy = "versioncopy";
            public const string Move = "move";
            public const string Share = "share";
            public const string Page = "page";
            public const string SharedPage = "sharedpage";
            public const string GetToPost = "gtp";
            public const string Create = "create";
            public const string AdditionalFactor = "af";
            public const string NetCalc = "nekjktc";
            public const string Sort = "sort";

            public const string HourlyPriceList = "hplzu";
            public const string DisplayPresets = "displaypresets";
        }

        public static class ResourceType
        {
            public const string Sort = "sort";
        }

        public static class Storages
        {
            public  const string Index = "Storages";
            public const string Save = "save";
            public const string Remove = "remove";
            public const string Update = "update";
        }

        public static class Template
        {
            public const string GetByDepartment = "getByDepartment";
            public const string GetById = "getbyid";
            public const string Set = "set";
            public const string Update = "update";
            public const string PostAdmin = "cadmin";

        }
        public static class TemplateColumn
        {
            public const string GetByDepartment = "getByDepartment";
            public const string GetById = "getbyid";
            public const string Set = "set";
            public const string Update = "update";
            public const string PostAdmin = "cadmin";
        }
        public static class Tender
        {
            public const string Get = "Get";
            public const string GetById = "getbyid";
            public const string Attribute = "attribute";
        }

        public static class Company
        {
            public const string GetById = "getbyid";
            public const string GetToPost = "gettopost";
            public const string GetVisibleOrByID = "gv";
        }

        public static class Customer
        {
            public const string GetById = "getbyid";
            public const string GetShortCustomer = "getShortCustomer";
        }

        public static class Folder
        {
            public const string GetFoldersByDepartmentId = "getbyepart";
            public const string Move = "move";
            public const string CreateForDepartment = "createfordepartment";
        }

        public static class Project
        {
            public const string Search = "search";
            public const string GetProjectPost = "gpp";
            public const string GetProjectsOtherDepartment = "gpgog";
            public const string GetByFolderDepartmentId = "gpmyg";
            public const string Move = "move";
            public const string Copy = "copy";
        }

        public static class Auth
        {
            public const string Login = "Login";
            public const string ForgotPassword = "ForgotPassword";
            public const string ResetPassword = "ResetPassword";
            public const string ChangePassword = "ChangePassword";
            public const string GetRoles = "GetRoles";
            public const string Uroles = "uroles";
            public const string RefreshToken = "RefreshToken";
            public const string Revoke = "Revoke";

        }
        public static class Offer
        {
            public const string Category = "category";
            public const string Offers = "offer";
            public const string Filter = "filter";

            public const string GetResourcesByOfferId = "getresources";
            public const string Set = "set";
            public const string ReCalc = "recalc";

            public const string GetOffersNotInResource = "getoffersninres";

        }
    }
}
