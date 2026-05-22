# ProjectManagement - Go-Live Review (v4)

## Ready enough to publish?
Yes, after a short launch-freeze pass and an operator-owned backup/restore drill. The app now has a stronger production base: global exception handling, correlation id, HSTS, antiforgery, Serilog, health endpoints, rate limiting, deployment safety validation, persistent Data Protection validation, and broader department isolation checks across project/calculation subdomains.

## Highest-priority findings

### Must fix before public launch
1. Production secrets must be supplied through environment variables or a managed secret store.
2. `AllowedHosts` must be restricted to the real production host names.
3. Production SQL connection strings must be set for `AuthPermissionsConnection` and `BlueprintsConnection`.
4. `DataProtection:KeysPath` must be an absolute, persistent path and included in backups.
5. Backup and restore must be tested before the first real customer launch.

### Strongly recommended before launch
1. Configure `Sentry:Dsn` or another production exception sink.
2. Keep configured-admin bootstrap disabled in production by default.
3. Keep `/internal/tenants/reload` disabled unless it is operationally required.
4. Run smoke tests with two departments to verify tenant/department boundaries.
5. Verify reverse proxy settings before exposing the app to the internet.

## What this hardening pass covers
- `/health/live`
- `/health/ready`
- built-in rate limiting
- request logging enrichment for Serilog
- basic non-breaking security headers
- `Bootstrap:EnableConfiguredAdmin=false` gate for configured admin seeding
- fail-fast validation for dangerous production config
- persistent Data Protection key validation
- constant-time comparison for internal shared secrets
- stricter department filtering in project, calculation, folder, task, resource, offer, tender, opportunity, and share calculation flows
- `appsettings.Production.example.json`
- `docs/Production-Operations-Runbook.md`

## Important deployment note
The current `UseForwardedHeaders()` setup clears known proxies/networks and therefore trusts forwarded headers broadly. This is acceptable only when Kestrel is not directly internet-exposed and traffic always comes through IIS/reverse proxy/firewall rules.

## Recommended next step after this pass
- Create a real `appsettings.Production.json`.
- Move secrets to IIS/App Pool environment variables or another secret store.
- Run a staging publish and verify:
  - `/health/live`
  - `/health/ready`
  - login/logout
  - password reset / email flow
  - a heavy customer workflow
  - cross-department access denial
  - backup and restore into staging
