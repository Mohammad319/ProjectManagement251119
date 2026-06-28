namespace ProjectManagement.Client.Shared.Constants
{
    public static class PMAPIConst
    {
        public static string Server { private get; set; } = string.Empty;
        public static string ApiV1 => Server + "api/v1/";
        public static string Projects => ApiV1 + "projects/";
        public static string Opportunity => ApiV1 + "opportunity/";
        public static string Folders => ApiV1 + "folders/";
        public static string Status => ApiV1 + "status/";
        public static string TaskStatus => ApiV1 + "taskstatus/";
        public static string ResourceStatus => ApiV1 + "resourceStatus/";
        public static string Type => ApiV1 + "type/";
        public static string Contract => ApiV1 + "Contract/";
        public static string Compensation => ApiV1 + "Compensation/";
        public static string ProcurementMethods => ApiV1 + "ProcurementMethods/";
        public static string Calculation => ApiV1 + "Calculations/";
        public static string Income => ApiV1 + "Income/";
        public static string Auth => ApiV1 + "auth/";
        public static string Token => ApiV1 + "token/";
        public static string Template => ApiV1 + "template/";
        public static string TemplateColumn => ApiV1 + "templatecolumn/";
        public static string Application => ApiV1 + "Application/";
        public static string Section => ApiV1 + "section/";
        public static string Tender => ApiV1 + "tender/";
        public static string ProjectBids => ApiV1 + "projectbids/";
        public static string ProjectShares => ApiV1 + "projectshares/";
        public static string ShareCalc => ApiV1 + "shareCalc/";
        public static string Resource => ApiV1 + "Resources/";
        public static string Task => ApiV1 + "Tasks/";
        public static string Customer => ApiV1 + "Customers/";
        public static string CustomerContact => ApiV1 + "CustomerContacts/";
        public static string OrganisationsType => ApiV1 + "OrganisationsType";
        public static string Storage => ApiV1 + "Storages/";
        public static string Offer => ApiV1 + "Offers/";
        public static string Organisations => ApiV1 + "Organisations/";
        public static string OrganisationsContact => ApiV1 + "OrganisationsContact/";
        public static string Account => ApiV1 + "accounts/";
        public static string Tenant => ApiV1 + "Tenant/";
        public static string ResourceType => ApiV1 + "ResourceType/";
        public static string Departments => ApiV1 + "Departments/";
        public static string ItemCalcCategory => ApiV1 + "ItemCalcCategory/";
        public static string UserListSettings => ApiV1 + "UserListSettings/";
        public static string Notifications => ApiV1 + "notifications/";
        public static string ChangeLog => ApiV1 + "changelog/";
        public static string Details => "details/";

    }
}
