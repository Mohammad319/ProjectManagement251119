# Launch Freeze Checklist

## Before publishing
- Create `appsettings.Production.json` from `ProjectManagement/ProjectManagement/appsettings.Production.example.json`, then keep real secrets outside git.
- Replace `AllowedHosts=*` with the production host names.
- Set `ConnectionStrings__AuthPermissionsConnection` and `ConnectionStrings__BlueprintsConnection` to production SQL databases.
- Use SQL connection strings with `Encrypt=True` when supported by the SQL environment.
- Set `DataProtection__KeysPath` to an absolute, persistent folder that survives redeployments.
- Back up the Data Protection key folder with the same care as the databases.
- Set `Bootstrap__EnableConfiguredAdmin=false` after any one-time bootstrap task is complete.
- Remove `User__Email` and `User__Password` from production config unless bootstrap is intentionally enabled.
- Replace `TenantReload__Secret` with a strong random value, or remove the setting if runtime tenant reload is not needed.
- If `TenantContext__AllowHeaderOverride=true`, set a strong `TenantContext__HeaderSecret` and keep the app behind a trusted proxy.
- Verify SMTP settings if password reset or account emails are enabled.
- Configure `Sentry__Dsn` or an equivalent production exception sink.
- Keep `SecurityHeaders__CspReportOnly=true` for the first production-like staging pass and review browser CSP reports before enforcing.
- Confirm `ValidateDeploymentSafety()` passes in the production environment.
- Confirm `.github/workflows/ci.yml` passes on the release branch.
- Run `.\tools\operations\Invoke-ProductionReadinessChecks.ps1`.
- Generate and review idempotent migration scripts with `.\tools\operations\New-MigrationScripts.ps1`.
- Confirm migrations/scripts have been applied to the auth database, tenant databases, and blueprints database.
- Confirm backup and restore have been tested before the first real customer launch.

## Smoke test after deploy
- Validate the staged publish directory with `.\tools\operations\Test-PublishedWebAssets.ps1 -PublishDirectory C:\Publish\ProjectManagement`.
- Open `/health/live`.
- Open `/health/ready`.
- Sign in and sign out.
- Open an existing project.
- Run one important calculation flow.
- Upload or process a representative price-import file if the feature is enabled.
- Check server logs for `Deployment safety warning:` entries.
- Confirm `429` is returned when hammering `/api/client-logs`.
- Confirm a user cannot open/edit a project, calculation, folder, task, resource, offer, tender, opportunity, or share setting from another department.
- Confirm `/internal/tenants/reload` rejects requests without the correct `X-Tenant-Reload-Secret`.
- Confirm mutating API/account/internal requests create `Audit event` log entries with user/tenant/department/trace context.
- Restart the app and confirm existing login/session behavior remains stable, proving Data Protection keys persisted.

## Rollback readiness
- Keep the previous deploy package available until the new release passes smoke tests.
- Keep the database backup taken immediately before migration.
- Document whether each migration is reversible or requires restore-from-backup.
- Verify the operator can restore the app package, config, Data Protection keys, uploaded/imported files, and databases into staging.
