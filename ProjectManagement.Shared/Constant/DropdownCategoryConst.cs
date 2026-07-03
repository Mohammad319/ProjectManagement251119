namespace ProjectManagement.Shared.Constant
{
    /// <summary>
    /// Stable keys for the admin-settings dropdown categories (one per tab).
    /// The "Obligatorisk" (IsRequired) setting is stored per category — never per row/value.
    /// </summary>
    public static class DropdownCategoryConst
    {
        public const string ProjectType = "ProjectType";
        public const string Contract = "Contract";
        public const string Compensation = "Compensation";
        public const string ProcurementMethod = "ProcurementMethod";
        public const string ProcurementProcedure = "ProcurementProcedure";
        public const string ProjectStatus = "ProjectStatus";
        public const string CalculationStatus = "CalculationStatus";
        public const string TaskStatus = "TaskStatus";
        public const string ResourceStatus = "ResourceStatus";

        public static readonly string[] All =
        [
            ProjectType,
            Contract,
            Compensation,
            ProcurementMethod,
            ProcurementProcedure,
            ProjectStatus,
            CalculationStatus,
            TaskStatus,
            ResourceStatus
        ];

        /// <summary>
        /// Default when no admin choice is stored: Projektstatus/Upphandlingsstatus and
        /// Kalkylstatus are required, every other dropdown is optional.
        /// </summary>
        public static bool DefaultIsRequired(string category) =>
            category is ProjectStatus or CalculationStatus;

        /// <summary>
        /// Merges stored overrides on top of the defaults so every category always has a value.
        /// </summary>
        public static Dictionary<string, bool> MergeWithDefaults(IReadOnlyDictionary<string, bool>? overrides)
        {
            var result = new Dictionary<string, bool>(StringComparer.Ordinal);

            foreach (var category in All)
                result[category] = overrides is not null && overrides.TryGetValue(category, out var stored)
                    ? stored
                    : DefaultIsRequired(category);

            return result;
        }
    }
}
