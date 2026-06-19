# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Commands

### Build & Test
```powershell
# Build entire solution
dotnet build "ProjectManagement.slnx"

# Run all tests
dotnet test "ProjectManagement.slnx" --no-build

# Run a single test class
dotnet test "Tests/ProjectManagement.Tests/ProjectManagement.Tests.csproj" --filter "FullyQualifiedName~ClassName"

# Production readiness check
.\tools\operations\Invoke-ProductionReadinessChecks.ps1
```

### Run Applications
```powershell
# Main app (Blazor WASM + API server)
dotnet run --project "ProjectManagement/ProjectManagement/ProjectManagement.csproj"

# Admin portal (Blazor Server)
dotnet run --project "ProjectManagement.Adminstrator/ProjectManagement.Adminstrator.csproj"

# Python price import service
cd PriceImportPythonService
uvicorn main:app --reload
```

### Database Migrations
```powershell
# Add migration for main DB (Persistence project)
dotnet ef migrations add <MigrationName> --project Persistence --startup-project ProjectManagement/ProjectManagement

# Add migration for auth DB (AuthPermissions project)
dotnet ef migrations add <MigrationName> --project AuthPermissions --startup-project ProjectManagement/ProjectManagement

# Generate SQL scripts for production
.\tools\operations\New-MigrationScripts.ps1
```

### Health Checks
- `GET /health/live` — لiveness probe
- `GET /health/ready` — readiness probe (checks DB connectivity)

---

## Architecture

### Solution Structure
```
Business/
  Domain/         ← Entities, Repository interfaces, DTOs
  Application/    ← CQRS: Commands & Queries organized by feature

Infrastructure/
  Persistence/          ← EF Core (ShardingSingleDbContext) — main DB
  AuthPermissions/      ← ASP.NET Identity + auth DB (ApplicationDbContext)
  TaskResourceBlueprints/ ← Second DB for resource blueprints

Presentation/
  ProjectManagement/                  ← APPLICATION 1: Blazor hybrid (Server + WASM)
    ProjectManagement/                ← ASP.NET Core host + API + Razor Components
    ProjectManagement.Client/         ← Blazor WebAssembly (SPA client)
  ProjectManagement.Adminstrator/     ← APPLICATION 2: Blazor Server (admin portal)
  ProjectManagement.AppHost/          ← .NET Aspire orchestrator

Shared/
  ProjectManagement.Shared/           ← DTOs, Enums, Base classes (shared by all)
  ProjectManagement.Client.Shared/    ← MVVM, ViewModels, client-side services
  ProjectManagement.ServiceDefaults/  ← Common service configuration

External (outside repo):
  Library/BlazorMHD/BlazorMHD.UI/
  Library/ContextMenuMHD/ContextMenuMHD/
```

### Two Databases
| Database | Context | Connection String Key |
|---|---|---|
| Main app data | `ShardingSingleDbContext` | `AuthPermissionsConnection` |
| Resource blueprints | `TaskResourceBlueprintsContext` | `BlueprintsConnection` |
| Auth/Identity | `AuthPermissionDbContext` | `AuthPermissionsConnection` |

### Multi-Tenancy
`ShardingSingleDbContext` is multi-tenant via a **global query filter** on `TenantId`. The `TenantId` and `CurrentUserId` must be set from middleware/service before any query. All auditable entities (`AuditableEntity<T>`, `AuditableSoftDeletableEntity<T>`) get `TenantId` automatically via the interceptor in `TenantAuditSaveChangesInterceptor`.

### CQRS Pattern (Application Layer)
Features are organized under `Application/Feature/<Domain>/`:
- `Commands/` — write operations implementing a command interface
- `Queries/` — read operations returning read models
- `I<Feature>Service.cs` — interface injected into UI/controllers

Key domains: `Calculation` (with sub-features: Task, Resource, Storage, Tender, Opportunity, CalcShare, TemplateTable), `Project`, `Organisation`, `Offer`, `PriceImport`, `TfIdf`.

### Main App: Hybrid Blazor Rendering
`ProjectManagement` (host) supports both **Interactive Server** and **Interactive WebAssembly** render modes simultaneously. `ProjectManagement.Client` assembly is added as additional assembly. The hub `/notification` uses SignalR.

### Admin App: Blazor Server Only
`ProjectManagement.Adminstrator` is pure Blazor Server with circuit-based interactivity. Default culture is `sv-SE` (Swedish), with `en-US` as secondary.

### Python Microservice (PriceImportPythonService)
FastAPI service at port configured separately. Exposes:
- `GET /health`
- `POST /extract-text` — extracts text from PDF, DOCX, XLSX files

Invoked from .NET via `IPriceImportExtractionClient` (defined in `Application/Feature/PriceImport/`).

### Authentication Bootstrap
Both apps call `await app.InitializeAuthPermissionsAsync()` at startup to initialize roles/permissions before any requests are served. The main app also calls `app.ValidateDeploymentSafety()` to fail fast on unsafe production config.

---

## Required Connection Strings

Both apps need these in `appsettings.json` or environment variables:
- `ConnectionStrings__AuthPermissionsConnection` — main + auth DB
- `ConnectionStrings__BlueprintsConnection` — resource blueprints DB

Legacy fallback names: `DefaultConnection`, `TaskResourceBlueprintsDb`.

---

## Key Production Settings (from README)

Required: `DataProtection__KeysPath`, `TenantReload__Secret`, `MailSettings__*`, `Sentry__Dsn`  
Optional AI feature: `PriceImportAi__Enabled`, `PriceImportAi__ApiKey`
