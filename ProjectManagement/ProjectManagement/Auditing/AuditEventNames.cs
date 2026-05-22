namespace ProjectManagement.Auditing;

public static class AuditEventNames
{
    public const string HttpMutation = "http.mutation";
    public const string InternalTenantReload = "internal.tenant.reload";
    public const string Identity = "identity";
    public const string CalculationSharing = "calculation.sharing";
    public const string DataDeletion = "data.deletion";
}
