# ProjectManagement – Go-Live Review (v4)

## Ready enough to publish?
Yes, after a short launch-freeze pass. The app already has a good base (global exception handling, correlation id, HSTS, antiforgery, Serilog), but a few production details should be closed before public launch.

## Highest-priority findings from the uploaded project

### Must fix before public launch
1. `appsettings.json` still contains `User:Email` and `User:Password`.
2. `TenantReload:Secret` still uses `dev-only-tenant-reload-secret`.
3. `AllowedHosts` is still `*`.
4. SQL connection strings still point to `./SQLEXPRESS` with `Encrypt=False`.
5. Data Protection key persistence is not configured.

### Strongly recommended before launch
1. Add production-safe request rate limiting.
2. Add health endpoints for uptime checks.
3. Add response security headers.
4. Log go-live configuration warnings at startup.
5. Keep configured-admin bootstrap disabled in production by default.

## What this patch adds
- `/health/live`
- `/health/ready`
- built-in rate limiting
- request logging enrichment for Serilog
- basic non-breaking security headers
- `Bootstrap:EnableConfiguredAdmin=false` gate for configured admin seeding
- startup warnings for dangerous production config
- `appsettings.Production.example.json`

## Important deployment note
Your current `UseForwardedHeaders()` setup clears known proxies/networks and therefore trusts forwarded headers broadly. This is acceptable only when Kestrel is **not** directly internet-exposed and traffic always comes through IIS/reverse proxy/firewall rules.

## Recommended next step after this patch
- create a real `appsettings.Production.json`
- move secrets to IIS/App Pool environment variables or another secret store
- run a staging publish and verify:
  - `/health/live`
  - `/health/ready`
  - login/logout
  - password reset / email flow
  - a heavy customer workflow (large calculation/project)
