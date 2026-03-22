using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ProjectManagement.Configuration;

public static class DeploymentSafetyExtensions
{
    public static WebApplication ValidateDeploymentSafety(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            return app;

        var failures = new List<string>();
        var configuration = app.Configuration;

        var tenantReloadSecret = configuration["TenantReload:Secret"];
        if (string.Equals(tenantReloadSecret, "dev-only-tenant-reload-secret", StringComparison.Ordinal))
        {
            failures.Add("TenantReload:Secret is still using the development default.");
        }

        var allowHeaderOverride = configuration.GetValue<bool>("TenantContext:AllowHeaderOverride");
        var headerSecret = configuration["TenantContext:HeaderSecret"];
        if (allowHeaderOverride && string.IsNullOrWhiteSpace(headerSecret))
        {
            failures.Add("TenantContext:AllowHeaderOverride=true requires TenantContext:HeaderSecret in non-development environments.");
        }

        var bootstrapAdminEnabled = configuration.GetValue<bool>("Bootstrap:EnableConfiguredAdmin");
        var bootstrapEmail = configuration["User:Email"]?.Trim();
        var bootstrapPassword = configuration["User:Password"];

        if (bootstrapAdminEnabled)
        {
            if (string.IsNullOrWhiteSpace(bootstrapEmail) || string.IsNullOrWhiteSpace(bootstrapPassword))
            {
                failures.Add("Bootstrap:EnableConfiguredAdmin=true but User:Email/User:Password are missing.");
            }
            else if (IsDefaultBootstrapAdmin(bootstrapEmail, bootstrapPassword))
            {
                failures.Add("Configured bootstrap admin is still using the repository default credentials.");
            }
        }

        if (failures.Count > 0)
            throw new InvalidOperationException("Production safety validation failed: " + string.Join(" | ", failures));

        if (string.IsNullOrWhiteSpace(configuration["DataProtection:KeysPath"]))
        {
            app.Logger.LogWarning("DataProtection:KeysPath is not configured. Cookies and antiforgery tokens may be lost after app restarts depending on hosting mode.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Sentry:Dsn"]))
        {
            app.Logger.LogWarning("Sentry:Dsn is not configured. Production exceptions will rely only on server-side logs.");
        }

        return app;
    }

    private static bool IsDefaultBootstrapAdmin(string? email, string? password)
    {
        return string.Equals(email, "Admin@at.com", StringComparison.OrdinalIgnoreCase)
            && string.Equals(password, "Admin@at.com9", StringComparison.Ordinal);
    }
}
