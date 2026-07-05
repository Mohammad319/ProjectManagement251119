namespace ProjectManagement.Shared.DTO.Organisation
{
    public static class OrganisationStatusCatalog
    {
        public const string Active = "Aktiv";
        public const string UnderReview = "Under granskning";
        public const string Approved = "Godkänd";
        public const string NotApproved = "Ej godkänd";
        public const string Paused = "Pausad";
        public const string Archived = "Arkiverad";
        public const string Warning = "Varning";
        public const string NotSpecified = "Ej angivet";

        public static readonly string[] FixedStatuses =
        [
            Active,
            UnderReview,
            Approved,
            NotApproved,
            Paused,
            Archived,
            Warning
        ];

        public static string Normalize(string? status, bool isVisible = true)
        {
            if (string.IsNullOrWhiteSpace(status))
                return NotSpecified;

            var trimmed = status.Trim();
            return FixedStatuses.FirstOrDefault(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase))
                ?? NotSpecified;
        }

        public static string BadgeClasses(string? status)
        {
            return Normalize(status) switch
            {
                Active => "bg-emerald-50 text-emerald-700 ring-emerald-600/20 dark:bg-emerald-950/40 dark:text-emerald-300 dark:ring-emerald-400/30",
                Approved => "bg-green-50 text-green-700 ring-green-600/20 dark:bg-green-950/40 dark:text-green-300 dark:ring-green-400/30",
                UnderReview => "bg-sky-50 text-sky-700 ring-sky-600/20 dark:bg-sky-950/40 dark:text-sky-300 dark:ring-sky-400/30",
                NotApproved => "bg-rose-50 text-rose-700 ring-rose-600/20 dark:bg-rose-950/40 dark:text-rose-300 dark:ring-rose-400/30",
                Paused => "bg-orange-50 text-orange-700 ring-orange-600/20 dark:bg-orange-950/40 dark:text-orange-300 dark:ring-orange-400/30",
                Archived => "bg-slate-100 text-slate-600 ring-slate-500/20 dark:bg-slate-800 dark:text-slate-300 dark:ring-slate-500/30",
                Warning => "bg-amber-50 text-amber-800 ring-amber-600/30 dark:bg-amber-950/40 dark:text-amber-200 dark:ring-amber-400/40",
                _ => "bg-slate-50 text-slate-600 ring-slate-500/20 dark:bg-slate-900 dark:text-slate-300 dark:ring-slate-600/40"
            };
        }
    }
}
