<#
Init databases (AuthPermissions + Tenant template + Blueprints)

ملاحظة:
 - شغّل السكربت من جذر الـ repository (نفس مستوى ProjectManagement251119)
 - تحتاج .NET SDK (net10) و EF Tools
 - عدّل connection strings حسب جهازك/السيرفر

الهدف:
 1) إنشاء/تحديث قاعدة بيانات AuthPermissions (Identity + Tenants)
 2) إنشاء/تحديث قاعدة بيانات الـ Blueprints
 3) (اختياري) إنشاء Tenant template DB وتحديثها (ShardingSingleDbContext)

خطوات سريعة:
  - في التطوير: تستطيع حذف قواعد البيانات ثم تشغيل هذا السكربت
  - قبل الإطلاق: استخدم 'dotnet ef migrations script --idempotent' لتوليد SQL قابل لإعادة التشغيل
#>

$ErrorActionPreference = "Stop"

# ---- Connection strings (يمكن تحريكها لـ user-secrets أو env vars) ----
if ([string]::IsNullOrWhiteSpace($env:CATALOG_CONN)) {
  $env:CATALOG_CONN = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=SiSTOfotoAppPM2;Integrated Security=True;MultipleActiveResultSets=True"
}
if ([string]::IsNullOrWhiteSpace($env:AuthPermissionsDB)) {
  $env:AuthPermissionsDB = $env:CATALOG_CONN
}
if ([string]::IsNullOrWhiteSpace($env:BLUEPRINTS_CONN)) {
  $env:BLUEPRINTS_CONN = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TaskResourceBlueprints;Integrated Security=True;MultipleActiveResultSets=True"
}
if ([string]::IsNullOrWhiteSpace($env:TENANT_TEMPLATE_CONN)) {
  $env:TENANT_TEMPLATE_CONN = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ProjectManagement_TenantTemplate;Integrated Security=True;MultipleActiveResultSets=True"
}

Write-Host "== Catalog/Identity DB ==" -ForegroundColor Cyan
dotnet ef database update `
  --project .\AuthPermissions\AuthPermissions.csproj `
  --startup-project .\ProjectManagement\ProjectManagement\ProjectManagement.csproj `
  --context AuthPermissions.Context.ApplicationDbContext

Write-Host "== Blueprints DB ==" -ForegroundColor Cyan
dotnet ef database update `
  --project .\TaskResourceBlueprints\TaskResourceBlueprints.csproj `
  --startup-project .\ProjectManagement\ProjectManagement\ProjectManagement.csproj `
  --context TaskResourceBlueprints.Infrastructure.TaskResourceBlueprintsContext

Write-Host "== Tenant template DB (ShardingSingleDbContext) ==" -ForegroundColor Cyan
dotnet ef database update `
  --project .\Persistence\Persistence.csproj `
  --startup-project .\ProjectManagement\ProjectManagement\ProjectManagement.csproj `
  --context Persistence.Context.ShardingSingleDbContext

Write-Host "Done." -ForegroundColor Green
