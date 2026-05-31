# ProjectManagement251119

## Production Configuration

Set production values in the host environment, IIS/App Pool environment variables, or a managed secret store. Do not put real secrets in source-controlled JSON files.

Required production settings:

- `ConnectionStrings__AuthPermissionsConnection`
- `ConnectionStrings__BlueprintsConnection`
- `DataProtection__KeysPath`
- `TenantReload__Secret`
- `MailSettings__Mail`
- `MailSettings__DisplayName`
- `MailSettings__Password`
- `MailSettings__Host`
- `MailSettings__Port`
- `Sentry__Dsn`
- `SecurityHeaders__CspReportOnly`

Optional feature settings:

- `Bootstrap__EnableConfiguredAdmin`
- `User__Email`
- `User__Password`
- `TenantContext__AllowHeaderOverride`
- `TenantContext__HeaderSecret`
- `PriceImport__StorageRoot`
- `PriceImport__MaxUploadSizeMb`
- `PriceImportAi__Enabled`
- `PriceImportAi__ApiKey`

`DefaultConnection` and `TaskResourceBlueprintsDb` are legacy fallback names. New deployments should use `AuthPermissionsConnection` and `BlueprintsConnection`.

## Production Checks

Before publishing:

```powershell
dotnet build "ProjectManagement.slnx"
dotnet test "ProjectManagement.slnx" --no-build
```

After publishing, verify `/health/live`, `/health/ready`, login/logout, one heavy project/calculation flow, and cross-department access denial for projects, calculations, folders, tasks, resources, offers, tenders, opportunities, and calculation sharing.

Operational guidance is documented in `docs/Production-Operations-Runbook.md` and the release checklist is in `docs/Launch-Freeze-Checklist-v4.md`.

Useful release helpers:

```powershell
.\tools\operations\Invoke-ProductionReadinessChecks.ps1
.\tools\operations\Test-PublishedWebAssets.ps1 -PublishDirectory C:\Publish\ProjectManagement
.\tools\operations\New-MigrationScripts.ps1
.\tools\operations\New-BackupRunbook.ps1 -DatabaseName ProjectManagement,TaskResourceBlueprints -BackupDirectory D:\SqlBackups
```
