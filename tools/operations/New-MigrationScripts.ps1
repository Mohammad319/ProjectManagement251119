[CmdletBinding()]
param(
    [string] $OutputDirectory = "artifacts/migrations",
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$outDir = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Invoke-EfMigrationScript {
    param(
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $StartupProject,
        [Parameter(Mandatory)] [string] $Context,
        [Parameter(Mandatory)] [string] $OutputFile
    )

    Write-Host "Generating idempotent migration script for $Context..."
    dotnet ef migrations script `
        --idempotent `
        --configuration $Configuration `
        --project (Join-Path $repoRoot $Project) `
        --startup-project (Join-Path $repoRoot $StartupProject) `
        --context $Context `
        --output (Join-Path $outDir $OutputFile)
}

Invoke-EfMigrationScript `
    -Project "AuthPermissions\AuthPermissions.csproj" `
    -StartupProject "ProjectManagement\ProjectManagement\ProjectManagement.csproj" `
    -Context "ApplicationDbContext" `
    -OutputFile "AuthPermissions.sql"

Invoke-EfMigrationScript `
    -Project "Persistence\Persistence.csproj" `
    -StartupProject "ProjectManagement\ProjectManagement\ProjectManagement.csproj" `
    -Context "ShardingSingleDbContext" `
    -OutputFile "TenantDatabase.sql"

Write-Host "Migration scripts written to $outDir"
