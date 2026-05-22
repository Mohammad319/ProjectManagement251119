# Production Operations Runbook

## Configuration
- Store secrets in the host environment or a managed secret store, not in source-controlled JSON files.
- Required production settings:
  - `AllowedHosts`
  - `ConnectionStrings__AuthPermissionsConnection`
  - `ConnectionStrings__BlueprintsConnection`
  - `DataProtection__KeysPath`
  - SMTP settings when password reset or account emails are enabled
- Recommended production settings:
  - `Sentry__Dsn` or an equivalent exception sink
  - explicit `ASPNETCORE_ENVIRONMENT=Production`
- Optional settings:
  - `TenantReload__Secret`; remove it if runtime tenant reload is not needed
  - `TenantContext__HeaderSecret` when `TenantContext__AllowHeaderOverride=true`
  - `PriceImport__StorageRoot` for uploaded/imported price files
  - `PriceImportAi__ApiKey` only when AI extraction is enabled

## Deployment
- Build and test before publish:
  - `dotnet build "ProjectManagement.slnx"`
  - `dotnet test "ProjectManagement.slnx" --no-build`
- Run the local readiness script before release candidates:
  - `.\tools\operations\Invoke-ProductionReadinessChecks.ps1`
- Deploy with `ASPNETCORE_ENVIRONMENT=Production`.
- Verify startup fails fast if unsafe production settings are present.
- Verify `/health/live` and `/health/ready` after deployment.
- Keep the previous package and config available until smoke tests pass.
- Check logs for `Deployment safety warning:` immediately after startup.
- Pull requests and release branches should pass the CI workflow in `.github/workflows/ci.yml`.

## Database Readiness
- Run EF migrations or the approved SQL migration script during a maintenance window.
- Confirm the auth database, all tenant databases, and the blueprints database are reachable from the application identity.
- Use encrypted SQL connections where possible.
- Keep tenant database connection strings limited to the exact tenant database.
- Before applying migrations, take a full backup and record the exact backup name/location in the deployment notes.
- If using generated migration scripts, review the script for destructive operations before execution.
- If using an EF migration bundle, run it in staging first with the same connection-string shape used in production.
- Generate reviewed idempotent migration scripts with:
  - `.\tools\operations\New-MigrationScripts.ps1`

## Data Protection
- `DataProtection__KeysPath` must point to a persistent folder shared by all app instances.
- Back up the key folder with the same retention policy as application databases.
- Do not delete old keys during normal deployments; doing so can invalidate cookies and antiforgery tokens.

## Backup And Recovery
- Back up the AuthPermissions database, tenant databases, TaskResourceBlueprints database, uploaded/imported files, and Data Protection keys.
- Test restore into a staging environment before relying on the backup policy.
- Store backups somewhere separate from the application server.
- Keep at least one backup from immediately before every deployment that changes schema or persisted data.
- Generate a SQL backup plan template with:
  - `.\tools\operations\New-BackupRunbook.ps1 -DatabaseName ProjectManagement,TaskResourceBlueprints -BackupDirectory D:\SqlBackups`
- Recovery smoke test:
  - app starts
  - login works
  - tenant connection strings resolve
  - existing projects/calculations load
  - price import files remain available

## Rollback
- App rollback restores the previous published package and production config.
- Data rollback restores the pre-deploy database backups when a migration cannot be safely reversed.
- Restore Data Protection keys only when moving to a new server or recovering from loss; do not overwrite healthy keys during a normal app rollback.
- After rollback, verify `/health/ready`, login, one project, one calculation, and one tenant-isolation denial.

## Security Smoke Tests
- A user from department A must not read or edit department B projects, calculations, folders, tasks, resources, offers, tenders, opportunities, or share settings.
- Public endpoints must be limited to intentional anonymous flows only.
- `/internal/tenants/reload` requires `X-Tenant-Reload-Secret` when enabled.
- Auth and client-log endpoints should return `429` under abusive request rates.
- If `TenantContext__AllowHeaderOverride=true`, verify requests without the header secret cannot override tenant context.
- Confirm production API responses do not include stack traces or raw exception details.
- Keep `SecurityHeaders__CspReportOnly=true` until the CSP report noise is understood; only switch to enforcement after Blazor and third-party assets are verified.

## Logging And Incident Handling
- Every support ticket should include timestamp, user, tenant, department, and correlation/trace id when available.
- Production exception details should stay in server logs, not API responses.
- Review logs after every deployment for deployment safety warnings, health check failures, and repeated authorization failures.
- For security incidents, preserve logs before redeploying or rotating data, then rotate the affected secret and invalidate sessions when needed.
- Audit logs are emitted for mutating `/api`, `/Account`, and `/internal` requests without request bodies. Review `Audit event` entries for user, tenant, department, status code, path, trace id, and remote IP.

## Performance Readiness
- Run at least one large calculation/project workflow in staging with production-like data.
- Watch SQL duration, memory, CPU, and request latency during import, calculation, tender, and offer flows.
- Check indexes before launch for high-volume filters: tenant, department, project, calculation, task, resource, tender, offer, and opportunity identifiers.
- Keep file upload limits aligned with the hosting proxy and `PriceImport__MaxUploadSizeMb`.

## User Experience Smoke Tests
- Verify first load, login, logout, navigation, and refresh behavior in the production host path.
- Verify validation messages are clear for failed login, failed upload, invalid calculation edits, and unauthorized cross-department actions.
- Test desktop and a narrow/mobile viewport for the main project and calculation pages.
