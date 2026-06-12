namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    /// <summary>
    /// Single source of truth for calculation status colors.
    /// Used by both the folder tree and the calculation lists so the
    /// status dot always shows the same color everywhere.
    /// The color stored on the status (admin settings / database) wins;
    /// the name-based mapping is only a legacy fallback, and statuses
    /// without any color get a neutral gray dot.
    /// </summary>
    public static class CalculationStatusColor
    {
        public const string NeutralFallback = "#94a3b8";

        public static string Resolve(ListCalculationMVVM calculation)
        {
            if (!string.IsNullOrWhiteSpace(calculation.StatusColor))
                return calculation.StatusColor;

            return ResolveByName(calculation.Status);
        }

        public static string ResolveByName(string? status) => status?.Trim().ToLowerInvariant() switch
        {
            "draft" or "utkast" => NeutralFallback,
            "active" or "ongoing" or "pagaende" or "pågående" => "#0ea5e9",
            "completed" or "done" or "klar" => "#10b981",
            "needs review" or "review" or "warning" or "varning" => "#f59e0b",
            "cancelled" or "canceled" or "avbruten" => "#f43f5e",
            _ => NeutralFallback
        };
    }
}
