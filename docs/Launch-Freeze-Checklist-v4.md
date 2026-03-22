# Launch Freeze Checklist

## Before publishing
- Replace `AllowedHosts=*` with the real host names.
- Create `appsettings.Production.json` from the example file.
- Move secrets out of `appsettings.json`.
- Set `Bootstrap:EnableConfiguredAdmin=false` in production.
- Set `DataProtection:KeysPath` to a persistent server folder.
- Replace `TenantReload:Secret` with a strong random value or disable that endpoint.
- Use production SQL connection strings with `Encrypt=True`.
- Verify SMTP settings.

## Smoke test after deploy
- Open `/health/live`
- Open `/health/ready`
- Sign in / sign out
- Open an existing project
- Run one important calculation flow
- Check server logs for `GoLive warning:` entries
- Confirm 429 is returned when hammering `/api/client-logs`
