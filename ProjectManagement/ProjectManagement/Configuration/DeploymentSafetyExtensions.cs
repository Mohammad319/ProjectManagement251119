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
        var warnings = new List<string>();
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
        else if (!string.IsNullOrWhiteSpace(bootstrapEmail) || !string.IsNullOrWhiteSpace(bootstrapPassword))
        {
            warnings.Add("User:Email/User:Password are still present in configuration although Bootstrap:EnableConfiguredAdmin=false. Remove them from production config if they are no longer needed.");
        }

        ValidateAllowedHosts(configuration, warnings);
        ValidateConnectionString(configuration, "ConnectionStrings:AuthPermissionsConnection", "AuthPermissionsConnection", warnings, failures);
        ValidateConnectionString(configuration, "ConnectionStrings:BlueprintsConnection", "BlueprintsConnection", warnings, failures);
        ValidateMailSettings(configuration, warnings);

        if (failures.Count > 0)
            throw new InvalidOperationException("Production safety validation failed: " + string.Join(" | ", failures));

        if (string.IsNullOrWhiteSpace(configuration["DataProtection:KeysPath"]))
        {
            warnings.Add("DataProtection:KeysPath is not configured. Cookies and antiforgery tokens may be lost after app restarts depending on hosting mode.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Sentry:Dsn"]))
        {
            warnings.Add("Sentry:Dsn is not configured. Production exceptions will rely only on server-side logs.");
        }

        foreach (var warning in warnings)
            app.Logger.LogWarning("Deployment safety warning: {Warning}", warning);

        return app;
    }

    private static void ValidateAllowedHosts(IConfiguration configuration, List<string> warnings)
    {
        var allowedHosts = configuration["AllowedHosts"]?.Trim();
        if (string.IsNullOrWhiteSpace(allowedHosts) || string.Equals(allowedHosts, "*", StringComparison.Ordinal))
        {
            warnings.Add("AllowedHosts is wide open. Prefer explicit production hosts/domains instead of '*'.");
        }
    }

    private static void ValidateConnectionString(
        IConfiguration configuration,
        string key,
        string name,
        List<string> warnings,
        List<string> failures)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{name} connection string is missing.");
            return;
        }

        var normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

        if (normalized.Contains(@".\sqlexpress") ||
            normalized.Contains("(localdb)") ||
            normalized.Contains("server=localhost") ||
            normalized.Contains("server=127.0.0.1") ||
            normalized.Contains("datasource=localhost") ||
            normalized.Contains("datasource=127.0.0.1"))
        {
            warnings.Add($"{name} appears to target a local/developer SQL instance. Confirm this is intentional for production.");
        }

        if (normalized.Contains("integratedsecurity=true") || normalized.Contains("trusted_connection=true"))
        {
            warnings.Add($"{name} uses integrated security. Confirm the IIS/app-pool identity has the exact database permissions you expect in production.");
        }

        if (normalized.Contains("encrypt=false"))
        {
            warnings.Add($"{name} disables SQL connection encryption. Prefer Encrypt=True for production where possible.");
        }
    }

    private static void ValidateMailSettings(IConfiguration configuration, List<string> warnings)
    {
        var host = configuration["MailSettings:Host"]?.Trim();
        var mail = configuration["MailSettings:Mail"]?.Trim();
        var password = configuration["MailSettings:Password"];

        if (!string.IsNullOrWhiteSpace(host))
        {
            if (string.IsNullOrWhiteSpace(mail))
                warnings.Add("MailSettings:Host is set but MailSettings:Mail is empty.");

            if (string.IsNullOrWhiteSpace(password))
                warnings.Add("MailSettings:Host is set but MailSettings:Password is empty.");
        }
    }

    private static bool IsDefaultBootstrapAdmin(string? email, string? password)
    {
        return string.Equals(email, "Admin@at.com", StringComparison.OrdinalIgnoreCase)
            && string.Equals(password, "Admin@at.com9", StringComparison.Ordinal);
    }
}
